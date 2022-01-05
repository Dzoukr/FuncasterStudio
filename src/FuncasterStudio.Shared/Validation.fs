module FuncasterStudio.Shared.Validation

open System
open Aether

type ValidationErrorType =
    | IsEmpty
    | IsNotEmail

module ValidationErrorType =
    let explain = function
        | IsEmpty -> "Must contain some value"
        | IsNotEmail -> "Must be valid email"

type ValidationError = {
    Key : string
    Message : ValidationErrorType
}

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