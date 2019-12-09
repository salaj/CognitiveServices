using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;

namespace TTS
{
    public class SpeechToText
    {
        private string subscriptionKey;
        private string region;


        public SpeechToText(string subscriptionKey, string region)
        {
            this.subscriptionKey = subscriptionKey;
            this.region = region;
        }

        public async Task<SpeechSynthesisResult> SynthesisToSpeakerAsync(string speechLanguage, string textToSynthesize)
        {
            // Creates a speech synthesizer using the default speaker as audio output.
            using (var synthesizer = new SpeechSynthesizer(getSpeechConfig(speechLanguage)))
            {
                var result = await synthesizer.SpeakTextAsync(textToSynthesize);
                return result;
            }
        }

        private SpeechConfig getSpeechConfig(string speechRecognitionLanguage)
        {
            var config = SpeechConfig.FromSubscription(subscriptionKey, region);
            config.SpeechRecognitionLanguage = speechRecognitionLanguage;
            return config;
        }
    }
}
