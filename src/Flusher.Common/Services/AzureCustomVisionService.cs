using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Flusher.Common.Models;
using Newtonsoft.Json;

namespace Flusher.Common.Services
{
    public class AzureCustomVisionService : IDisposable
    {
        private readonly HttpClient client;

        public AzureCustomVisionService(string customVisionApiKey, string defaultIterationName = "Iteration4")
        {
            var handler = new HttpClientHandler();

            if (handler.SupportsAutomaticDecompression)
            {
                handler.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            }

            client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://eastus2.api.cognitive.microsoft.com")
            };

            // Set Prediction-Key Header to : CustomVisionApiKey
            client.DefaultRequestHeaders.Add("Prediction-Key", customVisionApiKey);

            //Sets the default iteration name of the published service
            IterationName = defaultIterationName;
        }

        public string IterationName { get; set; }

        /// <summary>
        /// Evaluates an image using a trained and published Custom Vision API. 
        /// </summary>
        /// <param name="imgUrl">A url to the online image to be evaluated.</param>
        /// <param name="iterationName">The name of the training iteration (the default is usually 'Iteration1', 'Iteration2', etc.)</param>
        /// <returns>Results of the evaluation.</returns>
        public async Task<PredictionResult> EvaluateImageUrlAsync(string imgUrl)
        {
            var apiEndpoint = $"/customvision/v3.0/Prediction/c6388be2-74e8-4e5c-8b09-ad546818b6ae/detect/iterations/{IterationName}/url";

            // Set Content-Type Header to : application/json
            // Set Body to : {"Url": "https://example.com/image.png"}
            using (var content = new StringContent(@"{""Url"": """ + imgUrl + @"""}"))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                using (var response = await client.PostAsync(apiEndpoint, content))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var results = JsonConvert.DeserializeObject<PredictionResult>(json);
                        return results;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Evaluates an image using a trained and published Custom Vision API. 
        /// </summary>
        /// <param name="imgBytes">The byte array of the image file</param>
        /// <param name="iterationName">The name of the training iteration. By default this is usually 'Iteration1', 'Iteraton2', etc.</param>
        /// <returns>Results of the evaluation.</returns>
        public async Task<PredictionResult> EvaluateImageFileAsync(byte[] imgBytes)
        {
            var apiEndpoint = $"/customvision/v3.0/Prediction/c6388be2-74e8-4e5c-8b09-ad546818b6ae/detect/iterations/{IterationName}/image";

            // Set Content-Type Header to : application/octet-stream
            // Set Body to : Image's byte[]
            using (var content = new ByteArrayContent(imgBytes))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                using (var response = await client.PostAsync(apiEndpoint, content))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var results = JsonConvert.DeserializeObject<PredictionResult>(json);
                        return results;
                    }
                }
            }

            return null;
        }

        public void Dispose()
        {
            client?.Dispose();
        }
    }
}
