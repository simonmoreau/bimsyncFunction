
using System;
using System.IO;
using System.Configuration;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Extensions.Configuration;


namespace bimsync
{
    public static class bimsyncViewer
    {
        [FunctionName("bimsync-viewer")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]HttpRequest req, TraceWriter log, ExecutionContext context)
        {
            IConfigurationRoot configRoot = new ConfigurationBuilder()
    .SetBasePath(context.FunctionAppDirectory)
    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

            log.Info("C# HTTP trigger function processed a request.");

            string requestBody = new StreamReader(req.Body).ReadToEnd();
            Token token = JsonConvert.DeserializeObject<Token>(requestBody);

            string url = CreateHtmlPageAsBlob(token.viewerToken, token.viewer2DToken, configRoot).Result;

            return url != null
                ? (ActionResult)new OkObjectResult(url)
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }

        private async static Task<string> CreateHtmlPageAsBlob(string viewerToken, string viewer2DToken, IConfigurationRoot configRoot)
        {
            string accessKey;
            string accountName;
            string connectionString;
            CloudStorageAccount storageAccount;
            CloudBlobClient client;
            CloudBlobContainer container;

            accessKey = configRoot["BlobStorageAccessKey"];
            accountName = configRoot["BlobStorageAccountName"];
            connectionString = "DefaultEndpointsProtocol=https;AccountName=" + accountName + ";AccountKey=" + accessKey + ";EndpointSuffix=core.windows.net";
            storageAccount = CloudStorageAccount.Parse(connectionString);

            client = storageAccount.CreateCloudBlobClient();

            container = client.GetContainerReference("bimsync-viewer");

            await container.CreateIfNotExistsAsync();

            //Get template as string
            // Retrieve reference to a blob named "viewer-template.html"
            CloudBlockBlob htmlTemplateBlob = container.GetBlockBlobReference("viewer-template.html");

            //Download the html template
            using (var memoryStream = new MemoryStream())
            {
                string html = await htmlTemplateBlob.DownloadTextAsync();

                //Replace the placeholder with the actual token
                html = html.Replace("@viewerToken@", viewerToken);
                html = html.Replace("@viewer2DToken@", viewer2DToken);

                CloudBlockBlob blob = container.GetBlockBlobReference("bimsync-viewer-" + viewerToken+".html");
                blob.Properties.ContentType = "text/html";

                using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(html)))
                {
                    await blob.UploadFromStreamAsync(stream);
                }

                return blob.Uri.ToString();
            }
        }
    }

    public class Token
    {
        public string viewerToken { get; set; }
        public string viewer2DToken { get; set; }
    }


}
