using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using RestSharp;
using System;
using System.Configuration;

namespace bimsyncRefreshFunction
{
    public static class bimsyncPageNumber
    {
        [FunctionName("bimsyncPageNumber")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            // parse query parameter
            string ressource = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "ressource", true) == 0)
                .Value;

            string revision = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "revision", true) == 0)
                .Value;

            if (ressource == null)
            {
                req.CreateResponse(HttpStatusCode.OK, "Please provide a bimsync API ressource to get the page number");
            }

            //Get the token
            TokenWithDate token = GetToken();
            //string accessToken = ConfigurationManager.AppSettings["tempaccessToken"];
            string pageNumber = null;

            if (token != null)
            {
                pageNumber = GetPageNumber(ressource, revision, token.token.access_token);
            }

            return pageNumber == null
                ? req.CreateResponse(HttpStatusCode.InternalServerError, "Something went wrong while fetching your page number")
                : req.CreateResponse(HttpStatusCode.OK, pageNumber);
        }

        private static string GetPageNumber(string ressource, string revision, string access_token)
        {
            RestClient client = new RestClient("https://api.bimsync.com/v2/");

            //Get a generic ressource with only one item
            RestRequest genericRequest = new RestRequest(ressource, Method.GET);
            genericRequest.AddHeader("Authorization", "Bearer " + access_token);
            genericRequest.AddParameter("page", "1");
            genericRequest.AddParameter("pageSize", "1");
            if (revision != null) genericRequest.AddParameter("revision", revision);

            IRestResponse response = client.Execute(genericRequest);

            // parse response headers
            Parameter link = response.Headers
                .FirstOrDefault(q => string.Compare(q.Name, "Link", true) == 0);

            if (link == null) return null;

            string linkValue = link.Value.ToString();
            string[] values = linkValue.Split(',');
            string url = values.FirstOrDefault(x => x.Contains("rel=\"last\""));

            int pFrom = url.IndexOf("&page=") + "&page=".Length;
            int pTo = 0;
            if (url.IndexOf("&", pFrom) != -1)
            {
                pTo = url.IndexOf("&", pFrom);
            }
            else
            {
                pTo = url.IndexOf(">", pFrom);
            }

            if (pTo == 0) return null;

            String result = url.Substring(pFrom, pTo - pFrom);

            return result;
        }

        private static TokenWithDate GetToken()
        {
            //Find existing token
            TokenWithDate token = bimsyncRefresh.ReadToken();

            if (DateTime.Now - token.RefreshDate > new TimeSpan(0, 59, 00))
            {
                //Refresh it
                token = bimsyncRefresh.RefreshToken(token);

                if (token.token.access_token != null)
                {
                    //Write the new token
                    bimsyncRefresh.WriteTokenDown(token);

                    return token;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                //Return the current token, still valid
                return token;
            }
        }
    }
}
