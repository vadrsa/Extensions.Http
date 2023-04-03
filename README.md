# Project
VadRsa.Extensions.Http is a collection of Http extensions for added reliability and high-performance I/O.

# Installation
```shell
dotnet add package VadRsa.Extensions.Http
```

# HttpClient.DownloadAsync
`HttpClient.DownloadAsync` is an extension of `HttpClient` that reliably downloads files of any size with minimal memory usage.
## Features
 - Smart chunking, If the server supports the [Content-Range header](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Range) use it to download files in chunks, if not supported fallback to downloading in one chunk.
 - Retry chunk/file download.
 - Read at your own speed, download as fast as possible. This is done by using producer/consumer pattern under the hood to decouple writes and reads.

## Usage
```cs
HttpClient client = new HttpClient();

client.DownloadAsync("url/to/some/file");
```
