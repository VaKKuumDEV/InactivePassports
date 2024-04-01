using MVD.Endpoints;

namespace MVD.Util
{
    public static class Utils
    {
        public static readonly string DbPath = new FileInfo(GetAppDir() + "/quartznet.db").FullName;

        public static string GetAppDir(string? subDir = null)
        {
            DirectoryInfo dir = new(Environment.CurrentDirectory + "/Data");
            if (!dir.Exists) dir.Create();

            if (subDir != null)
            {
                dir = new(dir.FullName + "/" + subDir);
                if (!dir.Exists) dir.Create();
            }

            return dir.FullName;
        }

        public static async Task SetAnswer(this HttpContext context, EndpointAnswer answer)
        {
            context.Response.Clear();
            context.Response.ContentType = "text/plain;charset=utf-8";
            context.Response.StatusCode = answer.Code == EndpointAnswer.SUCCESS_CODE ? 200 : 404;
            await context.Response.WriteAsync(answer.ToString());
        }

        public static void MyDownloadFile(Uri url, string outputFilePath)
        {
            FileInfo outputFile = new(outputFilePath);
            if (outputFile.Directory != null && !outputFile.Directory.Exists) outputFile.Directory.Create();
            if (outputFile.Exists) outputFile.Delete();

            HttpClientHandler handler = new()
            {
                ClientCertificateOptions = ClientCertificateOption.Automatic,
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; },
                AllowAutoRedirect = true,
                SslProtocols = System.Security.Authentication.SslProtocols.Tls13 | System.Security.Authentication.SslProtocols.Tls12,
            };

            HttpClient client = new(handler, true)
            {
                Timeout = TimeSpan.FromSeconds(1200),
            };

            Task<HttpResponseMessage> responseTask = client.GetAsync(url.AbsoluteUri);
            HttpResponseMessage response = responseTask.GetAwaiter().GetResult();
            Stream respStream = response.Content.ReadAsStream();

            const int BUFFER_SIZE = 16 * 1024;
            using var outputFileStream = File.Create(outputFilePath, BUFFER_SIZE);
            var buffer = new byte[BUFFER_SIZE];
            int bytesRead;

            long totalLoadedBytes = 0;
            int percents = 0;
            do
            {
                bytesRead = respStream.Read(buffer, 0, BUFFER_SIZE);
                totalLoadedBytes += bytesRead;

                int newPercents = (int)Math.Floor(totalLoadedBytes / respStream.Length * 100.0);
                if (newPercents > percents) Logger.Info("File " + outputFile.Name + ": " + (percents = newPercents) + "%");

                outputFileStream.Write(buffer, 0, bytesRead);
            } while (bytesRead > 0);

            respStream.Close();
            outputFileStream.Close();
            response.Content.Dispose();
            response.Dispose();
        }
    }
}
