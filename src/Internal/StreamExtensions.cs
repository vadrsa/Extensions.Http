using System;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;

namespace VadRsa.Extensions.Http.Internal
{
    internal static class StreamExtensions
    {
        public static async Task CopyToPooledAsync(this Stream source, Stream destination, int bufferSize = 81920, CancellationToken cancellationToken = default)
        {
            int readBytesNumber;
            byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            while ((readBytesNumber = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await destination.WriteAsync(buffer, 0, readBytesNumber, cancellationToken);
            }
        }

        internal static ValueTask<int> ReadAsync(this Stream stream, Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> array))
            {
                return new ValueTask<int>(stream.ReadAsync(array.Array, array.Offset, array.Count, cancellationToken));
            }
            else
            {
                byte[] sharedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
                return FinishReadAsync(stream.ReadAsync(sharedBuffer, 0, buffer.Length, cancellationToken), sharedBuffer, buffer);
            }
        }

        private static async ValueTask<int> FinishReadAsync(Task<int> readTask, byte[] localBuffer, Memory<byte> localDestination)
        {
            try
            {
                int result = await readTask.ConfigureAwait(false);
                new Span<byte>(localBuffer, 0, result).CopyTo(localDestination.Span);
                return result;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(localBuffer);
            }
        }
    }
}
