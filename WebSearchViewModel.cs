using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MyWebSearch;
using Newtonsoft.Json.Linq;

namespace WPF_Application
{
    public class WebSearchViewModel
    {
        DispatcherOperation RunOnUIThread(Action action) => Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Normal, action);

        private WebSearch webSearch;

        private Image searchImage;
        private TextBlock searchImageText;

        public WebSearchViewModel(string subscriptionKey, string endpoint, Image searchImage, TextBlock searchImageText)
        {
            this.searchImage = searchImage;
            this.searchImageText = searchImageText;
            this.webSearch = new WebSearch(subscriptionKey, endpoint);
        }

        public async Task ProcessWebSearchREST(string phrase)
        {
            try
            {
                var result = await webSearch.SearchImage(phrase);
                var json = JObject.Parse(result.jsonResult);
                var firstJsonObj = json["value"][0];
                var firstJsonObjUrl = firstJsonObj["contentUrl"];
                var uriSource = new Uri(firstJsonObjUrl.ToString());
                await RunOnUIThread(() => searchImage.Source = new BitmapImage(uriSource));
            }
            catch (Exception ex)
            {
                searchImageText.Text = "Didn't find any images...";
                Console.WriteLine(ex.Message);
            }
        }

        public async Task SearchAsync(string phrase)
        {
            var webData = await webSearch.SearchAsync(phrase);

            if (webData?.Images?.Value?.Count > 0)
            {
                // find the first image result
                var firstImageResult = webData.Images.Value.FirstOrDefault();

                if (firstImageResult != null)
                {
                    var uriSource = new Uri(firstImageResult.ContentUrl, UriKind.Absolute);
                    await RunOnUIThread(() => searchImage.Source = new BitmapImage(uriSource));
                }
                else
                {
                    searchImageText.Text = "Didn't find any images...";
                }
            }
            else
            {
                searchImageText.Text = "Didn't find any images...";
            }
        }
    }
}
