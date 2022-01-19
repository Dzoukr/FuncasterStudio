module FuncasterStudio.Shared.CdnSetup.API

open System
open Aether
open FuncasterStudio.Shared.Validation

type CdnSetup = {
    IsEnabled : bool
    CdnUrl : string
}

module CdnSetup =
    let init = { IsEnabled = false; CdnUrl = "" }
    let cdnUrl = NamedLens.create "CDN Url" (fun x -> x.CdnUrl) (fun x v -> { v with CdnUrl = x })
    let enabled = NamedLens.create "Is Enabled" (fun x -> x.IsEnabled) (fun x v -> { v with IsEnabled = x })

    let validate =
        rules [
            check cdnUrl Validator.isUri
        ]


type CdnSetupAPI = {
    SaveSetup : CdnSetup -> Async<unit>
    GetSetup : unit -> Async<CdnSetup>
}
with
    static member RouteBuilder _ m = sprintf "/api/cdnsetup/%s" m