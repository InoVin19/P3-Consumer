using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Consumer.Services
{
    public class ConfigService
    {
        public int ConsumerCount { get; private set; }
        public int QueueLimit { get; private set; }
        public int BasePort { get; private set; }
        public string VideoStoragePath { get; private set; }

        private readonly ILogger<ConfigService> _logger;
        private readonly IConfiguration _configuration;

        public ConfigService(ILogger<ConfigService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            LoadConfig();
        }

        private void LoadConfig()
        {
            try
            {
                // Get the current directory and check for input.txt
                string currentDirectory = Directory.GetCurrentDirectory();
                string inputFilePath = Path.Combine(currentDirectory, "input.txt");
                
                // Try to read from input.txt first
                if (File.Exists(inputFilePath))
                {
                    _logger.LogInformation($"Reading configuration from {inputFilePath}");
                    string[] lines = File.ReadAllLines(inputFilePath);
                    
                    foreach (var line in lines)
                    {
                        var parts = line.Split('=');
                        if (parts.Length == 2)
                        {
                            string key = parts[0].Trim().ToLower();
                            string value = parts[1].Trim();
                            
                            switch (key)
                            {
                                case "c":
                                    ConsumerCount = int.Parse(value);
                                    _logger.LogInformation($"Set ConsumerCount to {ConsumerCount} from input.txt");
                                    break;
                                case "q":
                                    QueueLimit = int.Parse(value);
                                    _logger.LogInformation($"Set QueueLimit to {QueueLimit} from input.txt");
                                    break;
                                case "p":
                                case "port":
                                    BasePort = int.Parse(value);
                                    _logger.LogInformation($"Set BasePort to {BasePort} from input.txt");
                                    break;
                            }
                        }
                    }
                }
                else
                {
                    // Fall back to appsettings.json
                    _logger.LogInformation($"Input file not found at {inputFilePath}, reading from appsettings.json");
                }

                // If values are still default, read from appsettings.json
                if (ConsumerCount <= 0)
                    ConsumerCount = _configuration.GetValue<int>("ConsumerSettings:ConsumerCount", 2);
                
                if (QueueLimit <= 0)
                    QueueLimit = _configuration.GetValue<int>("ConsumerSettings:QueueLimit", 10);
                
                if (BasePort <= 0)
                    BasePort = _configuration.GetValue<int>("ConsumerSettings:BasePort", 9000);

                // Set video storage path
                VideoStoragePath = _configuration.GetValue<string>("ConsumerSettings:VideoStoragePath", 
                    Path.Combine(Directory.GetCurrentDirectory(), "VideoStorage"));
                
                // Create video storage directory if it doesn't exist
                if (!Directory.Exists(VideoStoragePath))
                {
                    Directory.CreateDirectory(VideoStoragePath);
                    _logger.LogInformation($"Created video storage directory: {VideoStoragePath}");
                }

                _logger.LogInformation($"Configuration loaded: {ConsumerCount} consumers, Queue limit: {QueueLimit}, Base port: {BasePort}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading configuration, using defaults");
                
                // Set defaults
                ConsumerCount = 2;
                QueueLimit = 10;
                BasePort = 9000;
                VideoStoragePath = Path.Combine(Directory.GetCurrentDirectory(), "VideoStorage");
                
                // Create video storage directory if it doesn't exist
                if (!Directory.Exists(VideoStoragePath))
                {
                    Directory.CreateDirectory(VideoStoragePath);
                }
            }
        }
    }
}
