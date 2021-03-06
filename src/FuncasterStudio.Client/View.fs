module FuncasterStudio.Client.View

open System
open Feliz
open Feliz.DaisyUI
open Feliz.DaisyUI.Operators
open Router
open Elmish
open SharedView
open FuncasterStudio.Client.Components.HotToast

type Msg =
    | UrlChanged of Page

type State = {
    Page : Page
}

let init () =
    let nextPage = Router.currentPath() |> Page.parseFromUrlSegments
    { Page = nextPage }, Cmd.navigatePage nextPage

let update (msg:Msg) (state:State) : State * Cmd<Msg> =
    match msg with
    | UrlChanged page -> { state with Page = page }, Cmd.none

let private inTemplate (ap:Page) (elm:ReactElement) =
    let btn (p:Page) (text:string) (icon:string) =
        Daisy.button.button [
            if Page.areRelated ap p then button.active
            button.ghost
            ++ prop.className "mx-1"
            prop.onClick (fun _ -> p |> Router.navigatePage)
            prop.children [
                Html.i [ prop.className (icon + " mr-2") ]
                Html.span text
            ]
        ]
    Html.divClassed "flex flex-col h-screen" [
        Toaster.toaster [
            toaster.position ToastPosition.TopRight
            toaster.options [
                toast.duration (TimeSpan.FromSeconds 4.)
            ]
        ]
        Daisy.navbar [
            prop.className "mb-4 px-4 md:px-16 lg:px-32 shadow-lg bg-neutral text-neutral-content"
            prop.children [
                Daisy.navbarStart [
                    Html.img [ prop.src "https://github.com/Dzoukr/Funcaster/blob/master/logo.png?raw=true"; prop.className "w-12 mx-5" ]
                    Html.span "Funcaster"
                    Html.strong "Studio"
                ]
                Daisy.navbarCenter [
                    btn Page.Episodes "Episodes" "fas fa-stream"
                    btn Page.Podcast "Podcast" "fas fa-podcast"
                    //btn Page.Messages "Messages" "fas fa-comments"
                    btn Page.CdnSetup "CDN" "fas fa-network-wired"
                ]
                Daisy.navbarEnd []
            ]
        ]
        Html.divClassed "flex-grow px-4 md:px-16 lg:px-32" [ elm ]
        Daisy.footer [
            prop.className "p-4 bg-neutral text-neutral-content" ++ footer.center
            prop.children [
                Html.divClassed "flex w-full justify-between" [
                    Html.div [
                        Html.text "Copyright © 2022 - "
                        Daisy.link [
                            prop.text "Roman Provazník"
                            prop.href "https://www.dzoukr.cz"
                        ]
                    ]
                    Html.div [
                        Html.text "Developed with "
                        Daisy.link [
                            prop.text "♥️️ + F#"
                            prop.href "https://www.fsharp.org"
                        ]
                    ]
                ]
            ]
        ]
    ]

[<ReactComponent>]
let AppView (state:State) (dispatch:Msg -> unit) =
    React.router [
        router.pathMode
        router.onUrlChanged (Page.parseFromUrlSegments >> UrlChanged >> dispatch)
        router.children [
            match state.Page with
            | Page.Episodes -> Pages.Episodes.EpisodesView ()
            | Page.EpisodesCreate -> Pages.EpisodesForm.EpisodesFormView None
            | Page.EpisodesEdit guid -> Pages.EpisodesForm.EpisodesFormView (Some guid)
            | Page.Podcast -> Pages.Podcast.PodcastView ()
            | Page.Messages -> Html.text "ME"
            | Page.CdnSetup -> Pages.CdnSetup.CdnSetupView ()
            |> inTemplate state.Page
        ]
    ]