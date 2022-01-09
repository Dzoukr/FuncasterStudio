module FuncasterStudio.Server.Episodes.API

open System
open System.IO
open Azure.Data.Tables
open Azure.Storage.Blobs
open FuncasterStudio.Server
open FuncasterStudio.Server.PodcastStorage
open FuncasterStudio.Shared.Episodes.API
open FuncasterStudio.Shared.Errors
open Giraffe
open Giraffe.GoodRead
open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open FsToolkit.ErrorHandling
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.StaticFiles
open Microsoft.Extensions.Logging


//let private uploadLogo (blobContainer:BlobContainerClient) (content:byte []) =
//    task {
//        let! _ = blobContainer.DeleteBlobIfExistsAsync(logoName)
//        let! _ = blobContainer.UploadBlobAsync(logoName, BinaryData.FromBytes content)
//        return ()
//    }
//
//let private getLogo (blobContainer:BlobContainerClient) () =
//    task {
//        return blobContainer.GetBlobClient(logoName).Uri |> string
//    }

//let getPodcast (podcastTableClient:TableClient) () =
//    task {
//        match! PodcastStorage.getPodcast podcastTableClient () with
//        | Some c ->
//            return
//                {
//                    Title = c.Title
//                    Link = c.Link |> string
//                    Description = c.Description
//                    Language = c.Language
//                    Author = c.Author
//                    OwnerName = c.Owner.Name
//                    OwnerEmail = c.Owner.Email
//                    Explicit = c.Explicit
//                    Category = c.Category
//                    Type = c.Type
//                    Restrictions = c.Restrictions
//                }
//        | None -> return Channel.init
//    }

let getEpisodes (episodesTableClient:TableClient) () =
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

let createEpisode (episodesTableClient:TableClient) (c:Episode) =
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

let private service (blobContainer:BlobContainerClient) (episodesTableClient:TableClient) = {
    CreateEpisode =
        ServerError.validate Episode.validate
        //>> ServerError.validate TODO_UNIQUE
        >> createEpisode episodesTableClient >> Async.AwaitTask
    GetEpisodes = getEpisodes episodesTableClient >> Async.AwaitTask

}

let private uploadFile (blobContainer:BlobContainerClient) (episodesTableClient:TableClient) (ctx:HttpContext) (b:byte []) =
    task {
        let originalName = ctx.Request.Headers.["filename"].[0]
        let episodeGuid = ctx.Request.Headers.["episodeGuid"].[0]
        let season = ctx.Request.Headers.["season"].[0] |> int
        let name = $"episodes/s{season}/{episodeGuid}{Path.GetExtension originalName}"
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
    Require.services<ILogger<_>, BlobContainerClient, EpisodesTable> (fun logger blobContainer (EpisodesTable tableClient) ->
        Remoting.createApi()
        |> Remoting.withRouteBuilder EpisodesAPI.RouteBuilder
        |> Remoting.fromValue (service blobContainer tableClient)
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

