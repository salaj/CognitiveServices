using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.CognitiveServices.Speech;

namespace WPF_Application
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly RecognizeSpeechViewModel recognizeSpeechViewModel;
        private readonly FaceViewModel faceViewModel;
        private readonly WebSearchViewModel webSearchViewModel;
        private readonly SynthesizeTextViewModel synthesizeTextViewModel;

        private readonly string subscriptionKey;
        private readonly string region;
        private readonly string endpoint;

        public MainWindow()
        {
            InitializeComponent();

            subscriptionKey = ConfigurationManager.AppSettings["AzureCognitiveServiceSubscriptionKey"];
            region = ConfigurationManager.AppSettings["AzureRegion"];
            endpoint = string.Format(ConfigurationManager.AppSettings["AzureCognitiveServiceEndpoint"], region);

            recognizeSpeechViewModel = new RecognizeSpeechViewModel(subscriptionKey, region, MySpeechResponse, SupportedLanguages, MySpeechResponseFromFile, searchImageText);
            webSearchViewModel = new WebSearchViewModel(subscriptionKey, endpoint, searchImage, searchImageText);
            faceViewModel = new FaceViewModel(subscriptionKey, endpoint, MySpeechResponseFromFile, LoadingBar, searchImage, searchImageText);
            synthesizeTextViewModel = new SynthesizeTextViewModel(subscriptionKey, region, MyFaceEmotionReponse, SupportedLanguages);

        }

        DispatcherOperation RunOnUIThread(Action action) => Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Normal, action);

        private async void button_FromFile_Click(object sender, RoutedEventArgs e)
        {
            var result = await recognizeSpeechViewModel.RecognizeFromFile();
            recognizeSpeechViewModel.HandleResultFromFile(result);
        }

        private async void button_FromFile_REST_Click(object sender, RoutedEventArgs e)
        {
            var result = recognizeSpeechViewModel.recognizeSpeech.RecognizeSpeechFromFileRESTApi(SupportedLanguages.Text);

            if (result["RecognitionStatus"].ToString() == "Success")
            {
                await RunOnUIThread(() =>
                {
                    this.MySpeechResponseConfidenceFromFile.Text = $"With confidence {result["NBest"][0]["Confidence"]:0.##}";
                    this.MySpeechResponseFromFile.Text = $"We recognized: {result["NBest"][0]["Display"]}";

                    this.LoadingBar.Visibility = Visibility.Visible;
                    this.searchImage.Visibility = Visibility.Collapsed;
                });
                await webSearchViewModel.ProcessWebSearchREST(result["NBest"][0]["Display"].ToString());

                await RunOnUIThread(() =>
                {
                    this.LoadingBar.Visibility = Visibility.Collapsed;
                    this.searchImage.Visibility = Visibility.Visible;
                });
            }
        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            var result = await recognizeSpeechViewModel.RecognizeFromMicrophoneInput();
            var recognized = recognizeSpeechViewModel.HandleResultFromMic(result);
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
            await faceViewModel.DetectFacesInThePicture();
        }

        private async void FacePhoto_MouseMove(object sender, MouseEventArgs e)
        {
            // Find the mouse position relative to the image.
            Point mouseXY = e.GetPosition(searchImage);
            var detectedFace = faceViewModel.GetFacesDescription(mouseXY);

            if (detectedFace != null)
            {
                var highestEmotion = faceViewModel.GetHighestEmotion(detectedFace);
                var textToSynthesize =
                    $"I'm  {Convert.ToInt16(highestEmotion.maxConfidence * 100)}% sure that this person's emotion is {highestEmotion.maxConfidenceEmotionName}";
                await RunOnUIThread(() => MyFaceEmotionReponse.Text = textToSynthesize);
                var supportedLanguages = SupportedLanguages.Text;
                await Task.Run(()=>synthesizeTextViewModel.SynthesisToSpeakerAsync(supportedLanguages, textToSynthesize));
            }
        }

        private async void button_Synthesize_Click(object sender, RoutedEventArgs e)
        {
            var supportedLanguages = SupportedLanguages.Text;
            var textToSynthesize = TextSynthesize.Text;
            await Task.Run(() => synthesizeTextViewModel.SynthesisToSpeakerAsync(supportedLanguages, textToSynthesize));
        }

        private async void button_Full_Click(object sender, RoutedEventArgs e)
        {
            // STEP 1 - RECOGNIZE SPEECH FROM MICROPHONE INPUT
            SpeechRecognitionResult result = null;
            var recognized = false;
            if (recognized)
            {
                await RunOnUIThread(() => this.LoadingBar.Visibility = Visibility.Visible);
                // STEP 2 - GET INTENT BASED ON RECOGNIZED TEXT 
                var intent = await new LUIS.LUIS(subscriptionKey, endpoint).GetLuisIntent(result.Text);
                await RunOnUIThread(() =>
                {
                    this.MySpeechIntent.Text = $"Intent: '{intent.prediction.topIntent}',";
                    this.MySpeechIntentScore.Text = $"score: {intent.prediction.topScore:0.##}";
                });
                // STEP 3 - FIND IMAGE BASED ON RECOGNIZED TEXT
                
                if (intent.prediction.topIntent == "PeoplePictures")
                {
                    // STEP 4 - DETECT FACES IF THERE ARE PEOPLE IN PICTURE 
                    
                    // STEP 5 - READ LOUD HIGHEST SCORED EMOTION
                    FaceAPI.FaceAPI.HighestEmotion highestEmotion = null;

                }
                else
                {
                    faceViewModel.faceList = null;
                    await RunOnUIThread(() => this.MyFaceEmotionReponse.Text = string.Empty);
                }
                await RunOnUIThread(() => this.LoadingBar.Visibility = Visibility.Collapsed);
            }
        }
    }
}
