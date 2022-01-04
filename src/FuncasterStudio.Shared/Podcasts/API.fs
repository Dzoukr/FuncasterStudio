module FuncasterStudio.Shared.Podcasts.API

type PodcastsAPI = {
    UploadLogo : byte [] -> Async<unit>
}
with
    static member RouteBuilder _ m = sprintf "/api/podcasts/%s" m