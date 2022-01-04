module FuncasterStudio.Server.BlobStorage

open Azure.Storage.Blobs

type PodcastBlobContainer = PodcastBlobContainer of BlobContainerClient

module PodcastBlobContainer =
    let client = function
        | PodcastBlobContainer client -> client