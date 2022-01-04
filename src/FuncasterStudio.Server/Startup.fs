module FuncasterStudio.Server.Startup

open Azure.Storage.Blobs
open Azure.Storage.Blobs.Models
open FuncasterStudio.Server.BlobStorage
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Giraffe

type Startup(cfg:IConfiguration, env:IWebHostEnvironment) =
    member _.ConfigureServices (services:IServiceCollection) =
        let podcast = BlobContainerClient(cfg.["PodcastStorage"], "podcast")
        let _ = podcast.CreateIfNotExists(PublicAccessType.Blob)

        services
            .AddApplicationInsightsTelemetry(cfg.["APPINSIGHTS_INSTRUMENTATIONKEY"])
            .AddSingleton<PodcastBlobContainer>(PodcastBlobContainer podcast)
            .AddGiraffe() |> ignore
    member _.Configure(app:IApplicationBuilder) =
        app
            .UseStaticFiles()
            .UseGiraffe WebApp.webApp