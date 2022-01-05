module FuncasterStudio.Client.Server

open Fable.Remoting.Client
open FuncasterStudio.Shared.Podcasts.API

type RemoteReadData<'a> =
    | Idle
    | InProgress
    | Finished of 'a

module RemoteReadData =
    let init = Idle
    let isInProgress = function
        | InProgress -> true
        | _ -> false
    let setInProgress = InProgress
    let setResponse r = Finished r

type RemoteData<'a,'b> = {
    Data : 'a
    Response : 'b option
    InProgress : bool
}

module RemoteData =
    let init data = { Data = data; Response = None; InProgress = false }
    let setData value t = { t with Data = value; InProgress = false }
    let getData t = t.Data
    let isInProgress = function
        | { InProgress = value } -> value
    let setInProgress t = { t with InProgress = true }
    let setResponse r t = { t with InProgress = false; Response = Some r }
    let clearResponse t = { t with Response = None }
    let hasResponse t = t.Response.IsSome

let podcastsAPI =
    Remoting.createApi()
    |> Remoting.withRouteBuilder PodcastsAPI.RouteBuilder
    |> Remoting.buildProxy<PodcastsAPI>