﻿module FuncasterStudio.Client.Router

open Browser.Types
open Feliz.Router
open Fable.Core.JsInterop

type Page =
    | Episodes
    | Podcast
    | Messages

[<RequireQualifiedAccess>]
module Page =
    let defaultPage = Page.Episodes

    let parseFromUrlSegments = function
        | [ "episodes" ] -> Page.Episodes
        | [ "podcast" ] -> Page.Podcast
        | [ "messages" ] -> Page.Messages
        | _ -> defaultPage

    let noQueryString segments : string list * (string * string) list = segments, []

    let toUrlSegments = function
        | Page.Episodes -> [ "episodes" ] |> noQueryString
        | Page.Podcast -> [ "podcast" ] |> noQueryString
        | Page.Messages -> [ "messages" ] |> noQueryString

[<RequireQualifiedAccess>]
module Router =
    let goToUrl (e:MouseEvent) =
        e.preventDefault()
        let href : string = !!e.currentTarget?attributes?href?value
        Router.navigatePath href

    let navigatePage (p:Page) = p |> Page.toUrlSegments |> Router.navigatePath

[<RequireQualifiedAccess>]
module Cmd =
    let navigatePage (p:Page) = p |> Page.toUrlSegments |> Cmd.navigatePath