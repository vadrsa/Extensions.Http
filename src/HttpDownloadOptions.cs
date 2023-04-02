namespace VadRsa.Extensions.Http
{
    public class HttpDownloadOptions
    {
        public long MinChunkSizeInBytes { get; set; } = 10 * 1024 * 1024;
        public long ChunkSizeInBytes { get; set; } = 100 * 1024 * 1024;
        public int ChunkRetryCount { get; set; } = 3;
        public int MinBufferSize { get; set; } = 200 * 1024;
        
        public int PauseDownloadThreshold { get; set; } = 0;
    }
}
