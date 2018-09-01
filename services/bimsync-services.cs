using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace bimsyncFunction.bimsync
{
    public class bimsyncServices
    {
        // Create a single, static HttpClient
        private static HttpClient httpClient = new HttpClient();

        public static async Task<AccessToken> GetAccessToken(string authorisationCode)
        {
            string callbackUri = "https://www.getpostman.com/oauth2/callback";
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));

            string bodyContent = $"grant_type=authorization_code" +
                    $"&code={authorisationCode}" +
                    $"&redirect_uri={callbackUri}" +
                    $"&client_id={Services.GetEnvironmentVariable("bimsync-client")}" +
                    $"&client_secret={Services.GetEnvironmentVariable("bimsync-secret")}";

            HttpContent body = new StringContent(bodyContent, System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");

            string clientURL = "https://api.bimsync.com/oauth2/token";

            HttpResponseMessage response = await httpClient.PostAsync(clientURL, body);

            response.EnsureSuccessStatusCode();

            string responseString = await response.Content.ReadAsStringAsync();
            AccessToken accessToken = (AccessToken)JsonConvert.DeserializeObject(responseString, typeof(AccessToken));

            return accessToken;
        }

        public static async Task<AccessToken> RefreshAccessToken(string refresh_token)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));

            string bodyContent = $"grant_type=refresh_token" +
                    $"&refresh_token={refresh_token}" +
                    $"&client_id={Services.GetEnvironmentVariable("client_id")}" +
                    $"&client_secret={Services.GetEnvironmentVariable("client_secret")}";

            HttpContent body = new StringContent(bodyContent, System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");

            string clientURL = "https://api.bimsync.com/oauth2/token";

            HttpResponseMessage response = await client.PostAsync(clientURL, body);

            string responseString = await response.Content.ReadAsStringAsync();
            AccessToken accessToken = (AccessToken)JsonConvert.DeserializeObject(responseString, typeof(AccessToken));

            return accessToken;
        }

        public static async Task<bimsync.User> GetCurrentUser(AccessToken accessToken)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken.access_token);

            HttpResponseMessage response = await client.GetAsync("https://api.bimsync.com/v2/user");

            string responseString = await response.Content.ReadAsStringAsync();
            bimsync.User bimsyncUser = (bimsync.User)JsonConvert.DeserializeObject(responseString, typeof(bimsync.User));

            return bimsyncUser;
        }
    }
}