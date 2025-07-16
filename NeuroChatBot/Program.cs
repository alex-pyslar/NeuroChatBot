using DotEnv.Core; // Добавлено для работы с .env
using NeuroChatBot.Core;
using NeuroChatBot.Services;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using System;
using System.IO;
using System.Threading.Tasks;
using Telegram.Bot;

namespace NeuroChatBot
{
    class Program
    {
        private static ILogger? _logger;
        private static IConfiguration? _configuration;
        private static ServiceProvider? _serviceProvider;

        static async Task Main(string[] args)
        {
            // Load environment variables from .env file
            new EnvLoader().Load();
            _configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables() // Load from environment variables (including .env)
                .Build();

            // Set up logging
            _logger = new ConsoleLogger();
            _logger.Logging = LogLevel.Info | LogLevel.Error; // Default logging level
#if DEBUG
            _logger.Logging |= LogLevel.DebugInfo;
#endif

            // Parse command-line arguments for logger
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "--logger":
                    case "-l":
                        _logger.Logging = LogLevel.All; // Enable all logs if -l is present
                        break;
                }
            }

            _logger.Info("Starting LlamaBot...");

            // Set up Dependency Injection
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            // Get necessary services
            var telegramBotService = _serviceProvider.GetRequiredService<TelegramBotService>();

            // Start Telegram Bot
            await telegramBotService.StartReceiving();

            _logger.Info("Bot is running. Use /start for menu.");
            await Task.Delay(-1); // Keep the application running indefinitely
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ILogger>(_logger!); // Use the pre-configured logger
            services.AddSingleton(_configuration!); // Add configuration

            // Configure Telegram Bot Client
            var botToken = _configuration!["TELEGRAM_BOT_TOKEN"];
            if (string.IsNullOrEmpty(botToken))
            {
                _logger?.Error("Telegram Bot Token is not configured. Check your .env file.");
                throw new InvalidOperationException("Telegram Bot Token is missing.");
            }
            services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));

            // Configure MongoDB Service
            var mongoConnectionString = _configuration!["MONGO_CONNECTION_STRING"];
            var mongoDatabaseName = _configuration!["MONGO_DATABASE_NAME"];
            if (string.IsNullOrEmpty(mongoConnectionString) || string.IsNullOrEmpty(mongoDatabaseName))
            {
                _logger?.Error("MongoDB Connection String or Database Name is not configured. Check your .env file.");
                throw new InvalidOperationException("MongoDB configuration is missing.");
            }
            services.AddSingleton<IMongoDbService>(new MongoDbService(mongoConnectionString, mongoDatabaseName, _logger!)); // Pass logger

            // Configure HttpClient for Llama-server communication
            services.AddHttpClient<IModelService, LlamaCppModelService>(client =>
            {
                var llamaServerAddress = _configuration!["LLAMA_SERVER_ADDRESS"];
                if (string.IsNullOrEmpty(llamaServerAddress))
                {
                    _logger?.Error("Llama Server Address is not configured. Check your .env file.");
                    throw new InvalidOperationException("Llama Server Address is missing.");
                }
                client.BaseAddress = new Uri(llamaServerAddress);
                client.Timeout = TimeSpan.FromMinutes(5); // Set a reasonable timeout
            });

            services.AddSingleton<IUserService, MongoDbUserService>();
            // ProcessManager is removed as per request
            services.AddSingleton<TelegramBotService>();
        }
    }
}