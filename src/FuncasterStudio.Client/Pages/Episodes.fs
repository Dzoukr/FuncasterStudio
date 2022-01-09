module FuncasterStudio.Client.Pages.Episodes

open System
open Aether
open Browser.Types
open Fable.Remoting.Client
open Feliz
open Feliz.DaisyUI
open Elmish
open Feliz.UseElmish
open Funcaster.Domain
open FuncasterStudio.Client.Server
open FuncasterStudio.Client.SharedView
open FuncasterStudio.Client.Router
open FuncasterStudio.Shared.Episodes.API
open FuncasterStudio.Shared.Podcasts.API
open Fable.Core.JsInterop
open FuncasterStudio.Shared.Validation

type State = {
    Episodes : RemoteReadData<EpisodeListItem list>
}

type Msg =
    | LoadEpisodes
    | EpisodesLoaded of ServerResult<EpisodeListItem list>

let init () = { Episodes = RemoteReadData.init }, Cmd.ofMsg LoadEpisodes

let update (msg:Msg) (state:State) : State * Cmd<Msg> =
    match msg with
    | LoadEpisodes -> { state with Episodes = RemoteReadData.setInProgress }, Cmd.OfAsync.eitherAsResult (fun _ -> episodesAPI.GetEpisodes ()) EpisodesLoaded
    | EpisodesLoaded i ->
        match i with
        | Ok eps -> { state with Episodes = RemoteReadData.setResponse eps }, Cmd.none
        | Error e -> { state with Episodes = RemoteReadData.init }, ToastView.Cmd.ofError e

[<ReactComponent>]
let EpisodesView () =
    let state, dispatch = React.useElmish(init, update, [| |])

    let episodeBadge (et:EpisodeType) =
        match et with
        | Full -> Html.none
        | Trailer -> Daisy.badge [ badge.outline; badge.secondary; prop.text "trailer"; prop.className "ml-4" ]
        | Bonus -> Daisy.badge [ badge.outline; badge.primary; prop.text "bonus"; prop.className "ml-4" ]

    Html.divClassed "" [
        Daisy.button.button [
            prop.text "Add new episode"
            prop.className "mb-4"
            button.primary
            prop.onClick (fun _ -> Router.navigatePage Page.EpisodesCreate)
        ]
        Daisy.table [
            table.zebra
            prop.className "w-full"
            prop.children [
                Html.thead [ Html.tr [
                    Html.th "Season"
                    Html.th "Episode"
                    Html.th "Title"
                    Html.th "Length"
                    Html.th "Publish Date"
                ] ]
                Html.tbody [
                    match state.Episodes with
                    | Idle | InProgress -> Html.tr [ Html.td [ prop.colSpan 5; prop.className "text-center"; prop.text "...loading" ] ]
                    | Finished data ->
                        for d in data do
                        Html.tr [
                            Html.td (d.Season |> Option.map string |> Option.defaultValue "-")
                            Html.td (d.Episode |> Option.map string |> Option.defaultValue "-")
                            Html.td [
                                Daisy.link [
                                    prop.text d.Title
                                    yield! prop.href (Page.EpisodesEdit d.Guid)
                                ]
                                episodeBadge d.EpisodeType
                            ]
                            Html.td (string d.Duration)
                            Html.td (d.Publish.ToString("o"))
                        ]
                ]
            ]
        ]
    ]

