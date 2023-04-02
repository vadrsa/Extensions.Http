using System.IO;
using System.IO.Pipelines;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using VadRsa.Extensions.Http.Internal;

namespace VadRsa.Extensions.Http
{
    /// <summary>
    /// A http download response that exposes the downloaded bytes as they are available.
    /// The bytes are being downloaded regardless of whether they are read or not, the number of unread bytes can be limited by the <see cref="HttpDownloadOptions.PauseDownloadThreshold"/> option
    /// </summary>
    public class HttpDownloadResponse
    {
        internal HttpDownloadResponse(HttpResponseMessage response,Pipe pipe)
        {
            RawResponse = response;
            Pipe = pipe;
        }

        private Pipe Pipe { get; }
        
        /// <summary>
        /// Get a stream for reading the downloaded bytes
        /// Warning: all the bytes read from the stream will be cleaned up.
        /// </summary>
        /// <returns>A stream of bytes</returns>
        public Stream GetStream() => Pipe.Reader.AsStream();
        
        /// <summary>
        /// Original <see cref="HttpResponseMessage"/>
        /// Note: don't try to get the content stream from the response directly
        /// </summary>
        public HttpResponseMessage RawResponse { get; }

        /// <summary>
        /// Copy the downloaded stream of bytes into the destination stream, this operation will complete only if the full file is downloaded, or the download fails
        /// Warning: all the bytes copied will be cleaned up.
        /// </summary>
        public async Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default)
        {
            using (var source = GetStream())
            {
                await source.CopyToPooledAsync(destination, cancellationToken: cancellationToken);
            }
        }
    }
}
