using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebVisionAI.Models;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.Text.Json;
using System.Drawing;

namespace WebVisionAI.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration configuration;
        private ComputerVisionClient computerVisionClient;
        public HomeController(ILogger<HomeController> logger, IConfiguration _configuration)
        {
            _logger = logger;
            configuration = _configuration;
            computerVisionClient = new ComputerVisionClient(
                new ApiKeyServiceClientCredentials(configuration["CogSrvKey"]))
            {
                Endpoint = configuration["CogSrvEndPoint"]
            };
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        public async Task<IActionResult> AnalyzeImagePost(string imageUrl)
        {
            //check to make sure the image url is valid.
            if (!Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute))
                return BadRequest("Invalid image URL.");

            var thumbnailPath = await GenerateThumbnailAsync(imageUrl, 150);

            List<VisualFeatureTypes?> features = new List<VisualFeatureTypes?>
            {
                VisualFeatureTypes.Description,
                VisualFeatureTypes.Tags,
                VisualFeatureTypes.Categories,
                VisualFeatureTypes.Brands,
                VisualFeatureTypes.Objects,
                VisualFeatureTypes.Adult
            };

            ImageAnalysis analysis = await computerVisionClient.AnalyzeImageAsync(imageUrl, features);

            var analysisViewModel = new ImageAnalysisViewModel
            {
                Description = analysis.Description.Captions.FirstOrDefault()?.Text,
                Tags = analysis.Tags.Select(t => t.Name).ToList(),
                Categories = analysis.Categories.Select(c => c.Name).ToList(),
                Brands = analysis.Brands.Select(b => b.Name).ToList(),
                Objects = analysis.Objects.ToList(),
                AdultContent = analysis.Adult,
                ImageUrl = imageUrl,
                ThumbnailPath = thumbnailPath.Replace("wwwroot", "")
            };

            TempData["AnalysisData"] = JsonSerializer.Serialize(analysisViewModel);

            return RedirectToAction("AnalyzeImage");
        }

        /// <summary>
        /// This action presents the results from the POST request for image analysis.
        /// </summary>
        [HttpGet]
        public IActionResult AnalyzeImage()
        {
            var serializedData = TempData["AnalysisData"] as string;
            var analysisViewModel = JsonSerializer.Deserialize<ImageAnalysisViewModel>(serializedData);

            return View(analysisViewModel);
        }

        public async Task<string> GenerateThumbnailAsync(string imageUrl, int maxDimension)
        {
            var thumbnailPath = Path.Combine("wwwroot/images/thumbnails", Guid.NewGuid().ToString() + ".png");

            using (HttpClient httpClient = new HttpClient())
            {
                using (Stream imageStream = await httpClient.GetStreamAsync(imageUrl))
                {
                    using (Image image = Image.FromStream(imageStream))
                    {
                        int originalWidth = image.Width;
                        int originalHeight = image.Height;

                        double ratio = 0;
                        int newWidth = 0;
                        int newHeight = 0;

                        // Check if width is greater than height
                        if (originalWidth > originalHeight)
                        {
                            ratio = (double)maxDimension / originalWidth;
                            newWidth = maxDimension;
                            newHeight = (int)(originalHeight * ratio);
                        }
                        else
                        {
                            ratio = (double)maxDimension / originalHeight;
                            newHeight = maxDimension;
                            newWidth = (int)(originalWidth * ratio);
                        }

                        var thumbnail = image.GetThumbnailImage(newWidth, newHeight, () => false, IntPtr.Zero);

                        var directory = Path.GetDirectoryName(thumbnailPath);
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        thumbnail.Save(thumbnailPath, System.Drawing.Imaging.ImageFormat.Png);
                    }
                }
            }
            return thumbnailPath;
        }
    }
}