
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
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace bimsyncFunction
{
    public static class users
    {
        [FunctionName("user-root")]
        public static HttpResponseMessage Root([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "manager/users")]HttpRequest req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject("Welcome to the bimsyncManager API"), Encoding.UTF8, "application/json")
            };
        }

        [FunctionName("create-user")]
        public static async Task<HttpResponseMessage> Create(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "manager/users")]HttpRequestMessage req,
            [CosmosDB(
                databaseName: "bim42db",
                collectionName: "bimsyncManagerCollection",
                ConnectionStringSetting = "myDBConnectionString")]IAsyncCollector<User> usersOut,
            [CosmosDB(
                databaseName: "bim42db",
                collectionName: "bimsyncManagerCollection",
                ConnectionStringSetting = "myDBConnectionString")]DocumentClient client,
            ILogger log)
        {
            try
            {
                log.LogInformation("Creating a new user");

                // Get request body
                AuthorisationCode authorisationCode = await req.Content.ReadAsAsync<AuthorisationCode>();

                if (authorisationCode == null)
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent(JsonConvert.SerializeObject("Please pass an Authorisation Code and a redirect URI in the request body"), Encoding.UTF8, "application/json")
                    };
                }

                bimsync.AccessToken accessToken = await bimsync.bimsyncServices.GetAccessToken(authorisationCode);

                if (accessToken == null)
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent(JsonConvert.SerializeObject("Could not get an authorisation code from bimsync"), Encoding.UTF8, "application/json")
                    };
                }

                bimsync.User bimsyncUser = await bimsync.bimsyncServices.GetCurrentUser(accessToken);

                Uri collectionUri = UriFactory.CreateDocumentCollectionUri("bim42db", "bimsyncManagerCollection");
                IEnumerable<User> existingUsers = client.CreateDocumentQuery<User>(collectionUri).Where(u => u.id == bimsyncUser.id).AsEnumerable();

                string secret = System.Guid.NewGuid().ToString();
                string bcfToken = "";
                if (existingUsers.Count() != 0)
                {
                    secret = existingUsers.FirstOrDefault().PowerBISecret;
                    bcfToken = existingUsers.FirstOrDefault().BCFToken;
                }

                User user = new User
                {
                    id = bimsyncUser.id,
                    Name = bimsyncUser.name,
                    PowerBISecret = secret,
                    AccessToken = accessToken,
                    RefreshDate = System.DateTime.Now + new System.TimeSpan(0, 0, accessToken.expires_in),
                    BCFToken = bcfToken
                };

                await usersOut.AddAsync(user);

                string jsonContent = JsonConvert.SerializeObject(user);

                return user == null
                    ? new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("The user has not been created", Encoding.UTF8, "application/json") }
                    : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(jsonContent, Encoding.UTF8, "application/json") };
            }
            catch (Exception ex)
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent(ex.Message, Encoding.UTF8, "application/json") };
            }
        }

        [FunctionName("get-user")]
        public static async Task<HttpResponseMessage> Get(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "manager/users/{secret}")]HttpRequestMessage req,
            [CosmosDB(
                databaseName: "bim42db",
                collectionName: "bimsyncManagerCollection",
                ConnectionStringSetting = "myDBConnectionString",
                SqlQuery = "select * from bim42db u where u.PowerBISecret = {secret}")]IEnumerable<User> users,
            [CosmosDB(
                databaseName: "bim42db",
                collectionName: "bimsyncManagerCollection",
                ConnectionStringSetting = "myDBConnectionString")]IAsyncCollector<User> usersOut,
            ILogger log)
        {
            try
            {
                log.LogInformation("Getting an existing user");

                if (users.Count() != 0)
                {
                    User user = users.FirstOrDefault();
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

                        await usersOut.AddAsync(user);
                    }

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                                JsonConvert.SerializeObject(user),
                                Encoding.UTF8,
                                "application/json"
                                )
                    };
                }
                else
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent(JsonConvert.SerializeObject("The user does not exist"), Encoding.UTF8, "application/json")
                    };
                }
            }
            catch (Exception ex)
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent(ex.Message, Encoding.UTF8, "application/json") };
            }

        }

        [FunctionName("get-bcf-token")]
        public static async Task<HttpResponseMessage> bcf(
[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "manager/users/{secret}/bcf")]HttpRequest req,
[CosmosDB(
                databaseName: "bim42db",
                collectionName: "bimsyncManagerCollection",
                ConnectionStringSetting = "myDBConnectionString",
                SqlQuery = "select * from bim42db u where u.PowerBISecret = {secret}")]IEnumerable<User> users,
[CosmosDB(
                databaseName: "bim42db",
                collectionName: "bimsyncManagerCollection",
                ConnectionStringSetting = "myDBConnectionString")]IAsyncCollector<User> usersOut,
