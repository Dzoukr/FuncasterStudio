module FuncasterStudio.Client.Server

open Fable.Remoting.Client
open FuncasterStudio.Shared.Episodes.API
open FuncasterStudio.Shared.Errors
open FuncasterStudio.Shared.Podcasts.API
open Fable.SimpleJson

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
    let isInProgress t = t.InProgress
    let setInProgress t = { t with InProgress = true }
    let setResponse r t = { t with InProgress = false; Response = Some r }
    let clearResponse t = { t with Response = None }
    let hasResponse t = t.Response.IsSome

let exnToError (e:exn) : ServerError =
    match e with
    | :? ProxyRequestException as ex ->
        try
            let serverError = Json.parseAs<{| error: ServerError |}>(ex.Response.ResponseBody)
            serverError.error
        with _ -> ServerError.Exception(e.Message)
    | _ -> ServerError.Exception(e.Message)

type ServerResult<'a> = Result<'a,ServerError>

module Cmd =
    open Elmish

    module OfAsync =
        let eitherAsResult fn resultMsg =
            Cmd.OfAsync.either fn () (Result.Ok >> resultMsg) (exnToError >> Result.Error >> resultMsg)


let podcastsAPI =
    Remoting.createApi()
    |> Remoting.withRouteBuilder PodcastsAPI.RouteBuilder
    |> Remoting.withBinarySerialization
    |> Remoting.buildProxy<PodcastsAPI>

let episodesAPI =
    Remoting.createApi()
    |> Remoting.withRouteBuilder EpisodesAPI.RouteBuilder
    |> Remoting.buildProxy<EpisodesAPI>