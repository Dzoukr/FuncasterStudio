module FuncasterStudio.Client.Pages.CdnSetup

open Elmish
open Feliz
open Feliz.UseElmish
open Feliz.DaisyUI
open Funcaster.Domain
open FuncasterStudio.Client.Server
open FuncasterStudio.Client.SharedView
open FuncasterStudio.Shared.Validation
open FuncasterStudio.Shared.CdnSetup.API
open FuncasterStudio.Client.Forms

type State = {
    CdnSetup : RemoteReadData<CdnSetup>
    CdnSetupForm : RemoteData<CdnSetup, unit, ValidationError>
}

type Msg =
    | LoadSetup
    | SetupLoaded of ServerResult<CdnSetup>
    | CdnSetupFormChanged of CdnSetup
    | SaveSetup
    | SetupSaved of ServerResult<unit>

let init () = { CdnSetup = RemoteReadData.init; CdnSetupForm = RemoteData.init CdnSetup.init CdnSetup.validate }, Cmd.ofMsg LoadSetup

let update (msg:Msg) (state:State) : State * Cmd<Msg> =
    match msg with
    | LoadSetup -> { state with CdnSetup = RemoteReadData.setInProgress }, Cmd.OfAsync.eitherAsResult (fun _ -> cdnSetupAPI.GetSetup ()) SetupLoaded
    | SetupLoaded i ->
        match i with
        | Ok setup -> { state with CdnSetup = RemoteReadData.setResponse setup; CdnSetupForm = state.CdnSetupForm |> RemoteData.setData setup CdnSetup.validate }, Cmd.none
        | Error e -> { state with CdnSetup = RemoteReadData.init }, ToastView.Cmd.ofError e
    | CdnSetupFormChanged form -> { state with CdnSetupForm = state.CdnSetupForm |> RemoteData.setData form CdnSetup.validate }, Cmd.none
    | SaveSetup ->
        { state with CdnSetupForm = state.CdnSetupForm |> RemoteData.setInProgress }, Cmd.OfAsync.eitherAsResult (fun _ -> cdnSetupAPI.SaveSetup state.CdnSetupForm.Data) SetupSaved
    | SetupSaved res ->
        let cmd = res |> ToastView.Cmd.ofResult "CDN Setup successfully saved"
        { state with
            CdnSetupForm =
                state.CdnSetupForm
                |> RemoteData.setResponse ()
                |> RemoteData.applyValidationErrors (res |> ServerResult.getValidationErrors) }, cmd

[<ReactComponent>]
let CdnSetupView () =
    let state, dispatch = React.useElmish(init, update, [|  |])

    let ti = textInput state.CdnSetupForm (CdnSetupFormChanged >> dispatch)
    let ci = checkboxInput state.CdnSetupForm (CdnSetupFormChanged >> dispatch)

    Html.divClassed "grid grid-cols-12 gap-4" [
        Html.divClassed "col-span-5" [
            ti CdnSetup.cdnUrl
        ]
        Html.divClassed "col-span-5" [
            ci CdnSetup.enabled
        ]
        Html.divClassed "col-span-2" [
            Daisy.label []
            Daisy.button.button [
                prop.text "Update CDN setup"
                prop.disabled (state.CdnSetupForm |> RemoteData.isNotValid)
                button.primary
                if state.CdnSetupForm.InProgress then button.loading
                prop.onClick (fun _ -> SaveSetup |> dispatch)
            ]
        ]
    ]