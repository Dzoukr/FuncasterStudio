module FuncasterStudio.Shared.Validation

open System
open Aether

type ValidationErrorType =
    | MustBeFilled
    | MustBeEmail
    | MustBeUri
    | MustBeLongerThanTimespan of ts:TimeSpan
    | MustBeDateTimeOffsetFormat
    | MustBeTimeSpanFormat
    | MustBeUnique

module ValidationErrorType =
    let explain = function
        | MustBeFilled -> "Must contain some value"
        | MustBeEmail -> "Must be a valid email"
        | MustBeUri -> "Must be a valid URL address"
        | MustBeLongerThanTimespan ts -> $"Must be longer than {ts}"
        | MustBeDateTimeOffsetFormat -> "Must be a correct date format"
        | MustBeTimeSpanFormat -> "Must be a correct time format"
        | MustBeUnique -> "Must be unique"

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
    static member isNotEmpty (v:string) = if String.IsNullOrWhiteSpace v then Some MustBeFilled else None
    static member isNotEmpty (v:byte []) = if v.Length > 0 then None else Some MustBeFilled

    static member isEmail (value:string) =
        let parts = value.Split([|'@'|])
        if parts.Length < 2 then Some MustBeEmail
        else
            let lastPart = parts.[parts.Length - 1]
            if (lastPart.Split([|'.'|], StringSplitOptions.RemoveEmptyEntries).Length > 1) then None else Some MustBeEmail

    static member isUri (value:string) =
        match Uri.TryCreate(value, UriKind.Absolute) with
        | true, _ -> None
        | _ -> Some MustBeUri

    static member isDateTimeOffsetFormat (value:string) =
        match DateTimeOffset.TryParse value with
        | true, _ -> None
        | _ -> Some MustBeDateTimeOffsetFormat

    static member isTimeSpanFormat (value:string) =
        match TimeSpan.TryParse value with
        | true, _ -> None
        | _ -> Some MustBeTimeSpanFormat

    static member isLongerThan (than:TimeSpan) =
        fun (value:TimeSpan) ->
        if value > than then None
        else Some (MustBeLongerThanTimespan than)
