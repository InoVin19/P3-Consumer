using Consumer.Models;
using Consumer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace Consumer.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly VideoStorageService _videoStorageService;
        private readonly QueueManager _queueManager;
        private readonly ConfigService _configService;

        public HomeController(
            ILogger<HomeController> logger,
            VideoStorageService videoStorageService,
            QueueManager queueManager,
            ConfigService configService)
        {
            _logger = logger;
            _videoStorageService = videoStorageService;
            _queueManager = queueManager;
            _configService = configService;
        }

        public IActionResult Index()
        {
            var allVideos = _videoStorageService.GetAllVideos();
            ViewBag.QueueCount = _queueManager.GetQueueCount();
            ViewBag.QueueLimit = _queueManager.GetMaxQueueSize();
            ViewBag.ConsumerCount = _configService.ConsumerCount;
            
            // Group videos by their ThreadId property
            var groupedVideos = new Dictionary<int, List<VideoUpload>>();
            
            foreach (var video in allVideos)
            {
                // Use the actual ThreadId from the video
                int threadId = video.ThreadId;
                
                // Ensure the threadId is valid (between 1 and ConsumerCount)
                if (threadId < 1 || threadId > _configService.ConsumerCount)
                {
                    threadId = 1; // Default to 1 if invalid
                    _logger.LogWarning($"Invalid ThreadId {video.ThreadId} detected for video {video.Id}. Using default of 1.");
                }
                
                if (!groupedVideos.ContainsKey(threadId))
                {
                    groupedVideos[threadId] = new List<VideoUpload>();
                }
                
                groupedVideos[threadId].Add(video);
            }
            
            // Make sure we have entries for all thread IDs (1 to ConsumerCount) even if empty
            for (int i = 1; i <= _configService.ConsumerCount; i++)
            {
                if (!groupedVideos.ContainsKey(i))
                {
                    groupedVideos[i] = new List<VideoUpload>();
                }
            }
            
            return View(groupedVideos);
        }

        [HttpGet("video/{id}")]
        public async Task<IActionResult> GetVideo(string id)
        {
            var videoData = await _videoStorageService.GetVideoDataAsync(id);
            
            if (videoData == null)
            {
                return NotFound();
            }
            
            return File(videoData, "video/mp4");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
