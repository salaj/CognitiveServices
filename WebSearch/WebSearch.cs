using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Search.WebSearch;
using Microsoft.Azure.CognitiveServices.Search.WebSearch.Models;

namespace MyWebSearch
{
    public class WebSearch
    {
        private string subscriptionKey;
        private string endpoint;

        public WebSearch(string subscriptionKey, string endpoint)
        {
            this.subscriptionKey = subscriptionKey;
            this.endpoint = endpoint;
        }

        public async Task<SearchResponse> WebResults(string phrase)
        {
            IList<string> promoteAnswertypeStrings = new List<string>() { "images" };
            var client = new WebSearchClient(new ApiKeyServiceClientCredentials(subscriptionKey));
            client.Endpoint = endpoint;
            try
            {
                var webData = await client.Web.SearchAsync(query: phrase, answerCount:2, promote: promoteAnswertypeStrings);
                return webData;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Encountered exception. " + ex.Message);
                return null;
            }
        }

        public struct SearchResult
        {
            public String jsonResult;
            public Dictionary<String, String> relevantHeaders;
        }

        public async Task<SearchResult> SearchImage(string phraseToSearch)
        {
            string uriBase = $"{endpoint}bing/v7.0/images/search";
            var uriQuery = uriBase + "?q=" + Uri.EscapeDataString(phraseToSearch);

            WebRequest request = WebRequest.Create(uriQuery);
            request.Headers["Ocp-Apim-Subscription-Key"] = subscriptionKey;
            var response = await request.GetResponseAsync() as HttpWebResponse;
            string json = new StreamReader(response.GetResponseStream()).ReadToEnd();

            // Create the result object for return
            var searchResult = new SearchResult()
            {
                jsonResult = json,
                relevantHeaders = new Dictionary<String, String>()
            };

            // Extract Bing HTTP headers
            foreach (String header in response.Headers)
            {
                if (header.StartsWith("BingAPIs-") || header.StartsWith("X-MSEdge-"))
                    searchResult.relevantHeaders[header] = response.Headers[header];
            }
            return searchResult;
        }
    }
}
