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
        public int BasePort { get; } = 9000; // Hardcoded to 9000
        public int ProducerCount { get; private set; } // Store the producer count from 'p' parameter
        public string VideoStoragePath { get; private set; }

        private readonly ILogger<ConfigService> _logger;
        private readonly IConfiguration _configuration;

        public ConfigService(ILogger<ConfigService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            Console.WriteLine("ConfigService constructor called - about to load config");
            LoadConfig();
        }

        private void LoadConfig()
        {
            try
            {
                // Get the current directory and check for input.txt
                string currentDirectory = Directory.GetCurrentDirectory();
                string inputFilePath = Path.Combine(currentDirectory, "input.txt");
                Console.WriteLine($"Looking for input.txt at: {inputFilePath}");
                
                // Try to read from input.txt first
                if (File.Exists(inputFilePath))
                {
                    Console.WriteLine($"Input.txt found at {inputFilePath}");
                    _logger.LogInformation($"Reading configuration from {inputFilePath}");
                    string[] lines = File.ReadAllLines(inputFilePath);
                    Console.WriteLine($"Read {lines.Length} lines from input.txt");
                    
                    foreach (var line in lines)
                    {
                        Console.WriteLine($"Processing line: {line}");
                        var parts = line.Split('=');
                        if (parts.Length == 2)
                        {
                            string key = parts[0].Trim().ToLower();
                            string value = parts[1].Trim();
                            Console.WriteLine($"Parsed key={key}, value={value}");
                            
                            switch (key)
                            {
                                case "c":
                                    ConsumerCount = int.Parse(value);
                                    Console.WriteLine($"*** Set ConsumerCount to {ConsumerCount} from input.txt ***");
                                    _logger.LogInformation($"Set ConsumerCount to {ConsumerCount} from input.txt");
                                    break;
                                case "q":
                                    QueueLimit = int.Parse(value);
                                    Console.WriteLine($"*** Set QueueLimit to {QueueLimit} from input.txt ***");
                                    _logger.LogInformation($"Set QueueLimit to {QueueLimit} from input.txt");
                                    break;
                                case "p":
                                case "port":
                                    // Still read 'p' for producer program compatibility
                                    ProducerCount = int.Parse(value);
                                    Console.WriteLine($"*** Read ProducerCount {ProducerCount} from input.txt (p parameter) ***");
                                    _logger.LogInformation($"Read ProducerCount {ProducerCount} from input.txt (p parameter)");
                                    break;
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Input.txt NOT found at {inputFilePath}");
                    _logger.LogInformation($"Input file not found at {inputFilePath}, reading from appsettings.json");
                }

                // If values are still default, set them to reasonable defaults
                if (ConsumerCount <= 0)
                {
                    ConsumerCount = 2;
                    Console.WriteLine($"Using default ConsumerCount: {ConsumerCount}");
                }
                
                if (QueueLimit <= 0)
                {
                    QueueLimit = 10;
                    Console.WriteLine($"Using default QueueLimit: {QueueLimit}");
                }
                
                if (ProducerCount <= 0)
                {
                    ProducerCount = 3;
                    Console.WriteLine($"Using default ProducerCount: {ProducerCount}");
                }

                // Set video storage path
                VideoStoragePath = _configuration.GetValue<string>("ConsumerSettings:VideoStoragePath", 
                    Path.Combine(Directory.GetCurrentDirectory(), "VideoStorage"));
                
                // Create video storage directory if it doesn't exist
                if (!Directory.Exists(VideoStoragePath))
                {
                    Directory.CreateDirectory(VideoStoragePath);
                    _logger.LogInformation($"Created video storage directory: {VideoStoragePath}");
                }

                string configMessage = $"Configuration loaded: {ConsumerCount} consumers, Queue limit: {QueueLimit}, Base port: {BasePort}, Producer count: {ProducerCount}";
                Console.WriteLine($"*** {configMessage} ***");
                _logger.LogInformation(configMessage);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error loading configuration: {ex.Message}";
                Console.WriteLine($"*** ERROR: {errorMessage} ***");
                _logger.LogError(ex, errorMessage);
                
                // Set defaults
                ConsumerCount = 2;
                QueueLimit = 10;
                ProducerCount = 3;
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
