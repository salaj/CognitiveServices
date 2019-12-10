using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;

namespace WPF_Application
{
    public class FaceViewModel
    {
        // The list of detected faces.
        public IList<DetectedFace> faceList;
        // The list of descriptions for the detected faces.
        private string[] faceDescriptions;
        // The resize factor for the displayed image.
        private double resizeFactor;
        private const string defaultStatusBarText =
            "Place the mouse pointer over a face to see the face description.";

        public FaceAPI.FaceAPI faceAPI;


        private TextBlock MyFaceResponseFromFile;
        private StackPanel LoadingBar;
        private Image searchImage;
        private TextBlock searchImageText;

        DispatcherOperation RunOnUIThread(Action action) => Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Normal, action);

        public FaceViewModel(string subscriptionKey, string endpoint, TextBlock myFaceResponseFromFile, StackPanel loadingBar, Image searchImage, TextBlock searchImageText)
        {
            MyFaceResponseFromFile = myFaceResponseFromFile;
            LoadingBar = loadingBar;
            this.searchImage = searchImage;
            this.searchImageText = searchImageText;
            faceAPI = new FaceAPI.FaceAPI(subscriptionKey, endpoint);
        }

        private void DrawRectangels()
        {
            var bitmapSource = searchImage.Source as BitmapSource;
            if (faceList.Count > 0)
            {
                // Prepare to draw rectangles around the faces.
                DrawingVisual visual = new DrawingVisual();
                DrawingContext drawingContext = visual.RenderOpen();
                drawingContext.DrawImage(bitmapSource,
                    new Rect(0, 0, bitmapSource.Width, bitmapSource.Height));
                double dpi = bitmapSource.DpiX;
                // Some images don't contain dpi info.
                resizeFactor = (dpi == 0) ? 1 : 96 / dpi;
                faceDescriptions = new String[faceList.Count];

                for (int i = 0; i < faceList.Count; ++i)
                {
                    DetectedFace face = faceList[i];

                    // Draw a rectangle on the face.
                    drawingContext.DrawRectangle(
                        Brushes.Transparent,
                        new Pen(Brushes.Red, 2),
                        new Rect(
                            face.FaceRectangle.Left * resizeFactor,
                            face.FaceRectangle.Top * resizeFactor,
                            face.FaceRectangle.Width * resizeFactor,
                            face.FaceRectangle.Height * resizeFactor
                        )
                    );

                    // Store the face description.
                    faceDescriptions[i] = FaceDescription(face);
                }

                drawingContext.Close();

                // Display the image with the rectangle around the face.
                RenderTargetBitmap faceWithRectBitmap = new RenderTargetBitmap(
                    (int)(bitmapSource.PixelWidth * resizeFactor),
                    (int)(bitmapSource.PixelHeight * resizeFactor),
                    96,
                    96,
                    PixelFormats.Pbgra32);

                faceWithRectBitmap.Render(visual);
                searchImage.Source = faceWithRectBitmap;

                // Set the status bar text.
                searchImageText.Text = defaultStatusBarText;
            }
        }

        public async Task DetectFacesInThePicture()
        {
            await RunOnUIThread(() =>
            {
                MyFaceResponseFromFile.Text = "Recognizing...";
                LoadingBar.Visibility = Visibility.Visible;
            });
            var bitmapImage = searchImage.Source as BitmapImage;
            WebClient client = new WebClient();
            using (var stream = client.OpenRead(bitmapImage.UriSource))
                faceList = await faceAPI.DetectFaces(stream);
            await RunOnUIThread(() =>
            {
                DrawRectangels();
                MyFaceResponseFromFile.Text = $"Detection Finished. {faceList.Count} face(s) detected";
                LoadingBar.Visibility = Visibility.Collapsed;
            });
        }

