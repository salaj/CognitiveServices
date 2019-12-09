using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace WPF_Application
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly RecognizeSpeechViewModel rsvm;
        private readonly FaceViewModel fvm;
        private readonly WebSearchViewModel wsvm;
        private readonly SynthesizeTextViewModel stvm;

        private readonly string subscriptionKey;
        private readonly string region;
        private readonly string endpoint;

        public MainWindow()
        {
            InitializeComponent();

            subscriptionKey = ConfigurationManager.AppSettings["AzureCognitiveServiceSubscriptionKey"];
            region = ConfigurationManager.AppSettings["AzureRegion"];
            endpoint = string.Format(ConfigurationManager.AppSettings["AzureCognitiveServiceEndpoint"], region);

            rsvm = new RecognizeSpeechViewModel(subscriptionKey, region);
            wsvm = new WebSearchViewModel(subscriptionKey, endpoint);
            fvm = new FaceViewModel(subscriptionKey, endpoint);
            stvm = new SynthesizeTextViewModel(subscriptionKey, region);

        }

        DispatcherOperation RunOnUIThread(Action action) => Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Normal, action);

        private async void button_FromFile_Click(object sender, RoutedEventArgs e)
        {
            var result = await rsvm.RecognizeFromFile(MySpeechResponseFromFile, SupportedLanguages);
            rsvm.handleOutput(result, this.MySpeechResponseFromFile, searchImageText);
        }

        private async void button_FromFile_REST_Click(object sender, RoutedEventArgs e)
        {
            var result = rsvm.recognizeSpeech.RecognizeSpeechFromFileRESTApi(SupportedLanguages.Text);

            if (result["RecognitionStatus"].ToString() == "Success")
            {
                await RunOnUIThread(() =>
                {
                    this.MySpeechResponseConfidenceFromFile.Text = $"With confidence {result["NBest"][0]["Confidence"]:0.##}";
                    this.MySpeechResponseFromFile.Text = $"We recognized: {result["NBest"][0]["Display"]}";

                    this.LoadingBar.Visibility = Visibility.Visible;
                    this.searchImage.Visibility = Visibility.Collapsed;
                });

                await wsvm.ProcessWebSearchREST(result["NBest"][0]["Display"].ToString(), searchImage, searchImageText);

                await RunOnUIThread(() =>
                {
                    this.LoadingBar.Visibility = Visibility.Collapsed;
                    this.searchImage.Visibility = Visibility.Visible;
                });
            }
        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            var result = await rsvm.RecognizeFromString(MySpeechResponse, SupportedLanguages);
            var recognized = rsvm.handleOutput(result, this.MySpeechResponse, this.searchImageText);
            if (recognized)
            {
                await RunOnUIThread(() => this.LoadingBar.Visibility = Visibility.Visible);
                var intent = await new LUIS.LUIS(subscriptionKey, endpoint).GetLuisIntent(result.Text);
                await RunOnUIThread(() =>
                {
                    this.MySpeechIntent.Text = $"Intent: '{intent.prediction.topIntent}',";
                    this.MySpeechIntentScore.Text = $"score: {intent.prediction.topScore:0.##}";
                });
                await wsvm.ProcessWebSearch(result.Text, searchImage, searchImageText);
                if (intent.prediction.topIntent == "PeoplePictures")
                {
                    await fvm.DetectFaces(MyFaceResponse, LoadingBar, searchImage, searchImageText);
                }
                await RunOnUIThread(() => this.LoadingBar.Visibility = Visibility.Collapsed);
            }
        }



        private async void button_Face_FromFile_Click(object sender, RoutedEventArgs e)
        {
            var path = Path.Combine(Environment.CurrentDirectory, "emotions.jpg");
            var uri = new Uri(path);
            var bitmapSource = new BitmapImage(uri);

            await RunOnUIThread(() =>
            {
                this.searchImage.Source = bitmapSource;
            });
            await fvm.DetectFaces(MyFaceResponse, LoadingBar, searchImage, searchImageText);
        }

       
        private void FacePhoto_MouseMove(object sender, MouseEventArgs e)
        {
            // Find the mouse position relative to the image.
            Point mouseXY = e.GetPosition(searchImage);
            var detectedFace = fvm.GetFacesDescription(searchImage, searchImageText, mouseXY);

            if (detectedFace != null)
            {
                var highestEmotion = fvm.GetHighestEmotion(detectedFace);
                var textToSynthesize =
                    $"I'm  {Convert.ToInt16(highestEmotion.maxConfidence * 100)}% sure that this person's emotion is {highestEmotion.maxConfidenceEmotionName}";
                MyFaceEmotionReponse.Text = textToSynthesize;
                Task.Run(()=>stvm.SynthesisToSpeakerAsync(SupportedLanguages.Text, textToSynthesize));
            }
        }
    }
}
