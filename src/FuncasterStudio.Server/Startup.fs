module FuncasterStudio.Server.Startup

open System
open System.Net.Http
open Azure.Core.Pipeline
open Azure.Storage.Blobs
open Azure.Storage.Blobs.Models
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Server.Kestrel.Core
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Funcaster.Storage

type Startup(cfg:IConfiguration, env:IWebHostEnvironment) =
    member _.ConfigureServices (services:IServiceCollection) =
        let opts = BlobClientOptions()
        opts.Transport <- new HttpClientTransport(new HttpClient(Timeout = TimeSpan.FromMinutes 10))
        opts.Retry.NetworkTimeout <- TimeSpan.FromMinutes 10
        let client = BlobContainerClient(cfg.["PodcastStorage"], "podcast", opts)
        let _ = client.CreateIfNotExists(PublicAccessType.Blob)

        services
            .Configure<KestrelServerOptions>(fun (x:KestrelServerOptions) ->
                x.Limits.MaxRequestBodySize <- 500L * 1024L * 1024L
                x.Limits.KeepAliveTimeout <- TimeSpan.FromMinutes 30.
                x.Limits.RequestHeadersTimeout <- TimeSpan.FromMinutes 30.
            )
            .AddApplicationInsightsTelemetry(cfg.["APPINSIGHTS_INSTRUMENTATIONKEY"])
            .AddSingleton<BlobContainerClient>(client)
            .AddSingleton<PodcastTable>(PodcastTable.createSafe cfg.["PodcastStorage"])
            .AddSingleton<EpisodesTable>(EpisodesTable.createSafe cfg.["PodcastStorage"])
            .AddSingleton<CdnSetupTable>(CdnSetupTable.createSafe cfg.["PodcastStorage"])
            .AddGiraffe() |> ignore
    member _.Configure(app:IApplicationBuilder) =
        app
            .UseStaticFiles()
            .UseGiraffe WebApp.webApp