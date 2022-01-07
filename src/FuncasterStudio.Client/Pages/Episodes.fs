module FuncasterStudio.Client.Pages.Episodes

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
open FuncasterStudio.Client.Router
open FuncasterStudio.Shared.Podcasts.API
open Fable.Core.JsInterop
open FuncasterStudio.Shared.Validation

[<ReactComponent>]
let EpisodesView () =
    Html.divClassed "" [
        Daisy.button.button [
            prop.text "Add new episode"
            button.primary
            prop.onClick (fun _ -> Router.navigatePage Page.EpisodesCreate)
        ]
    ]