using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace bimsyncFunction
{
    public static class ShareModel
    {
        [FunctionName("get-shared-model")]
        public static async Task<HttpResponseMessage> Create(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "manager/model/{id}")]HttpRequestMessage req,
            [CosmosDB(
                databaseName: "bimsyncManagerdb",
                collectionName: "bimsyncManagerCollection",
                ConnectionStringSetting = "myDBConnectionString",
                SqlQuery = "select * from bimsyncManagerdb u where u.id = {id}")]IEnumerable<SharingCode> sharingCodes,
            [CosmosDB(
                databaseName: "bimsyncManagerdb",
                collectionName: "bimsyncManagerCollection",
                ConnectionStringSetting = "myDBConnectionString")]IAsyncCollector<SharingCode> sharingCodesOut,
            [CosmosDB(
                databaseName: "bimsyncManagerdb",
                collectionName: "bimsyncManagerCollection",
                ConnectionStringSetting = "myDBConnectionString")]DocumentClient client,
           ILogger log)
        {
            try
            {
                log.LogInformation("Get a shared model");

                if (sharingCodes.Count() != 0)
                {
                    SharingCode sharingCode = sharingCodes.FirstOrDefault();

                    //Refrech the tokens if necessary
                    if (sharingCode.RefreshDate < DateTime.Now)
                    {
                        Uri collectionUri = UriFactory.CreateDocumentCollectionUri("bimsyncManagerdb", "bimsyncManagerCollection");
                        //Get the doc back as a Document so you have access to doc.SelfLink
                        IEnumerable<Document> documentUsers = client.CreateDocumentQuery<Document>(collectionUri).Where(u => u.Id == sharingCode.UserId).AsEnumerable();

                        if (documentUsers.Count() == 0)
                        {
                            return new HttpResponseMessage(HttpStatusCode.BadRequest)
                            {
                                Content = new StringContent(JsonConvert.SerializeObject("Could not find the associate user"), Encoding.UTF8, "application/json")
                            };
                        }

                        User user = (dynamic)documentUsers.FirstOrDefault();

                        //Refrech the token if necessary
                        if (user.RefreshDate < DateTime.Now)
                        {
                            bimsync.AccessToken accessToken = await bimsync.bimsyncServices.RefreshAccessToken(user.AccessToken.refresh_token);

                            if (accessToken == null)
                            {
                                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                                {
                                    Content = new StringContent(JsonConvert.SerializeObject("Could not get an authorisation code from bimsync"), Encoding.UTF8, "application/json")
                                };
                            }

                            user.AccessToken = accessToken;
                            user.RefreshDate = System.DateTime.Now + new System.TimeSpan(0, 0, accessToken.expires_in);

                            await client.ReplaceDocumentAsync(documentUsers.FirstOrDefault().SelfLink, user);
                        }

                        sharingCode = await Services.CreateSharingCode(
                            sharingCode.SharedRevisions,
                            user,
                            sharingCode.SharedModels,
                            sharingCode.SpacesId,
                            sharingCode.id
                        );

                        await sharingCodesOut.AddAsync(sharingCode);
                    }

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                                JsonConvert.SerializeObject(sharingCode),
                                Encoding.UTF8,
                                "application/json"
                                )
                    };
                }
                else
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent(JsonConvert.SerializeObject("The model does not exist or have not been shared"), Encoding.UTF8, "application/json")
                    };
                }
            }
            catch (Exception ex)
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent(ex.Message, Encoding.UTF8, "application/json") };
            }

        }
    }
}
