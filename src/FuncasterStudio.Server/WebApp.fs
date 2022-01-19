module FuncasterStudio.Server.WebApp

open Giraffe

let webApp : HttpHandler =
    choose [
        Podcasts.API.podcastsAPI
        Episodes.API.episodesAPI
        Episodes.API.episodesUploaderAPI
        CdnSetup.API.cdnSetupAPI
        htmlFile "public/index.html"
    ]