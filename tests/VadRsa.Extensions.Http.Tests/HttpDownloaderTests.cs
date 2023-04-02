namespace VadRsa.Extensions.Http.Tests;

public class HttpDownloaderTests
{
    [Fact]
    public async Task DownloadNotChunked_DownloadsCorrectNumberOfBytes()
    {
        var response = await new HttpClient().DownloadAsync("https://www.w3.org/WAI/ER/tests/xhtml/testfiles/resources/pdf/dummy.pdf");

        var fileName = Path.GetTempFileName();
        try
        {
            using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                await response.CopyToAsync(fileStream);
            }
            
            using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                var length = fileStream.Length;
                Assert.Equal(13264, length);
            }
        }
        finally
        {
            File.Delete(fileName);
        }
    }
}