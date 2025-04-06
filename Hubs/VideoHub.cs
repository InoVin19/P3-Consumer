using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Consumer.Models;

namespace Consumer.Hubs
{
    public class VideoHub : Hub
    {
        private readonly ILogger<VideoHub> _logger;
        private static IHubContext<VideoHub> _hubContext;

        public VideoHub(ILogger<VideoHub> logger)
        {
            _logger = logger;
        }

        // Set the hub context for static access
        public static void SetHubContext(IHubContext<VideoHub> hubContext)
        {
            _hubContext = hubContext;
        }

        // Send queue update to all clients
        public static async Task SendQueueUpdate(int queueSize, int maxQueueSize)
        {
            if (_hubContext != null)
            {
                await _hubContext.Clients.All.SendAsync("ReceiveQueueUpdate", queueSize, maxQueueSize);
            }
        }

        // Send video processed notification
        public static async Task SendVideoProcessed(string fileName, int consumerId)
        {
            if (_hubContext != null)
            {
                await _hubContext.Clients.All.SendAsync("ReceiveVideoProcessed", fileName, consumerId);
            }
        }

        // Send video dropped notification
        public static async Task SendVideoDropped(string fileName)
        {
            if (_hubContext != null)
            {
                await _hubContext.Clients.All.SendAsync("ReceiveVideoDropped", fileName);
            }
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation($"Client connected: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
            await base.OnDisconnectedAsync(exception);
        }
    }
}
