module FuncasterStudio.Client.Server

open Fable.Remoting.Client
open FuncasterStudio.Shared.Podcasts.API

let podcastsAPI =
    Remoting.createApi()
    |> Remoting.withRouteBuilder PodcastsAPI.RouteBuilder
    |> Remoting.buildProxy<PodcastsAPI>