module FuncasterStudio.Client.Pages.Podcast

open System
open Aether
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
    Logo : RemoteReadData<string>
    LogoUpload : RemoteData<File, unit>
    Podcast : RemoteReadData<Channel>
    PodcastForm : RemoteData<Channel,unit>
}

type Msg =
    | LogoFileChosen of File
    | UploadLogo
    | LogoUploaded
    | LoadLogo
    | LogoLoaded of string
    | LoadPodcast
    | PodcastLoaded of Channel
    | PodcastChanged of Channel

let init () =
    {
        Logo = RemoteReadData.init
        LogoUpload = RemoteData.init
        Podcast = RemoteReadData.init
        PodcastForm = RemoteData.init |> RemoteData.setData Channel.init
    }, Cmd.batch ([ LoadPodcast; LoadLogo ] |> List.map Cmd.ofMsg)

let private uploadLogo (file:File) =
    async {
        let! fileBytes = file.ReadAsByteArray()
        let! output = podcastsAPI.UploadLogo(fileBytes)
        return output
    }

let update (msg:Msg) (model:State) : State * Cmd<Msg> =
    match msg with
    | LogoFileChosen file -> { model with LogoUpload = model.LogoUpload |> RemoteData.setData file }, Cmd.none
    | UploadLogo ->
        let cmd =
            match model.LogoUpload.Data with
            | Some data -> Cmd.OfAsync.perform uploadLogo data (fun _ -> LogoUploaded)
            | None -> Cmd.none
        { model with LogoUpload = model.LogoUpload |> RemoteData.setInProgress }, cmd
    | LogoUploaded -> { model with LogoUpload = RemoteData.init }, Cmd.ofMsg LoadLogo
    | LoadLogo -> { model with Logo = RemoteReadData.setInProgress }, Cmd.OfAsync.perform podcastsAPI.GetLogo () LogoLoaded
    | LogoLoaded logo -> { model with Logo = RemoteReadData.setResponse logo }, Cmd.none
    | LoadPodcast -> { model with Podcast = RemoteReadData.setInProgress }, Cmd.OfAsync.perform podcastsAPI.GetPodcast () PodcastLoaded
    | PodcastLoaded channel -> { model with Podcast = RemoteReadData.Finished channel; PodcastForm = model.PodcastForm |> RemoteData.setData channel }, Cmd.none
    | PodcastChanged channel -> { model with PodcastForm = model.PodcastForm |> RemoteData.setData channel }, Cmd.none

let private withTS (s:string) =
    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
    |> string
    |> (fun x -> $"{s}?{x}")

let textInput (data:'a) (onDataChanged:'a -> unit) (n:NamedLens<'a,string>) =
    let value = data |> Optic.get n.Lens
    Daisy.formControl [
        Daisy.label [ Daisy.labelText n.Name ]
        Daisy.input [
            input.bordered
            prop.valueOrDefault value
            prop.onTextChange (fun t -> data |> Optic.set n.Lens t |> onDataChanged)
            prop.placeholder n.Name
        ]
    ]

[<ReactComponent>]
let PodcastView () =
    let state, dispatch = React.useElmish(init, update, [|  |])

    //let ti = textInput (state.Podcast |> RemoteData.

    Html.divClassed "grid grid-cols-12 gap-4 px-4 mt-4" [
        Html.divClassed "col-span-4" [
            Html.divClassed "text-center" [
                Html.img [
                    yield!
                        match state.Logo with
                        | Finished logo -> [ prop.src (withTS logo) ]
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