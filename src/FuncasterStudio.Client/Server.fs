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
    Data : 'a option
    Response : 'b option
    InProgress : bool
}

module RemoteData =
    let init = { Data = None; Response = None; InProgress = false }
    let setData value t = { t with Data = Some value; InProgress = false }
    let getData = function
        | { Data = Some data } -> data
        | _ -> failwith "Transfer has no data set"
    let isReady = function
        | { Data = Some _ } -> true
        | _ -> false
    let isInProgress = function
        | { InProgress = value } -> value
    let isReadyOrInProgress v = isReady v || isInProgress v
    let setInProgress t = { t with InProgress = true }
    let setResponse r t = { t with InProgress = false; Response = Some r }
    let hasResponse t = t.Response.IsSome

let podcastsAPI =
    Remoting.createApi()
    |> Remoting.withRouteBuilder PodcastsAPI.RouteBuilder
    |> Remoting.buildProxy<PodcastsAPI>