        // Creates a string out of the attributes describing the face.
        private string FaceDescription(DetectedFace face)
        {
            StringBuilder sb = new StringBuilder();

            // Add the gender, age, and smile.
            sb.AppendLine($"gender: {face.FaceAttributes.Gender.ToString()}");
            sb.AppendLine($"age: {face.FaceAttributes.Age.ToString()}");

            // Add the emotions. Display all emotions over 10%.
            sb.AppendLine("Emotion: ");
            Emotion emotionScores = face.FaceAttributes.Emotion;
            if (emotionScores.Anger >= 0.1f) sb.Append(
                String.Format("anger {0:F1}%, ", emotionScores.Anger * 100));
            if (emotionScores.Contempt >= 0.1f) sb.Append(
                String.Format("contempt {0:F1}%, ", emotionScores.Contempt * 100));
            if (emotionScores.Disgust >= 0.1f) sb.Append(
                String.Format("disgust {0:F1}%, ", emotionScores.Disgust * 100));
            if (emotionScores.Fear >= 0.1f) sb.Append(
                String.Format("fear {0:F1}%, ", emotionScores.Fear * 100));
            if (emotionScores.Happiness >= 0.1f) sb.Append(
                String.Format("happiness {0:F1}%, ", emotionScores.Happiness * 100));
            if (emotionScores.Neutral >= 0.1f) sb.Append(
                String.Format("neutral {0:F1}%, ", emotionScores.Neutral * 100));
            if (emotionScores.Sadness >= 0.1f) sb.Append(
                String.Format("sadness {0:F1}%, ", emotionScores.Sadness * 100));
            if (emotionScores.Surprise >= 0.1f) sb.Append(
                String.Format("surprise {0:F1}%, ", emotionScores.Surprise * 100));

            sb.AppendLine();
            // Add glasses.
            sb.AppendLine($"glasses: {face.FaceAttributes.Glasses}");

            // Add hair.
            sb.Append("Hair: ");

            // Display baldness confidence if over 1%.
            if (face.FaceAttributes.Hair.Bald >= 0.01f)
                sb.Append(String.Format("bald {0:F1}% ", face.FaceAttributes.Hair.Bald * 100));
            // Display all hair color attributes over 10%.
            IList<HairColor> hairColors = face.FaceAttributes.Hair.HairColor;
            foreach (HairColor hairColor in hairColors)
            {
                if (hairColor.Confidence >= 0.1f)
                {
                    sb.Append(hairColor.Color.ToString());
                    sb.Append(String.Format(" {0:F1}% ", hairColor.Confidence * 100));
                }
            }

            // Return the built string.
            return sb.ToString();
        }

        public DetectedFace GetFacesDescription(Point mouseXY)
        {
            // If the REST call has not completed, return.
            if (faceList == null)
                return null;

            ImageSource imageSource = searchImage.Source;
            BitmapSource bitmapSource = (BitmapSource)imageSource;

            // Scale adjustment between the actual size and displayed size.
            var scale = searchImage.ActualWidth / (bitmapSource.PixelWidth / resizeFactor);

            // Check if this mouse position is over a face rectangle.
            bool mouseOverFace = false;

            for (int i = 0; i < faceList.Count; ++i)
            {
                var fr = faceList[i].FaceRectangle;
                double left = fr.Left * scale;
                double top = fr.Top * scale;
                double width = fr.Width * scale;
                double height = fr.Height * scale;

                // Display the face description if the mouse is over this face rectangle.
                if (mouseXY.X >= left && mouseXY.X <= left + width &&
                    mouseXY.Y >= top && mouseXY.Y <= top + height)
                {
                    searchImageText.Text = faceDescriptions[i];
                    mouseOverFace = true;
                    return faceList[i];
                }
            }

            // String to display when the mouse is not over a face rectangle.
            if (!mouseOverFace) searchImageText.Text = defaultStatusBarText;

            return null;
        }

        public FaceAPI.FaceAPI.HighestEmotion GetHighestEmotion(DetectedFace face)
        {
            return faceAPI.GetHighestEmotion(face);
        }

        public FaceAPI.FaceAPI.HighestEmotion GetHighestEmotionForAnyDetectedFace()
        {
            if (faceList != null)
            {
                return faceAPI.GetHighestEmotion(faceList.First());
            }
            return null;
        }
    }
}
