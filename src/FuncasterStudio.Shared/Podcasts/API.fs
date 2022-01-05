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
    let init = {
        Title = ""
        Link = ""
        Description = ""
        Language = None
        Author = ""
        Owner = { Name = ""; Email = "" }
        Explicit = false
        Image = ""
        Category = None
        Type = ChannelType.Episodic
        Restrictions = []
    }

    let title = NamedLens.create "Title" (fun x -> x.Title) (fun x v -> { v with Title = x })
    let link = NamedLens.create "Link" (fun x -> x.Link) (fun x v -> { v with Link = x })
    let description = NamedLens.create "Description" (fun x -> x.Description) (fun x v -> { v with Description = x })
    let language = NamedLens.create "Language" (fun x -> x.Language |> Option.defaultValue "") (fun x v -> { v with Language = if String.IsNullOrEmpty x then None else Some x })
    let author = NamedLens.create "Author" (fun x -> x.Author) (fun x v -> { v with Author = x })
    let ownerName = NamedLens.create "Owner Name" (fun x -> x.Owner.Name) (fun x v -> { v with Owner = { v.Owner with Name = x } })
    let ownerEmail = NamedLens.create "Owner Email" (fun x -> x.Owner.Email) (fun x v -> { v with Owner = { v.Owner with Email = x } })
    let category = NamedLens.create "Category" (fun x -> x.Category |> Option.defaultValue "") (fun x v -> { v with Category = if String.IsNullOrEmpty x then None else Some x })
    let type' = NamedLens.create "Type" (fun x -> x.Type) (fun x v -> { v with Type = x })
    let restrictions = NamedLens.create "Restrictions" (fun x -> x.Restrictions) (fun x v -> { v with Restrictions = x })

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
    GetLogo : unit -> Async<string>
    UploadLogo : byte [] -> Async<unit>
    GetPodcast : unit -> Async<Channel>
}
with
    static member RouteBuilder _ m = sprintf "/api/podcasts/%s" m