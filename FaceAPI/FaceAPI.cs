using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;

namespace FaceAPI
{
    public class FaceAPI
    {
        private string subscriptionKey;
        private string faceEndpoint;

        private string imageFilePath = "emotions.jpg";

        public FaceAPI(string subscriptionKey, string faceEndpoint)
        {
            this.faceEndpoint = faceEndpoint;
            this.subscriptionKey = subscriptionKey;
        }


        public async Task<IList<DetectedFace>> DetectWithStreamAsync(Stream imageFileStream)
        {
            IFaceClient faceClient = new FaceClient(new ApiKeyServiceClientCredentials(subscriptionKey));
            faceClient.Endpoint = faceEndpoint;

            // The list of Face attributes to return.
            IList<FaceAttributeType> faceAttributes =
                new FaceAttributeType[]
                {
                    FaceAttributeType.Gender, FaceAttributeType.Age,
                    FaceAttributeType.Smile, FaceAttributeType.Emotion,
                    FaceAttributeType.Glasses, FaceAttributeType.Hair
                };

            // Call the Face API.
            try
            {
                //using (Stream i = File.OpenRead(imageFilePath))
                //{
                    //The second argument specifies to return the faceId, while

                    //the third argument specifies not to return face landmarks.
                   IList<DetectedFace> faceList =
                       await faceClient.Face.DetectWithStreamAsync(
                           imageFileStream, true, false, faceAttributes);
                    return faceList;
                //}
            }
            // Catch and display Face API errors.
            catch (APIErrorException f)
            {
                return new List<DetectedFace>();
            }
            // Catch and display all other errors.
            catch (Exception e)
            {
                return new List<DetectedFace>();
            }
        }


        public HighestEmotion GetHighestEmotion(DetectedFace face)
        {
            var maxConfidence = Single.MinValue;
            var maxConfidenceEmotionName = String.Empty;

            Type emotionAttributesType = typeof(Emotion);
            IEnumerable<string> emotionAttributesProperties = emotionAttributesType.GetProperties().Select(x => x.Name);
            foreach (var propertyName in emotionAttributesProperties)
            {
                PropertyInfo confidenceProperty = emotionAttributesType.GetProperty(propertyName);
                if (confidenceProperty != null)
                {
                    var castingOk = Single.TryParse(confidenceProperty.GetValue(face.FaceAttributes.Emotion)?.ToString(), out var confidence);
                    if (castingOk && confidence > maxConfidence)
                    {
                        maxConfidence = confidence;
                        maxConfidenceEmotionName = propertyName;
                    }
                }
            }
            return new HighestEmotion(maxConfidenceEmotionName, maxConfidence);
        }

        public class HighestEmotion
        {
            public float maxConfidence { get; }
            public string maxConfidenceEmotionName { get; }

            public HighestEmotion(string maxConfidenceEmotionName, float maxConfidence)
            {
                this.maxConfidenceEmotionName = maxConfidenceEmotionName;
                this.maxConfidence = maxConfidence;
            }
        }
    }
}
