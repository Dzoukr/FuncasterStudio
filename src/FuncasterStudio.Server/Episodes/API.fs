module FuncasterStudio.Server.Episodes.API

open System
open System.IO
open Azure.Data.Tables
open Azure.Storage.Blobs
open Funcaster.Domain
open FuncasterStudio.Server
open FuncasterStudio.Server.Errors
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

let private getEpisodes episodesTable () =
    task {
        let! eps = Funcaster.Storage.getEpisodes episodesTable ()
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

let private getEpisode episodesTable (guid:string) =
    task {
        let! ep = Funcaster.Storage.getEpisode episodesTable guid
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

let private createEpisode episodesTable (c:Episode) =
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
            item |> Funcaster.Storage.upsertEpisode episodesTable
    }

let private updateEpisode episodesTable (c:Episode) =
    task {
        match! Funcaster.Storage.getEpisode episodesTable c.Guid with
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
                |> Funcaster.Storage.upsertEpisode episodesTable
        | None -> return ()
    }

let private getEnclosureLocalUrl (blobContainer:BlobContainerClient) (enc:Enclosure) =
    enc.Url |> string |> (fun x -> x.Replace(blobContainer.Uri |> string, ""))

let private deleteEpisode (blobContainer:BlobContainerClient) episodesTable (guid:string) =
    task {
        match! Funcaster.Storage.getEpisode episodesTable guid with
        | Some item ->
            // delete episode
            let! _ = Funcaster.Storage.deleteEpisode episodesTable guid
            let! _ = blobContainer.DeleteBlobIfExistsAsync(item.Enclosure |> getEnclosureLocalUrl blobContainer)
            return ()
        | None -> return ()
    }

let private validateUnique episodesTable (ep:Episode) =
    task {
        let! item = Funcaster.Storage.getEpisode episodesTable ep.Guid
        if item.IsSome then return [{ Key = Episode.guid.Name; Message = ValidationErrorType.MustBeUnique }]
        else return []
    }

let private service (blobContainer:BlobContainerClient) episodesTable = {
    CreateEpisode =
        ServerError.validate Episode.validate
        >> ServerError.validateAsync (validateUnique episodesTable)
        >> Task.bind (createEpisode episodesTable)
        >> Async.AwaitTask
    UpdateEpisode =
        ServerError.validate Episode.validate
        >> updateEpisode episodesTable >> Async.AwaitTask
    GetEpisodes = getEpisodes episodesTable >> Async.AwaitTask
    GetEpisode = getEpisode episodesTable >> Async.AwaitTask
    DeleteEpisode = deleteEpisode blobContainer episodesTable >> Async.AwaitTask
}

let private uploadFile (blobContainer:BlobContainerClient) episodesTable (ctx:HttpContext) (b:byte []) =
    task {
        let originalName = ctx.Request.Headers.["filename"].[0]
        let episodeGuid = ctx.Request.Headers.["episodeGuid"].[0] |> Funcaster.Storage.key
        let season = ctx.Request.Headers.["season"].[0] |> int
        let deleteFile = ctx.Request.Headers.["replaceoldfile"].Count > 0
        let name = $"episodes/s{season}/{episodeGuid}{Path.GetExtension originalName}"
        if deleteFile then
            match! Funcaster.Storage.getEpisode episodesTable episodeGuid with
            | Some item ->
                let name = getEnclosureLocalUrl blobContainer item.Enclosure
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

        return! enc |> Funcaster.Storage.updateEnclosure episodesTable episodeGuid
    }

let private uploaderService (blobContainer:BlobContainerClient) episodesTable (ctx:HttpContext) =
    {
        UploadFile = uploadFile blobContainer episodesTable ctx >> Async.AwaitTask
    }

let episodesAPI : HttpHandler =
    Require.services<ILogger<_>, BlobContainerClient, Funcaster.Storage.EpisodesTable> (fun logger blobContainer tableClient ->
        Remoting.createApi()
        |> Remoting.withRouteBuilder EpisodesAPI.RouteBuilder
        |> Remoting.fromValue (service blobContainer tableClient)
        |> Remoting.withBinarySerialization
        |> Remoting.withErrorHandler (Remoting.errorHandler logger)
        |> Remoting.buildHttpHandler
    )

let episodesUploaderAPI : HttpHandler =
    Require.services<ILogger<_>, BlobContainerClient, Funcaster.Storage.EpisodesTable> (fun logger blobContainer tableClient ->
        Remoting.createApi()
        |> Remoting.withRouteBuilder EpisodesUploaderAPI.RouteBuilder
        |> Remoting.fromContext (uploaderService blobContainer tableClient)
        |> Remoting.withBinarySerialization
        |> Remoting.withErrorHandler (Remoting.errorHandler logger)
        |> Remoting.buildHttpHandler
    )

