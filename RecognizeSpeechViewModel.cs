using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.CognitiveServices.Speech;
using STT;

namespace WPF_Application
{
    public class RecognizeSpeechViewModel
    {
        DispatcherOperation RunOnUIThread(Action action) => Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Normal, action);

        public readonly RecognizeSpeech recognizeSpeech;

        public RecognizeSpeechViewModel(string subscriptionKey, string region)
        {
            this.recognizeSpeech = new RecognizeSpeech(subscriptionKey, region);
        }

        public async Task<SpeechRecognitionResult> RecognizeFromString(TextBlock MySpeechResponse, ComboBox SupportedLanguages)
        {
            await RunOnUIThread(() => MySpeechResponse.Text = "Listening...");

            var result = await recognizeSpeech.RecognizeSpeechAsync(SupportedLanguages.Text);
            return result;
        }

        public async Task<SpeechRecognitionResult> RecognizeFromFile(TextBlock MySpeechResponseFromFile, ComboBox SupportedLanguages)
        {
            await RunOnUIThread(() => MySpeechResponseFromFile.Text = "Recognizing...");

            var result = await recognizeSpeech.RecognizeSpeechAsyncFromFile(SupportedLanguages.Text);
            return result;
        }

        public bool handleOutput(SpeechRecognitionResult result, TextBlock textBlock, TextBlock searchImageText)
        {
            if (result.Reason == ResultReason.RecognizedSpeech)
            {
                textBlock.Text = $"We recognized: {result.Text}";
                searchImageText.Text = string.Empty;
                return true;
            }
            else if (result.Reason == ResultReason.NoMatch)
            {
                textBlock.Text = $"NOMATCH: Speech could not be recognized.";
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

                textBlock.Text = toReturn;
            }

            return false;
        }
    }
}
