module FuncasterStudio.Server.WebApp

open FuncasterStudio.Server.BlobStorage
open Giraffe

let webApp : HttpHandler =
    choose [
        Podcasts.API.podcastsAPI
        htmlFile "public/index.html"
    ]