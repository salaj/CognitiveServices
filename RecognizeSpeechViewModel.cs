using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.CognitiveServices.Speech;
using Newtonsoft.Json.Linq;
using STT;

namespace WPF_Application
{
    public class RecognizeSpeechViewModel
    {
        DispatcherOperation RunOnUIThread(Action action) => Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Normal, action);

        public readonly RecognizeSpeech recognizeSpeech;

        private TextBlock MySpeechResponse;
        private ComboBox SupportedLanguages;
        private TextBlock MySpeechResponseFromFile;
        private TextBlock searchImageText;

        public RecognizeSpeechViewModel(string subscriptionKey, string region, TextBlock mySpeechResponse, ComboBox supportedLanguages, TextBlock mySpeechResponseFromFile, TextBlock searchImageText)
        {
            MySpeechResponse = mySpeechResponse;
            SupportedLanguages = supportedLanguages;
            MySpeechResponseFromFile = mySpeechResponseFromFile;
            this.searchImageText = searchImageText;
            this.recognizeSpeech = new RecognizeSpeech(subscriptionKey, region);
        }

        public async Task<SpeechRecognitionResult> RecognizeFromMicrophoneInput()
        {
            await RunOnUIThread(() => MySpeechResponse.Text = "Listening...");

            var result = await recognizeSpeech.RecognizeOnceAsync(SupportedLanguages.Text);

            HandleResult(result, MySpeechResponse);

            return result;
        }

        public async Task<SpeechRecognitionResult> RecognizeSpeechAsyncFromFile(string fileName)
        {
            await RunOnUIThread(() => MySpeechResponseFromFile.Text = "Recognizing...");

            var result = await recognizeSpeech.RecognizeOnceAsyncFromFile(SupportedLanguages.Text, fileName);

            HandleResult(result, MySpeechResponseFromFile);

            return result;
        }


        public async Task<JObject> RecognizeSpeechFromFileRESTApi(string fileName)
        {
            await RunOnUIThread(() => MySpeechResponseFromFile.Text = "Recognizing...");

            var result = await recognizeSpeech.RecognizeOnceAsyncFromFileRESTApi(SupportedLanguages.Text, fileName);

            return result;
        }

        public bool HandleResultFromFile(SpeechRecognitionResult result)
        {
            return HandleResult(result, MySpeechResponseFromFile);
        }

        private bool HandleResult(SpeechRecognitionResult result, TextBlock responseTextBlock)
        {
            if (result.Reason == ResultReason.RecognizedSpeech)
            {
                responseTextBlock.Text = $"We recognized: {result.Text}";
                searchImageText.Text = string.Empty;
                return true;
            }
            else if (result.Reason == ResultReason.NoMatch)
            {
                responseTextBlock.Text = $"NOMATCH: Speech could not be recognized.";
            }
            else if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = CancellationDetails.FromResult(result);
                var toReturn = $"CANCELED: Reason={cancellation.Reason}\n";

                if (cancellation.Reason == CancellationReason.Error)
                {
                    toReturn += $"CANCELED: ErrorCode={cancellation.ErrorCode} \n";
                    toReturn += $"CANCELED: ErrorDetails={cancellation.ErrorDetails} \n";
                    toReturn += $"CANCELED: Did you update the subscription info? \n";
                }

                responseTextBlock.Text = toReturn;
            }

            return false;
        }
    }
}
