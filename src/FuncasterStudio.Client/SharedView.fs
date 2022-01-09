module FuncasterStudio.Client.SharedView

open Feliz
open FuncasterStudio.Client.Components.HotToast
open FuncasterStudio.Shared.Validation
open Router

type prop
    with
        static member inline href (p:Page) = [
            prop.href (p |> Page.toUrlSegments |> Router.formatPath)
            prop.onClick Router.goToUrl
        ]

type Html
    with
        static member inline a (text:string, p:Page) =
            Html.a [
                yield! prop.href p
                prop.text text
            ]

        static member inline divClassed (cn:string) (elms:ReactElement list) =
            Html.div [
                prop.className cn
                prop.children elms
            ]

module ToastView =
    open FuncasterStudio.Shared.Errors
    open FuncasterStudio.Client.Server
    open Feliz.DaisyUI

    let serverErrorToast (e:ServerError) =
        let title =
            match e with
            | Exception _ -> "An error occured"
            | Validation _ -> "Validation failed"
        let msg =
            match e with
            | Exception e -> e
            | Validation errs ->
                errs
                |> List.map (fun x -> $"{x.Key} : {x.Message |> ValidationErrorType.explain}")
                |> String.concat ", "

        Html.divClassed "" [
            Daisy.alert [
                alert.error
                prop.children [
                    Html.divClassed "flex-1" [
                        Html.i [ prop.className "fas fa-exclamation-circle mt-1 mr-2" ]
                        Html.label [
                            Html.divClassed "font-bold mb-2" [ Html.text title ]
                            Html.p msg
                        ]
                    ]
                ]
            ]
        ]

    let successToast (msg:string) =
        Daisy.alert [
            alert.success
            prop.className "justify-start"
            prop.children [
                Html.i [ prop.className "fas fa-thumbs-up mr-2" ]
                Html.label [ prop.text msg ]
            ]
        ]


    module Cmd =
        open Elmish
        let ofError (e:ServerError) = Cmd.ofSub (fun _ -> e |> serverErrorToast |> List.singleton |> Toast.custom)
        let ofSuccess (msg:string) = Cmd.ofSub (fun _ -> msg |> successToast |> List.singleton |> Toast.custom)
        let ofResult (successMsg:string) (res:ServerResult<_>) =
            match res with
            | Ok _ -> successMsg |> ofSuccess
            | Error e -> e |> ofError