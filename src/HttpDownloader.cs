using VadRsa.Extensions.Http.Internal;
using System;
using System.IO.Pipelines;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Args;

namespace VadRsa.Extensions.Http
{
	/// <summary>
	/// Reliable HTTP file Downloader with ability to chunk files and retry the chunks
	/// </summary>
	public class HttpDownloader
	{
		internal readonly HttpClient Client;
		internal readonly Func<HttpRequestMessage> RequestFactory;
		internal readonly HttpDownloadOptions Options;

		public HttpDownloader(HttpClient client, Func<HttpRequestMessage> requestFactory, HttpDownloadOptions downloadOptions)
		{
			Client = client.Arg(nameof(client)).IsNotNull().Value;
			RequestFactory = requestFactory.Arg(nameof(requestFactory)).IsNotNull().Value;
			Options = downloadOptions ?? new HttpDownloadOptions();
		}
		
		/// <summary>
		/// Download a file over HTTP
		/// </summary>
		/// <returns>Immediately returns a <see cref="HttpDownloadResponse"/> object which can be used to export the file to the wanted destination.</returns>
		public async Task<HttpDownloadResponse> DownloadAsync(CancellationToken cancellationToken = default)
		{
			var request = RequestFactory();
			request.Headers.Range = new RangeHeaderValue(0, null);
			
			var response = await Client.SendAsync(request, cancellationToken);
			var length = response.Content.Headers.ContentLength;
			var contentRange = response.Content.Headers.ContentRange;
			
			var pipe = new Pipe(new PipeOptions(minimumSegmentSize: Options.MinBufferSize, pauseWriterThreshold: 0));

			var contentDownloadRequest = new ContentDownloadRequest(pipe, this);
			Task downloadTask;
			if (contentRange != null && contentRange.HasLength && contentRange.Length.HasValue)
			{
				downloadTask = Task.Run(() => contentDownloadRequest.DownloadChunkedAsync(contentRange.Length.Value, cancellationToken), cancellationToken);
			}
			else
			{
				downloadTask = Task.Run(() => contentDownloadRequest.DownloadNonChunkedAsync(length, cancellationToken), cancellationToken);
			}

			_ = downloadTask.ContinueWith(x => response.Dispose(), cancellationToken);
			
			return new HttpDownloadResponse(response, pipe);
		}
	}
}
