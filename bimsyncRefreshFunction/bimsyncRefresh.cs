using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.IO;
using System.Threading.Tasks;
using System.Configuration;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using RestSharp;
using System.Xml.Serialization;

namespace bimsyncRefreshFunction
{
    public static class bimsyncRefresh
    {
        [FunctionName("bimsyncRefresh")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            // parse query parameter
            string toRefresh = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "refresh", true) == 0)
                .Value;

            // Get request body
            dynamic data = await req.Content.ReadAsAsync<object>();

            // Set name to query string or body data
            toRefresh = toRefresh ?? data?.refresh;

            if (toRefresh == "yes")
            {
                //Find existing token
                TokenWithDate token = ReadToken();

                if (DateTime.Now - token.RefreshDate > new TimeSpan(0,59,00))
                {
                    //Refresh it
                    token = RefreshToken(token);

                    if (token.token.access_token != null)
                    {
                        //Write the new token
                        WriteTokenDown(token);

                        log.Info("The token is now refreshed.");

                        return req.CreateResponse(HttpStatusCode.OK, token.token.access_token);
                    }
                    else
                    {
                        return req.CreateResponse(HttpStatusCode.InternalServerError, "Error while fetching the token");
                    }
                }
                else
                {
                    //Return the current token, still valid
                    return req.CreateResponse(HttpStatusCode.OK, token.token.access_token);
                }
            }
            else if (toRefresh == "start")
            {
                WriteTokenTemp();

                return req.CreateResponse(HttpStatusCode.OK, "The token has been written");
            }
            else
            {
                return req.CreateResponse(HttpStatusCode.OK, "The token has not being refreshed");
            }
        }

        private static void WriteTokenTemp()
        {
            Token token = new Token();

            token.access_token = "ZqySL9w2TQbK3JlWWhaj3e";
            token.token_type = "bearer";
            token.expires_in = 3599;
            token.refresh_token = "LdPPorAWIENjMTYyEMOwak";

            TokenWithDate tokenWithDate = new TokenWithDate();
            tokenWithDate.token = token;
            tokenWithDate.RefreshDate = DateTime.Now - new TimeSpan(0,58,0);

            WriteTokenDown(tokenWithDate);
        }


        private static TokenWithDate RefreshToken(TokenWithDate token)
        {

            string client_id = ConfigurationManager.AppSettings["client_id"];
            string client_secret = ConfigurationManager.AppSettings["client_secret"];
            //string client_id = GetEnvironmentVariable("client_id", EnvironmentVariableTarget.Process);
            //string client_secret = GetEnvironmentVariable("client_secret", EnvironmentVariableTarget.Process);

            RestClient client = new RestClient("https://api.bimsync.com");

            //Refresh token
            RestRequest refrechTokenRequest = new RestRequest("oauth2/token", Method.POST);
            //refrechTokenRequest.AddHeader("Authorization", "Bearer " + token.access_token);

            refrechTokenRequest.AddParameter("refresh_token", token.token.refresh_token);
            refrechTokenRequest.AddParameter("grant_type", "refresh_token");
            refrechTokenRequest.AddParameter("client_id", client_id);
            refrechTokenRequest.AddParameter("client_secret", client_secret);

            IRestResponse<Token> responseToken = client.Execute<Token>(refrechTokenRequest);

            if (responseToken.ErrorException != null)
            {
                string message = "Error retrieving your access token. " + responseToken.ErrorException.Message;
                return new TokenWithDate();
            }

            TokenWithDate newToken = new TokenWithDate();
            newToken.token = responseToken.Data;
            newToken.RefreshDate = DateTime.Now;
            return newToken;
        }

        private static void WriteTokenDown(TokenWithDate token)
        {
            var folder = Environment.ExpandEnvironmentVariables(@"%HOME%\data\MyFunctionAppData");
            var fullPath = Path.Combine(folder, "tokenFile.txt");
            Directory.CreateDirectory(folder); // noop if it already exists

            TextWriter writer = null;
            try
            {
                var serializer = new XmlSerializer(typeof(TokenWithDate));
                writer = new StreamWriter(fullPath, false);
                serializer.Serialize(writer, token);
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
        }

        private static TokenWithDate ReadToken()
        {
            var folder = Environment.ExpandEnvironmentVariables(@"%HOME%\data\MyFunctionAppData");
            var fullPath = Path.Combine(folder, "tokenFile.txt");
            Directory.CreateDirectory(folder); // noop if it already exists

            TextReader reader = null;
            try
            {
                var serializer = new XmlSerializer(typeof(TokenWithDate));
                reader = new StreamReader(fullPath);
                return (TokenWithDate)serializer.Deserialize(reader);
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
        }

    }

    [SettingsSerializeAs(SettingsSerializeAs.Xml)]
    public class Token
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public int expires_in { get; set; }
        public string refresh_token { get; set; }
        
    }

    public class TokenWithDate
    {
        public Token token { get; set; }
        public DateTime RefreshDate { get; set; }
    }
}
