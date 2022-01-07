module FuncasterStudio.Server.PodcastStorage

open System
open Funcaster.Domain
open Azure.Data.Tables
open Azure.Data.Tables.FSharp
open Newtonsoft.Json

module Channel =
    let toEntity (c:Channel) : TableEntity =
        let e = TableEntity()
        e.PartitionKey <- "podcast"
        e.RowKey <- "podcast"
        e.["Title"] <- c.Title
        e.["Link"] <- c.Link |> string
        e.["Description"] <- c.Description
        e.["Language"] <- c.Language |> Option.toObj
        e.["Author"] <- c.Author
        e.["Owner"] <- c.Owner |> JsonConvert.SerializeObject
        e.["Explicit"] <- c.Explicit
        e.["Image"] <- c.Image |> string
        e.["Category"] <- c.Category |> Option.toObj
        e.["Type"] <- c.Type |> ChannelType.value
        e.["Restrictions"] <-
            match c.Restrictions with
            | [] -> null
            | r -> r |> String.concat "|"
        e

    let fromEntity (e:TableEntity) : Channel =
        {
            Title = e.GetString("Title")
            Link = e.GetString("Link") |> Uri
            Description = e.GetString("Description")
            Language = e.GetString("Language") |> Option.ofObj
            Author = e.GetString("Author")
            Owner = e.GetString("Owner") |> JsonConvert.DeserializeObject<Owner>
            Explicit = e.GetBoolean("Explicit") |> Option.ofNullable |> Option.defaultValue false
            Image = e.GetString("Image") |> Uri
            Category = e.GetString("Category") |> Option.ofObj
            Type = e.GetString("Type") |> ChannelType.create
            Restrictions =
                e.GetString("Restrictions")
                |> Option.ofObj
                |> Option.map (fun x -> x.Split("|", StringSplitOptions.RemoveEmptyEntries))
                |> Option.map (Seq.toList >> List.map (fun x -> x.Trim()))
                |> Option.defaultValue []
        }

let getPodcast (podcastTable:TableClient) () =
    task {
        return
            tableQuery {
                filter (pk "podcast" + rk "podcast")
            }
            |> podcastTable.Query<TableEntity>
            |> Seq.tryHead
            |> Option.map Channel.fromEntity
    }

let upsertPodcast (podcastTable:TableClient) (channel:Channel) =
    task {
        let entity = channel |> Channel.toEntity
        let! _ = podcastTable.UpsertEntityAsync(entity, TableUpdateMode.Merge)
        return ()
    }