module FuncasterStudio.Client.Pages.Podcast

open System
open Browser.Types
open Fable.Core.JS
open Fable.Remoting.Client
open Feliz
open Feliz.DaisyUI
open Elmish
open Feliz.UseElmish
open FuncasterStudio.Client
open FuncasterStudio.Client.Server
open FuncasterStudio.Client.SharedView
open FuncasterStudio.Shared.Podcasts.API
open Fable.Core.JsInterop

type State = {
    LogoUpload : RemoteData<File, string>
    Podcast : RemoteReadData<Channel>
}

type Msg =
    | FileChosen of File
    | UploadLogo
    | FileUploaded of string
    | LoadPodcast
    | PodcastLoaded of Channel

let init () =
    { LogoUpload = RemoteData.init; Podcast = RemoteReadData.init }, Cmd.ofMsg LoadPodcast

let private uploadLogo (file:File) =
    async {
        let! fileBytes = file.ReadAsByteArray()
        let! output = podcastsAPI.UploadLogo(fileBytes)
        return output
    }

let update (msg:Msg) (model:State) : State * Cmd<Msg> =
    match msg with
    | FileChosen file -> { model with LogoUpload = model.LogoUpload |> RemoteData.setData file }, Cmd.none
    | UploadLogo -> { model with LogoUpload = model.LogoUpload |> RemoteData.setInProgress }, Cmd.OfAsync.perform uploadLogo (RemoteData.getData model.LogoUpload) FileUploaded
    | FileUploaded uri -> { model with LogoUpload = model.LogoUpload |> RemoteData.setResponse uri }, Cmd.none
    | LoadPodcast -> { model with Podcast = RemoteReadData.setInProgress }, Cmd.OfAsync.perform podcastsAPI.GetPodcast () PodcastLoaded
    | PodcastLoaded channel -> { model with Podcast = RemoteReadData.Finished channel }, Cmd.none

let private withTS (s:string) =
    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
    |> string
    |> (fun x -> $"{s}?{x}")

[<ReactComponent>]
let PodcastView () =
    let state, dispatch = React.useElmish(init, update, [|  |])


    Html.divClassed "grid grid-cols-12 gap-4 px-4 mt-4" [
        Html.divClassed "col-span-4" [
            Html.divClassed "text-center" [
                Html.img [
                    prop.src (state.LogoUpload.Response |> Option.map withTS |> Option.defaultValue "https://picsum.photos/id/1005/400/250")
                    prop.className "w-full mb-2 rounded"
                ]
                Daisy.input [
                    prop.type'.file
                    prop.accept ".png"
                    prop.onInput (fun e ->
                        let file : File = e.target?files?(0)
                        file |> FileChosen |> dispatch
                    )
                ]
                if state.LogoUpload |> RemoteData.isReadyOrInProgress then
                    Daisy.button.button [
                        prop.text "Upload new logo"
                        button.secondary
                        if state.LogoUpload |> RemoteData.isInProgress then button.loading
                        prop.onClick (fun _ -> UploadLogo |> dispatch)
                    ]
            ]
        ]
        Html.divClassed "col-span-8" [
            match state.Podcast with
            | Finished channel ->
                Daisy.formControl [
                    Daisy.label [ Daisy.labelText "Title" ]
                    Daisy.input [ input.bordered; prop.valueOrDefault channel.Title; prop.placeholder "Title" ]
                ]
                Daisy.formControl [
                    Daisy.label [ Daisy.labelText "Link" ]
                    Daisy.input [ input.bordered; prop.placeholder "Link" ]
                ]
            | _ -> Html.none
        ]
    ]