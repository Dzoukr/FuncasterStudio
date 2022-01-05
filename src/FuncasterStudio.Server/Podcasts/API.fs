module FuncasterStudio.Server.Podcasts.API

open System
open Giraffe
open Giraffe.GoodRead
open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open FuncasterStudio.Shared.Podcasts.API
open FuncasterStudio.Server.BlobStorage

let private uploadLogo (podcast:PodcastBlobContainer) (content:byte []) =
    task {
        let name = "assets/logo.png"
        let client = podcast |> PodcastBlobContainer.client
        let! _ = client.DeleteBlobIfExistsAsync(name)
        let! _ = client.UploadBlobAsync(name, BinaryData.FromBytes content)
        return client.GetBlobClient(name).Uri |> string
    }

let getPodcast () =
    task {
        return
            {
                Title = ""
                Link = "https://example.com"
                Description = ""
                Language = None
                Author = ""
                Owner = { Name = ""; Email = "" }
                Explicit = false
                Image = "https://example.com"
                Category = None
                Type = ChannelType.Episodic
                Restrictions = [] }

    }

let private service (podcast:PodcastBlobContainer) = {
    UploadLogo = uploadLogo podcast >> Async.AwaitTask
    GetPodcast = getPodcast >> Async.AwaitTask
}

let podcastsAPI : HttpHandler =
    Require.services<PodcastBlobContainer> (fun podcast ->
        Remoting.createApi()
        |> Remoting.withRouteBuilder PodcastsAPI.RouteBuilder
        |> Remoting.fromValue (service podcast)
        |> Remoting.buildHttpHandler
    )

