using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Newtonsoft.Json.Linq;

namespace STT
{
    public class RecognizeSpeech
    {
        private string subscriptionKey;
        private string region;

        private string audioFilePath = @"whatstheweatherlike.wav";
        private string audioFilePathPerson = @"Person.wav";

        public RecognizeSpeech(string subscriptionKey, string region)
        {
            this.subscriptionKey = subscriptionKey;
            this.region = region;
        }

        public async Task<SpeechRecognitionResult> RecognizeSpeechAsync(string speechRecognitionLanguage)
        {
            using (var recognizer = new SpeechRecognizer(getSpeechConfig(speechRecognitionLanguage)))
            {
                var result = await recognizer.RecognizeOnceAsync();

                return result;
            }
        }

        public async Task<SpeechRecognitionResult> RecognizeSpeechAsyncFromFile(string speechRecognitionLanguage)
        {
            using (var audioInput = AudioConfig.FromWavFileInput(audioFilePathPerson))
            {
                using (var recognizer = new SpeechRecognizer(getSpeechConfig(speechRecognitionLanguage), audioInput))
                {
                    var result = await recognizer.RecognizeOnceAsync();

                    return result;
                }
            }
        }
        
        public JObject RecognizeSpeechFromFileRESTApi(string speechRecognitionLanguage)
        {
            HttpWebRequest request = null;
            request = (HttpWebRequest)HttpWebRequest.Create($"https://{region}.stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1?language={speechRecognitionLanguage}&format=detailed");
            request.SendChunked = true;
            request.Accept = @"application/json;text/xml";
            request.Method = "POST";
            request.ProtocolVersion = HttpVersion.Version11;
            request.Host = "westeurope.stt.speech.microsoft.com";
            request.ContentType = @"audio/wav; codecs=audio/pcm; samplerate=16000";
            request.Headers["Ocp-Apim-Subscription-Key"] = subscriptionKey;
            request.AllowWriteStreamBuffering = false;

            using (var fs = new FileStream(audioFilePathPerson, FileMode.Open, FileAccess.Read))
            {
                /*
                * Open a request stream and write 1024 byte chunks in the stream one at a time.
                */
                byte[] buffer = null;
                int bytesRead = 0;
                using (Stream requestStream = request.GetRequestStream())
                {
                    /*
                    * Read 1024 raw bytes from the input audio file.
                    */
                    buffer = new Byte[checked((uint)Math.Min(1024, (int)fs.Length))];
                    while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        requestStream.Write(buffer, 0, bytesRead);
                    }

                    // Flush
                    requestStream.Flush();
                }

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                var encoding = ASCIIEncoding.ASCII;
                using (var reader = new StreamReader(response.GetResponseStream(), encoding))
                {
                    string responseText = reader.ReadToEnd();
                    return JObject.Parse(responseText);
                }
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
