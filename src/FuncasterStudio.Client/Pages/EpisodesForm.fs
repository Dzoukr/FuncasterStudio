module FuncasterStudio.Client.Pages.EpisodesForm

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
open FuncasterStudio.Client.Server
open FuncasterStudio.Client.SharedView
open FuncasterStudio.Client.Forms
open FuncasterStudio.Shared.Episodes.API
open Fable.Core.JsInterop
open FuncasterStudio.Shared.Validation

type State = {
    Guid : string option
    Episode : RemoteReadData<Episode>
    EpisodeForm : RemoteData<Episode,unit,ValidationError>
    FileForm : RemoteData<File option,unit,ValidationError>
}

type Msg =
    | LoadEpisode of guid:string
    | EpisodeLoaded of Episode
    | EpisodeFormChanged of Episode
    | FileFormChanged of File option
    | SaveEpisode
    | EpisodeSaved of unit

let private fileLens = NamedLens.create "Audio File" id (fun (x:File option) (v:File option) -> x )
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
    },
        match guid with
        | Some i -> Cmd.batch [Cmd.ofMsg (LoadEpisode i) ]
        | None -> Cmd.none

let update (msg:Msg) (state:State) : State * Cmd<Msg> =
    match msg with
    | LoadEpisode i -> state, Cmd.none
    | EpisodeFormChanged form -> { state with EpisodeForm = state.EpisodeForm |> RemoteData.setData form Episode.validate }, Cmd.none
    | FileFormChanged file ->
        JS.console.log file
        { state with FileForm = state.FileForm |> RemoteData.setData file validateFile }, Cmd.none


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

//            Html.divClassed "text-center" [
//                Html.img [
//                    yield!
//                        match state.Logo with
//                        | Finished logo -> [ prop.src logo ]
//                        | _ -> []
//                    prop.className "w-full mb-2 rounded"
//                ]
//                Daisy.input [
//                    prop.type'.file
//                    prop.accept ".png"
//                    prop.onInput (fun e ->
//                        let file : File = e.target?files?(0)
//                        file |> LogoFileChosen |> dispatch
//                    )
//                ]
//                if state.LogoUpload.Data.IsSome || state.LogoUpload.InProgress then
//                    Daisy.button.button [
//                        prop.text "Upload new logo"
//                        button.secondary
//                        if state.LogoUpload.InProgress then button.loading
//                        prop.onClick (fun _ -> UploadLogo |> dispatch)
//                    ]
//            ]
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
