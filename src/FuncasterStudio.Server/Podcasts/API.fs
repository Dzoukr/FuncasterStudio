module FuncasterStudio.Server.Podcasts.API

open System
open Azure.Storage.Blobs
open Giraffe
open Giraffe.GoodRead
open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open FuncasterStudio.Shared.Podcasts.API
open FsToolkit.ErrorHandling
open Microsoft.Extensions.Logging

let private logoName = "assets/logo.png"

let private uploadLogo (blobContainer:BlobContainerClient) (content:byte []) =
    task {
        let! _ = blobContainer.DeleteBlobIfExistsAsync(logoName)
        let! _ = blobContainer.UploadBlobAsync(logoName, BinaryData.FromBytes content)
        return ()
    }

let private getLogo (blobContainer:BlobContainerClient) () =
    task {
        return blobContainer.GetBlobClient(logoName).Uri |> string
    }

let getPodcast podcastTable () =
    task {
        match! Funcaster.Storage.getPodcast podcastTable () with
        | Some c ->
            return
                {
                    Title = c.Title
                    Link = c.Link |> string
                    Description = c.Description
                    Language = c.Language
                    Author = c.Author
                    OwnerName = c.Owner.Name
                    OwnerEmail = c.Owner.Email
                    Explicit = c.Explicit
                    Category = c.Category
                    Type = c.Type
                    Restrictions = c.Restrictions
                }
        | None -> return Channel.init
    }

let savePodcast (blobContainer:BlobContainerClient) podcastTable (c:Channel) =
    task {
        let! logo = getLogo blobContainer ()
        let channel : Funcaster.Domain.Channel =
            {
                Title = c.Title
                Link = c.Link |> Uri
                Description = c.Description
                Language = c.Language
                Author = c.Author
                Owner = { Name = c.OwnerName; Email = c.OwnerEmail }
                Explicit = c.Explicit
                Image = logo |> Uri
                Category = c.Category
                Type = c.Type
                Restrictions = c.Restrictions
            }
        return!
            channel |> Funcaster.Storage.upsertPodcast podcastTable
    }

let private service (blobContainer:BlobContainerClient) podcastTable = {
    GetLogo = getLogo blobContainer >> Async.AwaitTask
    UploadLogo = uploadLogo blobContainer >> Async.AwaitTask
    GetPodcast = getPodcast podcastTable >> Async.AwaitTask
    SavePodcast = savePodcast blobContainer podcastTable >> Async.AwaitTask
}

let podcastsAPI : HttpHandler =
    Require.services<ILogger<_>, BlobContainerClient, Funcaster.Storage.PodcastTable> (fun logger blobContainer podcastTable ->
        Remoting.createApi()
        |> Remoting.withRouteBuilder PodcastsAPI.RouteBuilder
        |> Remoting.fromValue (service blobContainer podcastTable)
        |> Remoting.withBinarySerialization
        |> Remoting.withErrorHandler (Remoting.errorHandler logger)
        |> Remoting.buildHttpHandler
    )

