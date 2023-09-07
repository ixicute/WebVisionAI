using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.ComponentModel;

namespace WebVisionAI.Models
{
    public class ImageAnalysisViewModel
    {
        public string Description { get; set; }
        public List<string> Tags { get; set; }
        public List<string> Categories { get; set; }
        public List<string> Brands { get; set; }
        public List<DetectedObject> Objects { get; set; }

        [DisplayName("Adult Content")]
        public AdultInfo AdultContent { get; set; }

        public string ImageUrl { get; set; }
        public string ThumbnailPath { get; set; }
    }
}
