namespace FuncasterStudio.Client.Components.HotToast

open System
open Fable.Core
open Fable.Core.JsInterop
open Fable.React
open Feliz
open FuncasterStudio.Client.Interop

[<RequireQualifiedAccess>]
module Toast =

    let private _customToast (elm:ReactElement) (props:obj) : unit = import "toast.custom" "react-hot-toast"

    let propsToRecord (p:Props) =
        {|
            duration = p |> Props.get "duration"
            position = p |> Props.get "position" |> Option.map ToastPosition.value
        |}
    let customToast (p:Props) = _customToast (p |> Props.getDefault "children" [] |> React.fragment) (p |> propsToRecord)


type IToastProperty = interface end

[<Erase>]
type toast =
    static member inline duration (ts:TimeSpan) : IToastProperty = unbox ("duration", int ts.TotalMilliseconds)
    static member inline position (p:ToastPosition) : IToastProperty = unbox ("position", p)
    static member inline children (xs:ReactElement list) : IToastProperty = unbox ("children", xs)

[<Erase>]
type Toast =
    static member inline custom (props:IToastProperty list) = props |> Props.ofList |> Toast.customToast
    static member inline custom (children:ReactElement list) = children |> toast.children |> List.singleton |> Toast.custom