ILogger log)
        {
            try
            {
                log.LogInformation("Getting a bcf token");

                if (users.Count() != 0)
                {
                    User user = users.FirstOrDefault();

                    string code = req.Query["code"];

                    bimsync.BCFToken bcfToken = await bimsync.bimsyncServices.GetBCFToken(code);

                    if (bcfToken == null)
                    {
                        return new HttpResponseMessage(HttpStatusCode.BadRequest)
                        {
                            Content = new StringContent(JsonConvert.SerializeObject("Could not get an authorisation code from bimsync"), Encoding.UTF8, "application/json")
                        };
                    }

                    user.BCFToken = bcfToken.access_token;

                    await usersOut.AddAsync(user);

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                JsonConvert.SerializeObject(user),
                Encoding.UTF8,
                "application/json"
                )
                    };
                }
                else
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent(JsonConvert.SerializeObject("The user does not exist"), Encoding.UTF8, "application/json")
                    };
                }
            }
            catch (Exception ex)
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent(ex.Message, Encoding.UTF8, "application/json") };
            }
        }

        [FunctionName("get-page-number")]
        public static async Task<HttpResponseMessage> Pages(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "manager/users/{secret}/pages")]HttpRequest req,
    [CosmosDB(
                databaseName: "bim42db",
                collectionName: "bimsyncManagerCollection",
                ConnectionStringSetting = "myDBConnectionString",
                SqlQuery = "select * from bim42db u where u.PowerBISecret = {secret}")]IEnumerable<User> users,
    [CosmosDB(
                databaseName: "bim42db",
                collectionName: "bimsyncManagerCollection",
                ConnectionStringSetting = "myDBConnectionString")]IAsyncCollector<User> usersOut,
    ILogger log)
        {
            try
            {
                log.LogInformation("Getting the number of pages");

                if (users.Count() != 0)
                {
                    User user = users.FirstOrDefault();
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

                        await usersOut.AddAsync(user);
                    }

                    string ressource = req.Query["ressource"];
                    string revision = req.Query["revision"];

                    Page page = new Page
                    {
                        PageNumber = bimsync.bimsyncServices.GetPageNumber(ressource, revision, user.AccessToken.access_token).Result
                    };

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                JsonConvert.SerializeObject(page),
                Encoding.UTF8,
                "application/json"
                )
                    };
                }
                else
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent(JsonConvert.SerializeObject("The user does not exist"), Encoding.UTF8, "application/json")
                    };
                }

            }
            catch (Exception ex)
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent(ex.Message, Encoding.UTF8, "application/json") };
            }

        }

        [FunctionName("create-shared-model")]
        public static async Task<HttpResponseMessage> Share(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "manager/users/{secret}/share")]HttpRequestMessage req,
    [CosmosDB(
                databaseName: "bim42db",
                collectionName: "bimsyncManagerCollection",
                ConnectionStringSetting = "myDBConnectionString",
                SqlQuery = "select * from bim42db u where u.PowerBISecret = {secret}")]IEnumerable<User> users,
    [CosmosDB(
                databaseName: "bim42db",
                collectionName: "bimsyncManagerCollection",
                ConnectionStringSetting = "myDBConnectionString")]IAsyncCollector<User> usersOut,
    [CosmosDB(
                databaseName: "bim42db",
                collectionName: "bimsyncManagerCollection",
                ConnectionStringSetting = "myDBConnectionString")]IAsyncCollector<SharingCode> sharingCodesOut,
    ILogger log)
        {
            try
            {
                log.LogInformation("Create a shared model");

                // Get request body
                SharedRevisions sharedRevisions = await req.Content.ReadAsAsync<SharedRevisions>();

                if (sharedRevisions == null)
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent(JsonConvert.SerializeObject("Please pass an array of revisions to be shared"), Encoding.UTF8, "application/json")
                    };
                }

                if (users.Count() != 0)
                {
                    User user = users.FirstOrDefault();
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

                        await usersOut.AddAsync(user);
                    }

                    List<bimsync.Revision> revisions = await bimsync.bimsyncServices.GetRevisions(user.AccessToken, sharedRevisions.ProjectId);
                    List<bimsync.Model> sharedModels = revisions.Where(r => sharedRevisions.Revisions3D.Contains(r.id)).Select(o => o.model).ToList();

                    List<string> spacesIds = new List<string>();
                    foreach (string revision in sharedRevisions.Revisions3D)
                    {
                        spacesIds.AddRange(await bimsync.bimsyncServices.GetSpacesIds(user.AccessToken, sharedRevisions.ProjectId, revision));
                    }

                    SharingCode sharingCode = await Services.CreateSharingCode(
                        sharedRevisions,
                        user,
                        sharedModels,
                        spacesIds,
                        Guid.NewGuid().ToString()
                    );

                    await sharingCodesOut.AddAsync(sharingCode);

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
                        Content = new StringContent(JsonConvert.SerializeObject("The user does not exist"), Encoding.UTF8, "application/json")
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
