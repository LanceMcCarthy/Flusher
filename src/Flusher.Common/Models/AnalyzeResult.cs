namespace Flusher.Common.Models
{
    public class AnalyzeResult
    {
        public bool IsPositiveResult { get; set; }
        public string Message { get; set; }
        public PhotoCaptureInfo PhotoResult { get; set; }
        public bool DidOperationComplete { get; set; }
    }
}
