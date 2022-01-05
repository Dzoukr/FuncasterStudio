module FuncasterStudio.Shared.Podcasts.API

open System
open Aether
open FuncasterStudio.Shared.Validation

type Owner = {
    Name : string
    Email : string
}

type ChannelType = Episodic | Serial

type Channel = {
    Title : string
    Link : string
    Description : string
    Language : string option
    Author : string
    Owner : Owner
    Explicit : bool
    Image : string
    Category : string option
    Type : ChannelType
    Restrictions : string list
}

module Channel =
    let title = NamedLens.create "title" (fun x -> x.Title) (fun x v -> { v with Title = x })
    let link = NamedLens.create "link" (fun x -> x.Link) (fun x v -> { v with Link = x })
    let description = NamedLens.create "description" (fun x -> x.Description) (fun x v -> { v with Description = x })
    let language = NamedLens.create "language" (fun x -> x.Language |> Option.defaultValue "") (fun x v -> { v with Language = if String.IsNullOrEmpty x then None else Some x })
    let author = NamedLens.create "author" (fun x -> x.Author) (fun x v -> { v with Author = x })
    let ownerName = NamedLens.create "OwnerName" (fun x -> x.Owner.Name) (fun x v -> { v with Owner = { v.Owner with Name = x } })
    let ownerEmail = NamedLens.create "OwnerEmail" (fun x -> x.Owner.Email) (fun x v -> { v with Owner = { v.Owner with Email = x } })
    let category = NamedLens.create "category" (fun x -> x.Category |> Option.defaultValue "") (fun x v -> { v with Category = if String.IsNullOrEmpty x then None else Some x })
    let type' = NamedLens.create "type" (fun x -> x.Type) (fun x v -> { v with Type = x })
    let restrictions = NamedLens.create "restrictions" (fun x -> x.Restrictions) (fun x v -> { v with Restrictions = x })

    let validate =
        rules [
            check title Validator.isNotEmpty
            check link Validator.isNotEmpty
            check description Validator.isNotEmpty
            check author Validator.isNotEmpty
            check ownerName Validator.isNotEmpty
            check ownerEmail Validator.isEmail
        ]

type PodcastsAPI = {
    UploadLogo : byte [] -> Async<string>
    GetPodcast : unit -> Async<Channel>
}
with
    static member RouteBuilder _ m = sprintf "/api/podcasts/%s" m