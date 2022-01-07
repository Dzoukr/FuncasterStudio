module FuncasterStudio.Server.WebApp

open Giraffe

let webApp : HttpHandler =
    choose [
        Podcasts.API.podcastsAPI
        htmlFile "public/index.html"
    ]