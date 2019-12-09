using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using TTS;

namespace WPF_Application
{
    public class SynthesizeTextViewModel
    {
        private TTS.SpeechToText speechToText;

        public SynthesizeTextViewModel(string subscriptionKey, string region)
        {
            this.speechToText = new SpeechToText(subscriptionKey, region);
        }

        private static object locker = new object();

        public async Task SynthesisToSpeakerAsync(string speechLanguage, string textToSynthesize)
        {
            if (Monitor.TryEnter(locker))
            {
                var result = await speechToText.SynthesisToSpeakerAsync(speechLanguage, textToSynthesize);

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
                Monitor.Exit(locker);
            }
        }
    }
}
