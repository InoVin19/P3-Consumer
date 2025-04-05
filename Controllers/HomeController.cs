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

        public HomeController(
            ILogger<HomeController> logger,
            VideoStorageService videoStorageService,
            QueueManager queueManager)
        {
            _logger = logger;
            _videoStorageService = videoStorageService;
            _queueManager = queueManager;
        }

        public IActionResult Index()
        {
            var allVideos = _videoStorageService.GetAllVideos();
            ViewBag.QueueCount = _queueManager.GetQueueCount();
            ViewBag.QueueLimit = _queueManager.GetMaxQueueSize();
            
            // Group videos by thread ID (using a hash of the ID as a simple way to assign thread IDs)
            var groupedVideos = new Dictionary<int, List<VideoUpload>>();
            
            foreach (var video in allVideos)
            {
                // Use a simple hash of the GUID to determine a thread ID (1 or 2)
                int threadId = (video.Id.GetHashCode() % 2) + 1;
                
                if (!groupedVideos.ContainsKey(threadId))
                {
                    groupedVideos[threadId] = new List<VideoUpload>();
                }
                
                groupedVideos[threadId].Add(video);
            }
            
            // Make sure we have entries for all thread IDs (1 and 2) even if empty
            if (!groupedVideos.ContainsKey(1))
            {
                groupedVideos[1] = new List<VideoUpload>();
            }
            
            if (!groupedVideos.ContainsKey(2))
            {
                groupedVideos[2] = new List<VideoUpload>();
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
