using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Flusher.Common.Models
{
    public class PredictionResult
    {
        [JsonProperty("Id")]
        public Guid Id { get; set; }

        [JsonProperty("Project")]
        public Guid Project { get; set; }

        [JsonProperty("Iteration")]
        public Guid Iteration { get; set; }

        [JsonProperty("Created")]
        public DateTimeOffset Created { get; set; }

        [JsonProperty("Predictions")]
        public List<Prediction> Predictions { get; set; }
    }

    public class Prediction
    {
        [JsonProperty("TagId")]
        public Guid TagId { get; set; }

        [JsonProperty("TagName")]
        public string TagName { get; set; }

        [JsonProperty("Probability")]
        public double Probability { get; set; }
    }
}
