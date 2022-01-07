module FuncasterStudio.Shared.Validation

open System
open Aether

type ValidationErrorType =
    | IsEmpty
    | IsNotEmail
    | IsNotUri

module ValidationErrorType =
    let explain = function
        | IsEmpty -> "Must contain some value"
        | IsNotEmail -> "Must be valid email"
        | IsNotUri -> "Must be valid URL address"

type ValidationError = {
    Key : string
    Message : ValidationErrorType
}

module ValidationError =
    let getAll (prop:NamedLens<_,_>) (errs:ValidationError list) =
        errs |> List.filter (fun x -> x.Key = prop.Name) |> List.map (fun x -> x.Message)
    let get (prop:NamedLens<_,_>) = getAll prop >> List.tryHead


let rules (fns:('a -> ValidationError option) list) (value:'a) =
    fns
    |> List.choose (fun fn -> fn value)

let check (l:NamedLens<'a,'b>) (fn:'b -> ValidationErrorType option) (value:'a) =
    value
    |> (fst l.Lens)
    |> fn
    |> Option.map (fun err -> { Key = l.Name; Message = err })

type Validator =
    static member isNotEmpty (v:string) = if String.IsNullOrWhiteSpace v then Some IsEmpty else None

    static member isEmail (value:string) =
        let parts = value.Split([|'@'|])
        if parts.Length < 2 then Some IsNotEmail
        else
            let lastPart = parts.[parts.Length - 1]
            if (lastPart.Split([|'.'|], StringSplitOptions.RemoveEmptyEntries).Length > 1) then None else Some IsNotEmail

    static member isUri (value:string) =
        match Uri.TryCreate(value, UriKind.Absolute) with
        | true, v ->None
        | _ -> Some IsNotUri