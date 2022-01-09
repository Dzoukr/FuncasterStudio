﻿module FuncasterStudio.Client.Pages.EpisodesForm

open System
open Aether
open Browser.Types
open Fable.Core
open Fable.Remoting.Client
open Feliz
open Feliz.DaisyUI
open Elmish
open Feliz.UseElmish
open Funcaster.Domain
open FuncasterStudio.Client
open FuncasterStudio.Client.Server
open FuncasterStudio.Client.SharedView
open FuncasterStudio.Client.Forms
open FuncasterStudio.Shared.Episodes.API
open Fable.Core.JsInterop
open FuncasterStudio.Shared.Errors
open FuncasterStudio.Shared.Validation

type State = {
    Guid : string option
    Episode : RemoteReadData<Episode>
    EpisodeForm : RemoteData<Episode,unit,ValidationError>
    FileForm : RemoteData<File option,unit,ValidationError>
    LogoForm : RemoteData<File option,unit,ValidationError>
}

type Msg =
    | LoadEpisode of guid:string
    | EpisodeLoaded of Episode
    | EpisodeFormChanged of Episode
    | FileFormChanged of File option
    | LogoFormChanged of File option
    | SaveEpisode
    | EpisodeSaved of ServerResult<unit>

let private fileLens = NamedLens.create "Audio File" id (fun (x:File option) (v:File option) -> x )
let private logoLens = NamedLens.create "Logo" id (fun (x:File option) (v:File option) -> x )

let private validateFile =
    rules [
        check fileLens (Option.map (fun x -> x.name) >> Option.defaultValue "" >> Validator.isNotEmpty)
    ]

let init (guid:string option) =
    {
        Guid = guid
        Episode = RemoteReadData.init
        EpisodeForm = RemoteData.init Episode.init Episode.validate
        FileForm = RemoteData.init None validateFile
        LogoForm = RemoteData.init None RemoteData.noValidation
    },
        match guid with
        | Some i -> Cmd.batch [Cmd.ofMsg (LoadEpisode i) ]
        | None -> Cmd.none

let private readAsByteOpt (file:File option) =
    async {
        let! data =
            match file with
            | Some f -> f.ReadAsByteArray()
            | None -> async.Return [||]
        if data.Length > 0 then
            return {| Data = data; Name = file.Value.name |} |> Some
        else return None
    }

let private readAsByte (file:File) =
    async {
        let! d = file |> Some |> readAsByteOpt
        return d.Value
    }

let update (msg:Msg) (state:State) : State * Cmd<Msg> =
    match msg with
    | LoadEpisode i -> state, Cmd.none
    | EpisodeLoaded i -> state, Cmd.none
    | EpisodeFormChanged form -> { state with EpisodeForm = state.EpisodeForm |> RemoteData.setData form Episode.validate }, Cmd.none
    | FileFormChanged file -> { state with FileForm = state.FileForm |> RemoteData.setData file validateFile }, Cmd.none
    | LogoFormChanged file -> { state with LogoForm = state.LogoForm |> RemoteData.setData file RemoteData.noValidation }, Cmd.none
    | SaveEpisode ->
        let save () =
            async {
                let! formSaved = episodesAPI.CreateEpisode state.EpisodeForm.Data
                let! f = readAsByte state.FileForm.Data.Value
                let! fileUploaded =
                    (episodesUploaderAPI [
                        ("filename",f.Name)
                        ("episodeguid",state.EpisodeForm.Data.Guid)
                        ("season",state.EpisodeForm.Data.Season |> Option.map string |> Option.defaultValue "0")
                    ]).UploadFile(f.Data)
                return ()
            }
        { state with EpisodeForm = state.EpisodeForm |> RemoteData.setInProgress }, Cmd.OfAsync.eitherAsResult (fun _ -> save ()) EpisodeSaved
    | EpisodeSaved res ->
        let cmd =
            Cmd.batch [
                res |> ToastView.Cmd.ofResult "Episode successfully created"
                if res |> ServerResult.isOk then Router.Cmd.navigatePage (Router.Page.Episodes)
            ]
        { state with
            EpisodeForm =
                state.EpisodeForm
                |> RemoteData.setResponse ()
                |> RemoteData.applyValidationErrors (res |> ServerResult.getValidationErrors) }, cmd

[<ReactComponent>]
let EpisodesFormView (guid:string option) =

    let state, dispatch = React.useElmish(init guid, update, [| box guid |])

    let ti = textInput state.EpisodeForm (EpisodeFormChanged >> dispatch)
    let si = selectInput state.EpisodeForm (EpisodeFormChanged >> dispatch)
    let ni = numberInput state.EpisodeForm (EpisodeFormChanged >> dispatch)
    let ci = checkboxInput state.EpisodeForm (EpisodeFormChanged >> dispatch)
    let lti = textAreaInput state.EpisodeForm (EpisodeFormChanged >> dispatch)
    let fi = fileInput state.FileForm (FileFormChanged >> dispatch)

    Html.divClassed "grid grid-cols-12 gap-4" [
        Html.divClassed "col-span-4 text-center" [
            fi fileLens
            //fi logoLens
        ]
        Html.divClassed "col-span-8" [
            Html.divClassed "grid grid-cols-2 gap-4" [
                Html.div [
                    ti Episode.guid
                    ti Episode.title
                    lti Episode.description
                    ti Episode.duration
                    si (Compose.namedLens Episode.episodeType (EpisodeType.value, EpisodeType.create)) ([ EpisodeType.Trailer; EpisodeType.Bonus; EpisodeType.Full ] |> List.map EpisodeType.value)
                ]
                Html.div [
                    ti Episode.publish
                    ni (Compose.namedLens Episode.season (Option.defaultValue 0, (fun v -> if v > 0 then Some v else None)))
                    ni (Compose.namedLens Episode.episode (Option.defaultValue 0, (fun v -> if v > 0 then Some v else None)))
                    ti (Compose.namedLens Episode.restrictions (StringConversions.fromList, StringConversions.toList))
                    ti (Compose.namedLens Episode.keywords (StringConversions.fromList, StringConversions.toList))
                    ci Episode.explicit
                ]
            ]
            Daisy.label [  ]
            Daisy.button.button [
                state.Guid |> Option.map (fun _ -> prop.text "Update") |> Option.defaultValue (prop.text "Add new")
                prop.disabled (state.EpisodeForm |> RemoteData.isNotValid || state.FileForm |> RemoteData.isNotValid)
                button.primary
                if state.EpisodeForm.InProgress then button.loading
                prop.onClick (fun _ -> SaveEpisode |> dispatch)
            ]
        ]
    ]
