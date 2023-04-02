using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Threading;

namespace VadRsa.Extensions.Http.Internal
{
    internal class ContentDownloadRequest
    {
        public ContentDownloadRequest(Pipe pipe, HttpDownloader downloader)
        {
            Pipe = pipe;
            Downloader = downloader;
            Context = new DownloadContext();
        }

        private Pipe Pipe { get; }
        private HttpDownloader Downloader { get; }
        private DownloadContext Context { get; }

        public async Task DownloadNonChunkedAsync(long? length, CancellationToken cancellationToken)
        {
            try
            {
                var request = Downloader.RequestFactory();
                var response = await Downloader.Client.SendAsync(request, cancellationToken);
                long total = 0;

                try
                {
                    using (var responseStream = await response.Content.ReadAsStreamAsync())
                    {
                        int count;
                        while ((count = await responseStream.ReadAsync(Pipe.Writer.GetMemory(), cancellationToken)) != 0)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            Pipe.Writer.Advance(count);
                            await Pipe.Writer.FlushAsync(cancellationToken);
                            total += count;
                            Context.TotalBytesWritten += count;
                        }
                    }

                    if (length.HasValue && length > 0 && length != total)
                    {
                        await Pipe.Writer.CompleteAsync(new OperationCanceledException("Download operation closed before receiving all bytes."));
                    }
                    else
                    {
                        await Pipe.Writer.CompleteAsync();
                    }
                }
                finally
                {
                    response.Dispose();
                }
            }
            catch (Exception ex)
            {
                await Pipe.Writer.CompleteAsync(ex);
            }
        }

        public async Task DownloadChunkedAsync(long length, CancellationToken cancellationToken)
        {
            try
            {
                var chunks = GetChunks(length);
                foreach (var chunk in chunks)
                {
                    await DownloadChunkToPipeAsync(chunk, Downloader.Options.ChunkRetryCount, cancellationToken);
                }

                await Pipe.Writer.CompleteAsync();
            }
            catch (Exception ex)
            {
                await Pipe.Writer.CompleteAsync(ex);
            }
        }

        private async Task DownloadChunkToPipeAsync(Chunk chunk, int maxRetriesCount, CancellationToken cancellationToken)
        {
            var numRetries = maxRetriesCount;
            var request = Downloader.RequestFactory();
            request.Headers.Range = new RangeHeaderValue(chunk.FromIndex, chunk.ToIndex);
            var response = await Downloader.Client.SendAsync(request, cancellationToken);
            var expectedLength = chunk.Length;
            long total = 0;

            try
            {
                using (var responseStream = await response.Content.ReadAsStreamAsync())
                {
                    int count;
                    while ((count = await responseStream.ReadAsync(Pipe.Writer.GetMemory(), cancellationToken)) != 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        Pipe.Writer.Advance(count);
                        await Pipe.Writer.FlushAsync(cancellationToken);
                        total += count;
                        Context.TotalBytesWritten += count;
                    }
                }
            }
            catch (Exception)
            {
                if (total != expectedLength)
                {
                    if (numRetries <= 0)
                    {
                        throw;
                    }
                    numRetries--;
                    await DownloadChunkToPipeAsync(new Chunk
                    {
                        FromIndex = Context.TotalBytesWritten,
                        ToIndex = chunk.ToIndex
                    }, numRetries, cancellationToken);
                }
            }
            finally
            {
                response.Dispose();
            }
        }

        IEnumerable<Chunk> GetChunks(long length)
        {
            var chunkSize = Downloader.Options.ChunkSizeInBytes;
            var minChunkSize = Downloader.Options.MinChunkSizeInBytes;
            var d = length / chunkSize;
            var m = length % chunkSize;

            for (var i = 0; i < d; i++)
            {
                if (m > 0 && m < minChunkSize && i == d - 1)
                {
                    yield return new Chunk
                    {
                        FromIndex = chunkSize * i,
                        ToIndex = length - 1
                    };
                }
                else
                {
                    yield return new Chunk
                    {
                        FromIndex = chunkSize * i,
                        ToIndex = chunkSize * (i + 1) - 1
                    };
                }
            }

            if (m > 0 && (m >= minChunkSize || d == 0))
            {
                yield return new Chunk
                {
                    FromIndex = chunkSize * d,
                    ToIndex = length - 1
                };
            }
        }


        private class Chunk
        {
            public long FromIndex { get; set; }
            public long ToIndex { get; set; }
            public long Length => ToIndex - FromIndex + 1;
        }

        private class DownloadContext
        {
            public long TotalBytesWritten { get; set; }
        }
    }

}
