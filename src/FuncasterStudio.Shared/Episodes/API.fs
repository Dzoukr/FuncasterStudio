module FuncasterStudio.Shared.Episodes.API

open System
open Aether
open FuncasterStudio.Shared.Validation
open Funcaster.Domain

type Episode = {
    Guid : string
    Season : int option
    Episode : int option
    //Enclosure : Enclosure
    //Publish : DateTimeOffset
    Publish : string
    Title : string
    Description : string
    Restrictions : string list
//    Duration : TimeSpan
    Duration : string
    Explicit : bool
    //Image : Uri option
    Keywords : string list
    EpisodeType : EpisodeType
}

module Episode =
    let init = {
        Guid = ""
        Season = None
        Episode = None
        Publish = ""
        Title = ""
        Description = ""
        Restrictions = []
        Duration = ""
        Explicit = false
        Keywords = []
        EpisodeType = EpisodeType.Full
    }

    let guid = NamedLens.create "Guid" (fun x -> x.Guid) (fun x v -> { v with Guid = x })
    let season = NamedLens.create "Season" (fun x -> x.Season) (fun x v -> { v with Season = x })
    let episode = NamedLens.create "Episode" (fun x -> x.Episode) (fun x v -> { v with Episode = x })
    let publish = NamedLens.create "Publish" (fun x -> x.Publish) (fun x v -> { v with Publish = x })
    let title = NamedLens.create "Title" (fun x -> x.Title) (fun x v -> { v with Title = x })
    let description = NamedLens.create "Description" (fun x -> x.Description) (fun x v -> { v with Description = x })
    let restrictions = NamedLens.create "Restrictions" (fun x -> x.Restrictions) (fun x v -> { v with Restrictions = x })
    let duration = NamedLens.create "Duration" (fun x -> x.Duration) (fun x v -> { v with Duration = x })
    let explicit = NamedLens.create "Explicit" (fun x -> x.Explicit) (fun x v -> { v with Explicit = x })
    let keywords = NamedLens.create "Keywords" (fun x -> x.Keywords) (fun x v -> { v with Keywords = x })
    let episodeType = NamedLens.create "Episode Type" (fun x -> x.EpisodeType) (fun x v -> { v with EpisodeType = x })

    let validate =
        rules [
            check guid Validator.isNotEmpty
            check title Validator.isNotEmpty
            check description Validator.isNotEmpty
            check duration Validator.isTimeSpanFormat
            check publish Validator.isDateTimeOffsetFormat
        ]

type DataFile = {
    Data : byte []
    Name : string
}

module DataFile =
    let init = { Data = [||]; Name = "" }
    let name = NamedLens.create "Name" (fun x -> x.Name) (fun x v -> { v with Name = x })
    let data = NamedLens.create "Data" (fun x -> x.Data) (fun x v -> { v with Data = x })

    let validate =
        rules [
            check name Validator.isNotEmpty
            check data Validator.isNotEmpty
        ]

type EpisodesAPI = {
    //GetLogo : string -> Async<string>
    //UploadLogo : byte [] -> Async<unit>
    GetEpisode : string -> Async<Episode>
    SaveEpisode : Episode -> Async<unit>
}
with
    static member RouteBuilder _ m = sprintf "/api/episodes/%s" m