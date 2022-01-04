module FuncasterStudio.Server.Podcasts.API

open System
open Azure.Storage.Blobs
open Giraffe
open Giraffe.GoodRead
open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open FuncasterStudio.Shared.Podcasts.API
open FuncasterStudio.Server.BlobStorage

let private uploadLogo (podcast:PodcastBlobContainer) (content:byte []) =
    task {
        let client = podcast |> PodcastBlobContainer.client
        let! _ = client.UploadBlobAsync($"assets/logo.png", BinaryData.FromBytes content)
        return ()
    }

let private service (podcast:PodcastBlobContainer) = {
    UploadLogo = uploadLogo podcast >> Async.AwaitTask
}

let podcastsAPI : HttpHandler =
    Require.services<PodcastBlobContainer> (fun podcast ->
        Remoting.createApi()
        |> Remoting.withRouteBuilder PodcastsAPI.RouteBuilder
        |> Remoting.fromValue (service podcast)
        |> Remoting.buildHttpHandler
    )

