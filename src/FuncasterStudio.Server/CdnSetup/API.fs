module FuncasterStudio.Server.CdnSetup.API

open System
open Azure.Storage.Blobs
open FuncasterStudio.Shared.CdnSetup.API
open Funcaster.Domain
open Giraffe
open Giraffe.GoodRead
open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open FsToolkit.ErrorHandling
open Microsoft.Extensions.Logging

let private toSetup (d:Funcaster.Domain.CdnSetup) : FuncasterStudio.Shared.CdnSetup.API.CdnSetup =
    { IsEnabled = d.IsEnabled; CdnUrl = string d.CdnUrl }

let private fromSetup (d:FuncasterStudio.Shared.CdnSetup.API.CdnSetup) : Funcaster.Domain.CdnSetup =
    { IsEnabled = d.IsEnabled; CdnUrl = d.CdnUrl |> Uri }

let private getSetup setupTable () =
    task {
        match! Funcaster.Storage.getCdnSetup setupTable () with
        | Some c -> return c
        | None -> return CdnSetup.none
    }

let private saveSetup setupTable (s:CdnSetup) =
    task {
        return! s |> Funcaster.Storage.upsertCdnSetup setupTable
    }

let private service setupTable = {
    SaveSetup = fromSetup >> saveSetup setupTable >> Async.AwaitTask
    GetSetup = getSetup setupTable >> Task.map toSetup >> Async.AwaitTask
}

let cdnSetupAPI : HttpHandler =
    Require.services<ILogger<_>, Funcaster.Storage.CdnSetupTable> (fun logger setupTable ->
        Remoting.createApi()
        |> Remoting.withRouteBuilder CdnSetupAPI.RouteBuilder
        |> Remoting.fromValue (service setupTable)
        |> Remoting.withBinarySerialization
        |> Remoting.withErrorHandler (Remoting.errorHandler logger)
        |> Remoting.buildHttpHandler
    )

