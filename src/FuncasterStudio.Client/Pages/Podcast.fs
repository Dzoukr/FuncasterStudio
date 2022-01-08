module FuncasterStudio.Client.Pages.Podcast

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
open FuncasterStudio.Client.Forms
open FuncasterStudio.Shared.Podcasts.API
open Fable.Core.JsInterop
open FuncasterStudio.Shared.Validation

type State = {
    Logo : RemoteReadData<string>
    LogoUpload : RemoteData<File option, unit, unit>
    Podcast : RemoteReadData<Channel>
    PodcastForm : RemoteData<Channel, unit, ValidationError>
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
    | SavePodcast
    | PodcastSaved of unit

let init () =
    {
        Logo = RemoteReadData.init
        LogoUpload = RemoteData.init None RemoteData.noValidation
        Podcast = RemoteReadData.init
        PodcastForm = RemoteData.init Channel.init Channel.validate
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
    | LogoFileChosen file -> { model with LogoUpload = model.LogoUpload |> RemoteData.setData (Some file) RemoteData.noValidation }, Cmd.none
    | UploadLogo ->
        let cmd =
            match model.LogoUpload.Data with
            | Some data -> Cmd.OfAsync.perform uploadLogo data LogoUploaded
            | None -> Cmd.none
        { model with LogoUpload = model.LogoUpload |> RemoteData.setInProgress }, cmd
    | LogoUploaded _ -> { model with LogoUpload = RemoteData.init None RemoteData.noValidation }, Cmd.ofMsg LoadLogo
    | LoadLogo -> { model with Logo = RemoteReadData.setInProgress }, Cmd.OfAsync.perform podcastsAPI.GetLogo () LogoLoaded
    | LogoLoaded logo -> { model with Logo = RemoteReadData.setResponse (withTS logo) }, Cmd.none
    | LoadPodcast -> { model with Podcast = RemoteReadData.setInProgress }, Cmd.OfAsync.perform podcastsAPI.GetPodcast () PodcastLoaded
    | PodcastLoaded channel ->
        { model with
            Podcast = RemoteReadData.Finished channel
            PodcastForm = model.PodcastForm |> RemoteData.setData channel Channel.validate }, Cmd.none
    | PodcastChanged channel -> { model with PodcastForm = model.PodcastForm |> RemoteData.setData channel Channel.validate }, Cmd.none
    | SavePodcast -> { model with PodcastForm = model.PodcastForm |> RemoteData.setInProgress }, Cmd.OfAsync.perform podcastsAPI.SavePodcast model.PodcastForm.Data PodcastSaved
    | PodcastSaved _ -> { model with PodcastForm = model.PodcastForm |> RemoteData.setResponse () }, Cmd.none


open Funcaster.Domain

[<ReactComponent>]
let PodcastView () =
    let state, dispatch = React.useElmish(init, update, [|  |])

    let ti = textInput state.PodcastForm (PodcastChanged >> dispatch)
    let lti = textAreaInput state.PodcastForm (PodcastChanged >> dispatch)
    let si = selectInput state.PodcastForm (PodcastChanged >> dispatch)
    let ci = checkboxInput state.PodcastForm (PodcastChanged >> dispatch)

    Html.divClassed "grid grid-cols-12 gap-4" [
        Html.divClassed "col-span-4 text-center" [
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
        Html.divClassed "col-span-8" [

            Html.divClassed "grid grid-cols-2 gap-4" [
                Html.div [
                    ti Channel.title
                    lti Channel.description
                    ti Channel.author
                    ti Channel.category
                    si (Compose.namedLens Channel.type' (ChannelType.value, ChannelType.create)) ([ Serial; Episodic ] |> List.map ChannelType.value)
                ]
                Html.div [
                    ti Channel.link
                    ti Channel.ownerName
                    ti Channel.ownerEmail
                    ti Channel.language
                    ci Channel.explicit
                    ti (Compose.namedLens Channel.restrictions (StringConversions.fromList, StringConversions.toList))
                ]
            ]

            Daisy.label [  ]
            Daisy.button.button [
                prop.text "Update"
                prop.disabled (state.PodcastForm |> RemoteData.isNotValid)
                button.primary
                if state.PodcastForm.InProgress then button.loading
                prop.onClick (fun _ -> SavePodcast |> dispatch)
            ]
        ]
    ]