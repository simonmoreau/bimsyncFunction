using System;
using System.Collections.Generic;
using System.Linq;
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

        public static async Task<AccessToken> GetAccessToken(AuthorisationCode authorisationCode)
        {
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));

            string bodyContent = $"grant_type=authorization_code" +
                    $"&code={authorisationCode.AuthorizationCode}" +
                    $"&redirect_uri={authorisationCode.RedirectURI}" +
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
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));

            string bodyContent = $"grant_type=refresh_token" +
                    $"&refresh_token={refresh_token}" +
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

        public static async Task<BCFToken> GetBCFToken(string authorisationCode)
        {
            httpClient.DefaultRequestHeaders.Accept.Clear();

            string bodyContent = $"client_id={Services.GetEnvironmentVariable("bimsync-client")}" +
                    $"&client_secret={Services.GetEnvironmentVariable("bimsync-secret")}" +
                    $"&code={authorisationCode}" +
                    "&grant_type=authorization_code";

            HttpContent body = new StringContent(bodyContent, System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");

            string clientURL = "https://api.bimsync.com/1.0/oauth/access_token";

            HttpResponseMessage response = await httpClient.PostAsync(clientURL, body);

            response.EnsureSuccessStatusCode();

            string responseString = await response.Content.ReadAsStringAsync();
            BCFToken bcfToken = (BCFToken)JsonConvert.DeserializeObject(responseString, typeof(BCFToken));

            return bcfToken;
        }

        public static async Task<bimsync.User> GetCurrentUser(AccessToken accessToken)
        {
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken.access_token);

            HttpResponseMessage response = await httpClient.GetAsync("https://api.bimsync.com/v2/user");

            response.EnsureSuccessStatusCode();

            string responseString = await response.Content.ReadAsStringAsync();
            bimsync.User bimsyncUser = (bimsync.User)JsonConvert.DeserializeObject(responseString, typeof(bimsync.User));

            return bimsyncUser;
        }

        public static async Task<int> GetPageNumber(string ressource, string revision, string access_token)
        {
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + access_token);

            string query = "https://api.bimsync.com/v2/";
            if (ressource.Contains("?"))
            {
                query = query + ressource + "&page=1&pageSize=1";
            }
            else
            {
                query = query + ressource + "?page=1&pageSize=1";
            }
            if (revision != null && revision != "") query = query + "&revision=" + revision;

            HttpResponseMessage response = await httpClient.GetAsync(query);

            string responseString = await response.Content.ReadAsStringAsync();

            response.EnsureSuccessStatusCode();

            // parse response headers
            KeyValuePair<string, IEnumerable<string>> link = response.Headers
               .FirstOrDefault(q => string.Compare(q.Key, "Link", true) == 0);

            if (link.Key == null) return 999;

            string linkValue = link.Value.FirstOrDefault().ToString();
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

            if (pTo == 0) return 0;

            String result = url.Substring(pFrom, pTo - pFrom);

            return Convert.ToInt32(result);
        }

        public static async Task<ViewerToken> Viewer2dToken(AccessToken accessToken, string projectId, string revisionId)
        {
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken.access_token);

            HttpContent body = new StringContent("", System.Text.Encoding.UTF8, "application/json");

            string clientURL = $"https://api.bimsync.com/v2/projects/{projectId}/viewer2d/token";

            if (!string.IsNullOrEmpty(revisionId)){
                clientURL = clientURL + $"?revision={revisionId}";
            }

            HttpResponseMessage response = await httpClient.PostAsync(clientURL, body);

            response.EnsureSuccessStatusCode();

            string responseString = await response.Content.ReadAsStringAsync();
            ViewerToken viewerToken = (ViewerToken)JsonConvert.DeserializeObject(responseString, typeof(ViewerToken));

            return viewerToken;
        }

    }
}