using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.CognitiveServices.Speech;
using TTS;

namespace WPF_Application
{
    public class SynthesizeTextViewModel
    {
        private TTS.SpeechToText speechToText;

        private TextBlock MyFaceEmotionReponse;
        private ComboBox SupportedLanguages;

        DispatcherOperation RunOnUIThread(Action action) => Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Normal, action);

        public SynthesizeTextViewModel(string subscriptionKey, string region, TextBlock myFaceEmotionReponse, ComboBox supportedLanguages)
        {
            MyFaceEmotionReponse = myFaceEmotionReponse;
            SupportedLanguages = supportedLanguages;
            this.speechToText = new SpeechToText(subscriptionKey, region);
        }

        private static object locker = new object();

        public void SynthesisToSpeakerAsync(string speechLanguage, string textToSynthesize)
        {
            if (Monitor.TryEnter(locker))
            {
                try
                {
                    var result = speechToText.SynthesisToSpeakerAsync(speechLanguage, textToSynthesize).Result;

                    if (result.Reason == ResultReason.SynthesizingAudioCompleted)
                    {
                        Console.WriteLine($"Speech synthesized to speaker for text [{textToSynthesize}]");
                    }
                    else if (result.Reason == ResultReason.Canceled)
                    {
                        var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                        Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                        if (cancellation.Reason == CancellationReason.Error)
                        {
                            Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                            Console.WriteLine($"CANCELED: ErrorDetails=[{cancellation.ErrorDetails}]");
                            Console.WriteLine($"CANCELED: Did you update the subscription info?");
                        }
                    }
                }
                finally
                {
                    Monitor.Exit(locker);
                }
            }
        }

        public async Task ReadHighestEmotion(FaceAPI.FaceAPI.HighestEmotion highestEmotion)
        {
            var textToSynthesize =
                $"I'm  {Convert.ToInt16(highestEmotion.maxConfidence * 100)}% sure that this person's emotion is {highestEmotion.maxConfidenceEmotionName}";
            await RunOnUIThread(() => MyFaceEmotionReponse.Text = textToSynthesize);
            var supportedLanguages = SupportedLanguages.Text;
            await Task.Run(() => SynthesisToSpeakerAsync(supportedLanguages, textToSynthesize));
        }
    }
}
