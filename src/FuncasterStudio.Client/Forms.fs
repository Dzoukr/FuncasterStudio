module FuncasterStudio.Client.Forms

open Aether
open Fable.Core.JsInterop
open Browser.Types
open Feliz
open Feliz.DaisyUI
open FuncasterStudio.Client.Server
open FuncasterStudio.Shared.Validation

let textInput (form:RemoteData<'a,_,ValidationError>) (onDataChanged:'a -> unit) (n:NamedLens<'a,string>) =
    let value = form.Data |> Optic.get n.Lens
    let err = form.Errors |> ValidationError.get n
    Daisy.formControl [
        Daisy.label [ Daisy.labelText n.Name ]
        Daisy.input [
            input.bordered
            if err.IsSome then input.error
            prop.valueOrDefault value
            prop.onTextChange (fun t -> form.Data |> Optic.set n.Lens t |> onDataChanged)
            prop.placeholder n.Name
        ]
        match err with
        | Some e -> Daisy.label [ Daisy.labelTextAlt [ prop.text (ValidationErrorType.explain e); color.textError ] ]
        | None -> Html.none
    ]

let textAreaInput (form:RemoteData<'a,_,ValidationError>) (onDataChanged:'a -> unit) (n:NamedLens<'a,string>) =
    let value = form.Data |> Optic.get n.Lens
    let err = form.Errors |> ValidationError.get n
    Daisy.formControl [
        Daisy.label [ Daisy.labelText n.Name ]
        Daisy.textarea [
            input.bordered
            if err.IsSome then input.error
            prop.valueOrDefault value
            prop.onTextChange (fun t -> form.Data |> Optic.set n.Lens t |> onDataChanged)
            prop.placeholder n.Name
            prop.rows 4
        ]
        match err with
        | Some e -> Daisy.label [ Daisy.labelTextAlt [ prop.text (ValidationErrorType.explain e); color.textError ] ]
        | None -> Html.none
    ]

let numberInput (form:RemoteData<'a,_,ValidationError>) (onDataChanged:'a -> unit) (n:NamedLens<'a,int>) =
    let value = form.Data |> Optic.get n.Lens
    let err = form.Errors |> ValidationError.get n
    Daisy.formControl [
        Daisy.label [ Daisy.labelText n.Name ]
        Daisy.input [
            input.bordered
            prop.type'.number
            if err.IsSome then input.error
            prop.min 0
            prop.valueOrDefault value
            prop.onChange (fun t -> form.Data |> Optic.set n.Lens t |> onDataChanged)
            prop.placeholder n.Name
        ]
        match err with
        | Some e -> Daisy.label [ Daisy.labelTextAlt [ prop.text (ValidationErrorType.explain e); color.textError ] ]
        | None -> Html.none
    ]

let fileInput (form:RemoteData<'a,_,ValidationError>) (onDataChanged:'a -> unit) (n:NamedLens<'a,File option>) =
    let err = form.Errors |> ValidationError.get n
    Daisy.formControl [
        Daisy.label [ Daisy.labelText n.Name ]
        Daisy.input [
            prop.className "pt-1"
            input.bordered
            prop.type'.file
            if err.IsSome then input.error
            prop.onInput (fun e ->
                let file : File = e.target?files?(0)
                form.Data |> Optic.set n.Lens (Some file) |> onDataChanged
            )
            prop.placeholder n.Name
        ]
        match err with
        | Some e -> Daisy.label [ Daisy.labelTextAlt [ prop.text (ValidationErrorType.explain e); color.textError ] ]
        | None -> Html.none
    ]

let selectInput (form:RemoteData<'a,_,ValidationError>) (onDataChanged:'a -> unit) (n:NamedLens<'a,string>) (allValues:string list) =
    let value = form.Data |> Optic.get n.Lens
    let err = form.Errors |> ValidationError.get n
    Daisy.formControl [
        Daisy.label [ Daisy.labelText n.Name ]
        Daisy.select [
            input.bordered
            if err.IsSome then input.error
            prop.className "capitalize"
            prop.defaultValue value
            prop.onChange (fun (v:string) -> form.Data |> Optic.set n.Lens v |> onDataChanged)
            prop.children [
                for v in allValues do
                    Html.option [
                        prop.text v
                        prop.className "capitalize"
                    ]
            ]
        ]
        match err with
        | Some e -> Daisy.label [ Daisy.labelTextAlt [ prop.text (ValidationErrorType.explain e); color.textError ] ]
        | None -> Html.none
    ]

let checkboxInput (form:RemoteData<'a,_,ValidationError>) (onDataChanged:'a -> unit) (n:NamedLens<'a,bool>) =
    let value = form.Data |> Optic.get n.Lens
    let err = form.Errors |> ValidationError.get n
    Daisy.formControl [
        Daisy.label [ Daisy.labelText n.Name ]
        Daisy.toggle [
            input.bordered
            prop.className "my-3"
            if err.IsSome then input.error
            prop.isChecked value
            prop.onChange (fun (t:bool) -> form.Data |> Optic.set n.Lens t |> onDataChanged)
        ]
        match err with
        | Some e -> Daisy.label [ Daisy.labelTextAlt [ prop.text (ValidationErrorType.explain e); color.textError ] ]
        | None -> Html.none
    ]

module StringConversions =
    let fromList (xs:string list) = xs |> String.concat ", "
    let toList (s:string) = s.Split(",") |> Seq.map (fun x -> x.Trim()) |> Seq.toList