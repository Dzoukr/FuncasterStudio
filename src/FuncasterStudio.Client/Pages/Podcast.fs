﻿module FuncasterStudio.Client.Pages.Podcast

open System
open Aether
open Browser.Types
open Fable.Remoting.Client
open Feliz
open Feliz.DaisyUI
open Elmish
open Feliz.UseElmish
open FuncasterStudio.Client.Server
open FuncasterStudio.Client.SharedView
open FuncasterStudio.Shared.Podcasts.API
open Fable.Core.JsInterop
open FuncasterStudio.Shared.Validation

type State = {
    Logo : RemoteReadData<string>
    LogoUpload : RemoteData<File option, unit>
    Podcast : RemoteReadData<Channel>
    PodcastForm : RemoteData<Channel,unit>
    PodcastFormErrors : ValidationError list
}

type Msg =
    | LogoFileChosen of File
    | UploadLogo
    | LogoUploaded of unit
    | LoadLogo
    | LogoLoaded of string
    | LoadPodcast
    | PodcastLoaded of Channel
    | PodcastChanged of Channel
    | SendPodcast
    | PodcastSent of unit

let init () =
    {
        Logo = RemoteReadData.init
        LogoUpload = RemoteData.init None
        Podcast = RemoteReadData.init
        PodcastForm = RemoteData.init Channel.init
        PodcastFormErrors = []
    }, Cmd.batch ([ LoadPodcast; LoadLogo ] |> List.map Cmd.ofMsg)

let private uploadLogo (file:File) =
    async {
        let! fileBytes = file.ReadAsByteArray()
        let! output = podcastsAPI.UploadLogo(fileBytes)
        return output
    }

let private withTS (s:string) =
    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
    |> string
    |> (fun x -> $"{s}?{x}")

let update (msg:Msg) (model:State) : State * Cmd<Msg> =
    match msg with
    | LogoFileChosen file -> { model with LogoUpload = model.LogoUpload |> RemoteData.setData (Some file) }, Cmd.none
    | UploadLogo ->
        let cmd =
            match model.LogoUpload.Data with
            | Some data -> Cmd.OfAsync.perform uploadLogo data LogoUploaded
            | None -> Cmd.none
        { model with LogoUpload = model.LogoUpload |> RemoteData.setInProgress }, cmd
    | LogoUploaded _ -> { model with LogoUpload = RemoteData.init None }, Cmd.ofMsg LoadLogo
    | LoadLogo -> { model with Logo = RemoteReadData.setInProgress }, Cmd.OfAsync.perform podcastsAPI.GetLogo () LogoLoaded
    | LogoLoaded logo -> { model with Logo = RemoteReadData.setResponse (withTS logo) }, Cmd.none
    | LoadPodcast -> { model with Podcast = RemoteReadData.setInProgress }, Cmd.OfAsync.perform podcastsAPI.GetPodcast () PodcastLoaded
    | PodcastLoaded channel ->
        { model with
            Podcast = RemoteReadData.Finished channel
            PodcastForm = model.PodcastForm |> RemoteData.setData channel
            PodcastFormErrors = channel |> Channel.validate }, Cmd.none
    | PodcastChanged channel -> { model with PodcastForm = model.PodcastForm |> RemoteData.setData channel; PodcastFormErrors = channel |> Channel.validate }, Cmd.none
    | SendPodcast ->
        model, Cmd.none

let textInput (data:'a) (onDataChanged:'a -> unit) (errors:ValidationError list) (n:NamedLens<'a,string>) =
    let value = data |> Optic.get n.Lens
    let err = errors |> ValidationError.get n
    Daisy.formControl [
        Daisy.label [ Daisy.labelText n.Name ]
        Daisy.input [
            input.bordered
            if err.IsSome then input.error
            prop.valueOrDefault value
            prop.onTextChange (fun t -> data |> Optic.set n.Lens t |> onDataChanged)
            prop.placeholder n.Name
        ]
        match err with
        | Some e -> Daisy.label [ Daisy.labelTextAlt [ prop.text (ValidationErrorType.explain e); color.textError ] ]
        | None -> Html.none
    ]

[<ReactComponent>]
let PodcastView () =
    let state, dispatch = React.useElmish(init, update, [|  |])

    let ti = textInput state.PodcastForm.Data (PodcastChanged >> dispatch) state.PodcastFormErrors

    Html.divClassed "grid grid-cols-12 gap-4 px-4 mt-4" [
        Html.divClassed "col-span-4" [
            Html.divClassed "text-center" [
                Html.img [
                    yield!
                        match state.Logo with
                        | Finished logo -> [ prop.src logo ]
                        | _ -> []
                    prop.className "w-full mb-2 rounded"
                ]
                Daisy.input [
                    prop.type'.file
                    prop.accept ".png"
                    prop.onInput (fun e ->
                        let file : File = e.target?files?(0)
                        file |> LogoFileChosen |> dispatch
                    )
                ]
                if state.LogoUpload.Data.IsSome || state.LogoUpload.InProgress then
                    Daisy.button.button [
                        prop.text "Upload new logo"
                        button.secondary
                        if state.LogoUpload.InProgress then button.loading
                        prop.onClick (fun _ -> UploadLogo |> dispatch)
                    ]
            ]
        ]
        Html.divClassed "col-span-8" [
            ti Channel.title
            ti Channel.link
            ti Channel.description

            Daisy.label [  ]
            Daisy.button.button [
                prop.text "Update"
                prop.disabled (state.PodcastFormErrors |> List.isEmpty |> not)
                button.primary

                prop.onClick (fun _ -> SendPodcast |> dispatch)
            ]
        ]
    ]