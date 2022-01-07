module FuncasterStudio.Server.Startup

open Azure.Data.Tables
open Azure.Storage.Blobs
open Azure.Storage.Blobs.Models
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Server.Kestrel.Core
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Giraffe

type Startup(cfg:IConfiguration, env:IWebHostEnvironment) =
    member _.ConfigureServices (services:IServiceCollection) =
        let client = BlobContainerClient(cfg.["PodcastStorage"], "podcast")
        let _ = client.CreateIfNotExists(PublicAccessType.Blob)

        let podcastTable = TableClient(cfg.["PodcastStorage"], "Podcast")
        let _ = podcastTable.CreateIfNotExists()

        services
            .Configure<KestrelServerOptions>(fun (x:KestrelServerOptions) ->
                x.Limits.MaxRequestBodySize <- 500L * 1024L * 1024L
            )
            .AddApplicationInsightsTelemetry(cfg.["APPINSIGHTS_INSTRUMENTATIONKEY"])
            .AddSingleton<BlobContainerClient>(client)
            .AddSingleton<TableClient>(podcastTable)
            .AddGiraffe() |> ignore
    member _.Configure(app:IApplicationBuilder) =
        app
            .UseStaticFiles()
            .UseGiraffe WebApp.webApp