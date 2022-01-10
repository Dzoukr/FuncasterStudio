module FuncasterStudio.Server.Episodes.API

open System
open System.IO
open Azure.Data.Tables
open Azure.Storage.Blobs
open FuncasterStudio.Server
open FuncasterStudio.Server.Errors
open FuncasterStudio.Server.PodcastStorage
open FuncasterStudio.Shared.Episodes.API
open FuncasterStudio.Shared.Errors
open FuncasterStudio.Shared.Validation
open Giraffe
open Giraffe.GoodRead
open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open FsToolkit.ErrorHandling
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.StaticFiles
open Microsoft.Extensions.Logging

let private getEpisodes (episodesTableClient:TableClient) () =
    task {
        let! eps = PodcastStorage.getEpisodes episodesTableClient ()
        return
            eps
            |> List.map (fun x ->
                ({
                    Guid = x.Guid
                    Season = x.Season
                    Episode = x.Episode
                    Publish = x.Publish
                    Title = x.Title
                    Duration = x.Duration
                    EpisodeType = x.EpisodeType
                } : EpisodeListItem)
            )
            |> List.sortByDescending (fun x -> x.Publish)
    }

let private getEpisode (episodesTableClient:TableClient) (guid:string) =
    task {
        let! ep = PodcastStorage.getEpisode episodesTableClient guid
        let ep = ep.Value
        return
            ({
                Guid = ep.Guid
                Season = ep.Season
                Episode = ep.Episode
                Publish = ep.Publish.ToString("yyyy-MM-ddTHH:mm:sszzz")
                Title = ep.Title
                Duration = ep.Duration |> string
                EpisodeType = ep.EpisodeType
                Description = ep.Description
                Restrictions = ep.Restrictions
                Explicit = ep.Explicit
                Keywords = ep.Keywords
            } : Episode), (ep.Enclosure.Url |> string)
    }

let private createEpisode (episodesTableClient:TableClient) (c:Episode) =
    task {
        let item : Funcaster.Domain.Item =
            {
                Guid = c.Guid
                Season = c.Season
                Episode = c.Episode
                Enclosure = { Url = Uri("http://example.com"); Type = ""; Length = 0L }
                Publish = DateTimeOffset.Parse c.Publish
                Title = c.Title
                Description = c.Description
                Restrictions = c.Restrictions
                Duration = TimeSpan.Parse c.Duration
                Explicit = c.Explicit
                Image = None
                Keywords = c.Keywords
                EpisodeType = c.EpisodeType
            }
        return!
            item |> PodcastStorage.upsertEpisode episodesTableClient
    }

let private updateEpisode (episodesTableClient:TableClient) (c:Episode) =
    task {
        match! PodcastStorage.getEpisode episodesTableClient c.Guid with
        | Some item ->
            return!
                { item with
                    Season = c.Season
                    Episode = c.Episode
                    Publish = DateTimeOffset.Parse c.Publish
                    Title = c.Title
                    Description = c.Description
                    Restrictions = c.Restrictions
                    Duration = TimeSpan.Parse c.Duration
                    Explicit = c.Explicit
                    Image = None
                    Keywords = c.Keywords
                    EpisodeType = c.EpisodeType
                }
                |> PodcastStorage.upsertEpisode episodesTableClient
        | None -> return ()
    }

let private validateUnique (episodesTableClient:TableClient) (ep:Episode) =
    task {
        let! item = PodcastStorage.getEpisode episodesTableClient ep.Guid
        if item.IsSome then return [{ Key = Episode.guid.Name; Message = ValidationErrorType.MustBeUnique }]
        else return []
    }

let private service (episodesTableClient:TableClient) = {
    CreateEpisode =
        ServerError.validate Episode.validate
        >> ServerError.validateAsync (validateUnique episodesTableClient)
        >> Task.bind (createEpisode episodesTableClient)
        >> Async.AwaitTask
    UpdateEpisode =
        ServerError.validate Episode.validate
        >> updateEpisode episodesTableClient >> Async.AwaitTask
    GetEpisodes = getEpisodes episodesTableClient >> Async.AwaitTask
    GetEpisode = getEpisode episodesTableClient >> Async.AwaitTask
}

let private uploadFile (blobContainer:BlobContainerClient) (episodesTableClient:TableClient) (ctx:HttpContext) (b:byte []) =
    task {
        let originalName = ctx.Request.Headers.["filename"].[0]
        let episodeGuid = ctx.Request.Headers.["episodeGuid"].[0] |> key
        let season = ctx.Request.Headers.["season"].[0] |> int
        let deleteFile = ctx.Request.Headers.["replaceoldfile"].Count > 0
        let name = $"episodes/s{season}/{episodeGuid}{Path.GetExtension originalName}"
        if deleteFile then
            match! PodcastStorage.getEpisode episodesTableClient episodeGuid with
            | Some item ->
                let name = item.Enclosure.Url |> string |> (fun x -> x.Replace(blobContainer.Uri |> string, ""))
                let! _ = blobContainer.DeleteBlobIfExistsAsync(name)
                ()
            | None -> ()
        let! _ = blobContainer.UploadBlobAsync(name, BinaryData.FromBytes b)

        let type' =
            match FileExtensionContentTypeProvider().TryGetContentType name with
            | true, ct -> ct
            | _ -> "unknown"

        let enc : Funcaster.Domain.Enclosure =
            {
                Url = blobContainer.GetBlobClient(name).Uri
                Type = type'
                Length = b.LongLength
            }

        return! enc |> PodcastStorage.updateEnclosure episodesTableClient episodeGuid
    }

let private uploaderService (blobContainer:BlobContainerClient) (episodesTableClient:TableClient) (ctx:HttpContext) =
    {
        UploadFile = uploadFile blobContainer episodesTableClient ctx >> Async.AwaitTask
    }

let episodesAPI : HttpHandler =
    Require.services<ILogger<_>, EpisodesTable> (fun logger (EpisodesTable tableClient) ->
        Remoting.createApi()
        |> Remoting.withRouteBuilder EpisodesAPI.RouteBuilder
        |> Remoting.fromValue (service tableClient)
        |> Remoting.withBinarySerialization
        |> Remoting.withErrorHandler (Remoting.errorHandler logger)
        |> Remoting.buildHttpHandler
    )

let episodesUploaderAPI : HttpHandler =
    Require.services<ILogger<_>, BlobContainerClient, EpisodesTable> (fun logger blobContainer (EpisodesTable tableClient) ->
        Remoting.createApi()
        |> Remoting.withRouteBuilder EpisodesUploaderAPI.RouteBuilder
        |> Remoting.fromContext (uploaderService blobContainer tableClient)
        |> Remoting.withBinarySerialization
        |> Remoting.withErrorHandler (Remoting.errorHandler logger)
        |> Remoting.buildHttpHandler
    )

