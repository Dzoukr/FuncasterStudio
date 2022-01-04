module FuncasterStudio.Client.Pages.Podcast

open Browser.Types
open Fable.Remoting.Client
open Feliz
open Feliz.DaisyUI
open Feliz.DaisyUI.Operators
open Elmish
open Feliz.UseElmish
open FuncasterStudio.Client
open FuncasterStudio.Client.SharedView
open Fable.Core.JsInterop

type State = {
    LogoToUpload : File option
}

type Msg =
    | FileChosen of File
    | UploadLogo
    | FileUploaded

let init () = { LogoToUpload = None }, Cmd.none

let private uploadLogo (file:File) =
    async {
        let! fileBytes = file.ReadAsByteArray()
        let! output = Server.podcastsAPI.UploadLogo(fileBytes)
        return output
    }

let update (msg:Msg) (model:State) : State * Cmd<Msg> =
    match msg with
    | FileChosen file -> { model with LogoToUpload = Some file }, Cmd.none //Cmd.OfAsync.perform Server.service.GetMessage () MessageReceived
    | UploadLogo -> model, Cmd.OfAsync.perform uploadLogo model.LogoToUpload.Value (fun _ -> FileUploaded)
    | FileUploaded -> { model with LogoToUpload = None }, Cmd.none

[<ReactComponent>]
let PodcastView () =
    let state, dispatch = React.useElmish(init, update, [| |])

    Html.divClassed "grid grid-cols-12 gap-4 px-4 mt-4" [
        Html.divClassed "col-span-4" [
            Html.divClassed "text-center" [
                Html.img [ prop.src "https://picsum.photos/id/1005/400/250"; prop.className "w-full mb-2 rounded" ]
                Daisy.input [
                    prop.type'.file
                    prop.accept ".png"
                    prop.onInput (fun e ->
                        let file : File = e.target?files?(0)
                        file |> FileChosen |> dispatch
                    )
                ]
                if state.LogoToUpload.IsSome then
                    Daisy.button.button [
                        prop.text "Upload new logo"
                        button.secondary
                        prop.onClick (fun _ -> UploadLogo |> dispatch)
                    ]
            ]
        ]
        Html.divClassed "col-span-8" [
            Daisy.formControl [
                Daisy.label [ Daisy.labelText "Username" ]
                Daisy.input [ input.bordered; prop.placeholder "Username" ]
            ]
            Daisy.formControl [
                Daisy.label [ Daisy.labelText "Username" ]
                Daisy.input [ input.bordered; prop.placeholder "Username" ]
            ]
        ]
    ]