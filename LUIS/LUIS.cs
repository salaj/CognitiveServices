using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;

namespace LUIS
{
    public class LUIS
    {
        private string appId = "7365609c-3538-44c4-9d83-8211bb58d269";
        private string endpoint;
        private string subscriptionKey;

        public LUIS(string subscriptionKey, string endpoint)
        {
            this.subscriptionKey = subscriptionKey;
            this.endpoint = endpoint;
        }

        public async Task<LuisIntent> GetLuisIntent(string utterance)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["query"] = utterance;

            // These optional request parameters are set to their default values
            queryString["verbose"] = "true";
            queryString["show-all-intents"] = "true";
            //queryString["staging"] = "false";
            queryString["timezoneOffset"] = "0";

            // Request parameters
            var uri = $"{endpoint}/luis/prediction/v3.0/apps/{appId}/slots/staging/predict?query={queryString}";

            var response = await client.GetAsync(uri);
            var json = await response.Content.ReadAsStringAsync();
            LuisIntent intent = JsonConvert.DeserializeObject<LuisIntent>(json);

            return intent;
        }

    }
}
