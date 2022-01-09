namespace FuncasterStudio.Client.Components.HotToast

open System
open Fable.Core
open Fable.React
open Feliz
open FuncasterStudio.Client.Interop

[<RequireQualifiedAccess>]
module Toaster =

    [<ReactComponent>]
    let View (p:Props) =
        ofImport "Toaster" "react-hot-toast"
            {|
                position = p |> Props.get "position" |> Option.map ToastPosition.value
                reverseOrder = p |> Props.get "reverseOrder"
                toastOptions = p |> Props.get "toastOptions" |> Option.map (Props.ofList >> Toast.propsToRecord)
            |}
            []

type IToasterProperty = interface end

[<Erase>]
type toaster =
    //static member inline duration (ts:TimeSpan) : IToasterProperty = unbox ("duration", ts.TotalMilliseconds)
    static member inline position (p:ToastPosition) : IToasterProperty = unbox ("position", p)
    static member inline options (opts:IToastProperty list) : IToasterProperty = unbox ("toastOptions", opts)
    static member inline reverseOrder : IToasterProperty = unbox ("reverseOrder", true)

[<Erase>]
type Toaster =
    static member inline toaster (props:IToasterProperty list) = props |> Props.ofList |> Toaster.View