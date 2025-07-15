using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Identity;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;


interface ILogger
{
    LogLevel Logging { get; set; }

    void Log(LogLevel level, ConsoleColor color, params string[] messages);
    void Info(string message);
    void Info(params string[] messages);
    void DebugInfo(string message);
    void DebugInfo(params string[] messages);
    void Error(string message);
    void Error(params string[] messages);
}
[Flags]
enum LogLevel
{
    None = 0x00000000,
    Info = 0x00000001,
    Error = 0x00000010,
    DebugInfo = 0x00000100,
    All = 0x11111111, 
}
class ConsoleLogger : ILogger
{
    public LogLevel Logging { get; set; }
    public void Log(LogLevel level, ConsoleColor color, params string[] messages)
    {
        if ((Logging & level) != level)
            return;
        Console.ForegroundColor = ConsoleColor.Blue;
        if (messages.Length == 1)
            Console.Write($"[{DateTime.Now}][{level}]");
        else
            Console.WriteLine($"[{DateTime.Now}][{level}]");
        Console.ForegroundColor = color;
        foreach (var message in messages)
            Console.WriteLine($"{message}");
        Console.ResetColor();
    }
    public void Info(string message) => Info(messages: message);
    public void Info(params string[] messages) => Log(LogLevel.Info, ConsoleColor.Green, messages);
    public void DebugInfo(string message) => DebugInfo(messages: message);
    public void DebugInfo(params string[] messages) => Log(LogLevel.DebugInfo, ConsoleColor.DarkYellow, messages);
    public void Error(string message) => Error(messages: message);
    public void Error(params string[] messages) => Log(LogLevel.Error, ConsoleColor.Red, messages);
}
/**/
class User
{
    [JsonIgnore] public string? PendingCommand { get; set; } = null;
    [JsonIgnore] public DateTime RequestTime { get; set; } = DateTime.MinValue;
    [JsonIgnore] public CharacterPreset CurrentCharacter => Characters[currentCharacter];
    [JsonIgnore] public int LastMessageId { get; set; } = 0;


    [JsonInclude] private int currentCharacter = 0;
    [JsonInclude] public long Id { get; private set; }
    public string UserName { get; set; } = "Вы";
    public string? UserDescription { get; set; } = null;
    public List<CharacterPreset> Characters { get; set; } = new List<CharacterPreset>(1) { new CharacterPreset() };
    public User() { }
    public User(long id) { LoadData(id); Id = id; }
    public void SaveData() => SaveData(Id);
    public void ChangeCurrentCharacter(int ind) => currentCharacter = ind;
    public void SaveData(long userId)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                IncludeFields = true
            };
            string jsonString = JsonSerializer.Serialize(this, options);
            File.WriteAllText($"C:\\AI\\SaveUserData\\{userId}", jsonString);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при сохранении данных: {ex.Message}");
        }
    }
    public static User LoadData(long userId)
    {
        string jsonString = File.ReadAllText($"C:\\AI\\SaveUserData\\{userId}");
        return JsonSerializer.Deserialize<User>(jsonString) ?? new User() { Id = userId };
    }
}
class CharacterPreset
{
    public string Name { get; set; } = "AI";
    public string Prompt { get; set; } = "You are a useful AI assistant";
    public string Greeting { get; set; } = "How I can help?";
    [JsonInclude] public List<ChatMessage> Chat { get; set; } = new List<ChatMessage>(20);
}
public class ChatMessage
{
    [JsonInclude]public string Role { get; set; } = null;
    [JsonInclude]public string Content { get; set; } = null;
    public ChatMessage(string role, string content)
    {
        Role = role;
        Content = content;
    }
}

/*
[BsonIgnoreExtraElements]
public class User
{
    [BsonId] // Указывает, что это поле будет _id в MongoDB
    [BsonRepresentation(BsonType.String)] // Сохраняем как строку
    public string Id { get; set; } // Заменяем long на string для соответствия "user123"

    [BsonElement("name")]
    public string Name { get; set; } = "Вы";

    [BsonElement("email")]
    public string Email { get; set; } = "";

    [BsonElement("prompts")]
    public List<Prompt> Prompts { get; set; } = new List<Prompt> { new Prompt() };

    // Дополнительные поля из вашего класса
    [BsonIgnore] // Не сохраняем в БД
    public Prompt CurrentPrompt => Prompts.Count > 0 ? Prompts[0] : new Prompt();
}

public class Prompt
{
    [BsonElement("id")]
    public string Id { get; set; } = $"prompt{Guid.NewGuid().ToString("N").Substring(0, 8)}";

    [BsonElement("title")]
    public string Title { get; set; } = "Чат";

    [BsonElement("messages")]
    public List<Message> Messages { get; set; } = new List<Message>();
}

public class Message
{
    [BsonElement("text")]
    public string Text { get; set; } = "";

    [BsonElement("role")]
    public string Role { get; set; } = "user"; // "user" или "bot"
}
public class MongoDbService
{
    private readonly IMongoCollection<User> _usersCollection;
    private readonly IMongoDatabase _database;

    public MongoDbService(string connectionString)
    {
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase("myapp");
        _usersCollection = database.GetCollection<User>("users");
    }

    // Сохранение или обновление пользователя (без изменений)
    public async Task SaveUserAsync(User user)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, user.Id);
        var options = new ReplaceOptions { IsUpsert = true };
        await _usersCollection.ReplaceOneAsync(filter, user, options);
    }

    // Загрузка пользователя (без изменений)
    public async Task<User> LoadUserAsync(string userId)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        return await _usersCollection.Find(filter).FirstOrDefaultAsync();
    }

    // Исправленный метод добавления сообщения
    public async Task AddMessageAsync(string userId, string promptId, Message message)
    {
        try
        {
            // Фильтр для нахождения пользователя и конкретного промпта
            var filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(u => u.Id, userId),
                Builders<User>.Filter.ElemMatch(u => u.Prompts, p => p.Id == promptId)
            );

            // Обновление с использованием позиционного оператора $
            var update = Builders<User>.Update
                .Push("prompts.$.messages", message); // $ указывает на первый элемент, соответствующий фильтру

            var result = await _usersCollection.UpdateOneAsync(filter, update);

            if (result.MatchedCount == 0)
            {
                throw new Exception("Пользователь или промпт не найдены");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при добавлении сообщения: {ex.Message}");
            throw;
        }
    }

    // Метод для получения и вывода всех данных
    public async Task PrintAllDataAsync()
    {
        try
        {
            // Получаем все документы из коллекции users
            var users = await _usersCollection.Find(new BsonDocument()).ToListAsync();

            if (users.Count == 0)
            {
                Console.WriteLine("В базе данных нет пользователей");
                return;
            }

            Console.WriteLine($"Найдено пользователей: {users.Count}");
            Console.WriteLine(new string('-', 50));

            foreach (var user in users)
            {
                Console.WriteLine($"Пользователь ID: {user.Id}");
                Console.WriteLine($"Имя: {user.Name}");
                Console.WriteLine($"Email: {user.Email}");
                Console.WriteLine("Промпты:");

                if (user.Prompts != null && user.Prompts.Count > 0)
                {
                    foreach (var prompt in user.Prompts)
                    {
                        Console.WriteLine($"\tPrompt ID: {prompt.Id}");
                        Console.WriteLine($"\tНазвание: {prompt.Title}");
                        Console.WriteLine("\tСообщения:");

                        if (prompt.Messages != null && prompt.Messages.Count > 0)
                        {
                            foreach (var message in prompt.Messages)
                            {
                                Console.WriteLine($"\t\t{message.Role}: {message.Text}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("\t\tНет сообщений");
                        }
                        Console.WriteLine();
                    }
                }
                else
                {
                    Console.WriteLine("\tНет промптов");
                }
                Console.WriteLine(new string('-', 50));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при получении данных: {ex.Message}");
            throw;
        }
    }
}
/**/
class Program
{
    static readonly HttpClient httpClient = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
    static readonly TelegramBotClient botClient = new TelegramBotClient("");
    static readonly Dictionary<long, User> users = new Dictionary<long, User>();
    static ILogger? logger;

    static bool _cpuMode = false;

    static string pathToLoader = _cpuMode ? 
        "C:\\AI\\llama.cpp\\build\\bin\\Release\\llama-server.exe" : 
        "C:\\AI\\llamaCUDA\\build\\bin\\Release\\llama-server.exe";
    static string pathToModel = _cpuMode ?
        "C:\\AI\\text-generation-webui\\models\\Mistral-Small-24B-Instruct-2501-abliterated.Q8_0.gguf" :
        "C:\\AI\\text-generation-webui\\models\\Mistral-Small-24B-Instruct-2501-abliterated.Q4_0.gguf";
    //static string pathToModel = "C:\\AI\\text-generation-webui\\models\\Mistral-Small-3.1-24B-Instruct-2503-Q4_K_L.gguf";
    //static string pathToModel = "C:\\AI\\text-generation-webui\\models\\Qwen_Qwen3-14B-Q5_K_L.gguf";
    //static string pathToModel = "C:\\AI\\text-generation-webui\\models\\qwen2.5-coder-7b-instruct-q8_0.gguf";
    static async Task Main(string[] args)
    {
#if DEBUG
        var tmp = args.ToList();
        tmp.Add(args.Length > 0 ? " -l" : "-l");
        args = tmp.ToArray();
#endif
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--logger":
                case "-l":
                    logger = new ConsoleLogger();
                    logger.Logging = LogLevel.All;
#if DEBUG
                    logger.Logging = logger.Logging | LogLevel.DebugInfo;
#endif
                    break;
                case "--model":
                case "-m":
                    pathToModel = args[i + 1];
                    i++;
                    break;
                default: Console.WriteLine($"Unknown command: {args[i]}"); break;
            }
            Process proc = new Process();
            if (!Process.GetProcesses().Any(p => p.ProcessName == "llama-server"))
            {
                proc.StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c chcp 65001 && \"{pathToLoader}\" " +
                    (
                    _cpuMode ?
                    $"-m {pathToModel} -t 16 -c {8192 * 4} --no-mmap --mlock" :
                    $"-m {pathToModel} --n-gpu-layers 999 -c {8192} --jinja --chat-template-file C:\\AI\\Mistral-Small-24B-Instruct-2501-abliterated-Q4_0.j2"// --host 10.66.66.4 --port 1010 --verbose"
                    ),
                    //UseShellExecute = true
                };
                //proc.StartInfo.UseShellExecute = true;
                //proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                proc.Start();
                logger?.Info($"Процесс \"{proc.ProcessName}\" запущен");
                await SendMessage(578338071, $"Процесс \"{proc.ProcessName}\" запущен", null);
            }
            else
            {
                proc = Process.GetProcesses().Single(p => p.ProcessName == "llama-server");
                logger?.Info($"Процесс \"{proc.ProcessName}\" был запущен ранее");
                await SendMessage(578338071, $"Процесс \"{proc.ProcessName}\" был запущен ранее", null);
            }
        }
        /*
        string connectionString = "mongodb://admin:s5ZYQw9Z-N+1,9@37.252.19.157:27017/myapp?authSource=admin";
        var dbService = new MongoDbService(connectionString);

        // Создаем нового пользователя
        var user = new User
        {
            Id = "user123",
            Name = "Alice",
            Email = "alice@example.com",
            Prompts = new List<Prompt>
        {
            new Prompt
            {
                Id = "prompt1",
                Title = "Техподдержка",
                Messages = new List<Message>
                {
                    new Message
                    {
                        Text = "Как сбросить пароль?",
                        Role = "user"
                    },
                    new Message
                    {
                        Text = "Используйте кнопку 'Забыли пароль?'",
                        Role = "bot"
                    }
                }
            }
        }
        };
        Console.WriteLine("Пользователь создан!");
        // Сохраняем пользователя
        await dbService.SaveUserAsync(user);
        Console.WriteLine("Пользователь сохранен!");

        // Пример добавления нового сообщения
        var newMessage = new Message
        {
            Text = "Спасибо за помощь!",
            Role = "user"
        };
        await dbService.AddMessageAsync("user123", "prompt1", newMessage);
        Console.WriteLine("Сообщение добавлено!");

        // Загрузка пользователя
        var loadedUser = await dbService.LoadUserAsync("user123");
        if (loadedUser != null)
        {
            Console.WriteLine($"Загружен пользователь: {loadedUser.Name}, Email: {loadedUser.Email}");
            foreach (var prompt in loadedUser.Prompts)
            {
                Console.WriteLine($"Промпт: {prompt.Title}");
                foreach (var msg in prompt.Messages)
                {
                    Console.WriteLine($"- {msg.Role}: {msg.Text}");
                }
            }
        }
        Console.ReadLine();
        // Выводим все данные
        await dbService.PrintAllDataAsync();

        Console.ReadLine();
        /**/
        /**/
        botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync);
        Console.WriteLine("Бот запущен. Используй /start для меню.");
        await Task.Delay(-1);
        /**/
    }
    static async Task DeleteCommandMessage(long chatId, int messageId, CancellationToken cancellationToken)
    {
        try
        {
            await botClient.DeleteMessage(chatId, messageId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger?.Error($"Ошибка при удалении сообщения: {ex.Message}");
        }
    }
    static async Task<int> SendMessage(long chatId, string response, CancellationToken cancellationToken) =>
        await SendMessage(chatId, response, null, cancellationToken);
    static async Task<int> SendMessage(long chatId, string response, ReplyMarkup? markup = null, CancellationToken cancellationToken = default)
    {
        try
        {

            var sentMessage = await botClient.SendMessage(
            new ChatId(chatId),
            response,
            replyMarkup: markup,
            cancellationToken: cancellationToken
            );
            return sentMessage.Id;
        }
        catch (Exception e)
        {
            Console.WriteLine($"{e.Data}\n{e.Message}");
        }
        return -1;
        //botClient.EditMessage
    }
    static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type == UpdateType.CallbackQuery)
        {
            await HandleCallbackQueryAsync(botClient, update.CallbackQuery, cancellationToken);
            return;
        }

        if (update.Type != UpdateType.Message || update.Message?.Text == null)
            return;


        var message = update.Message;
        long userId = message.From.Id;
        long chatId = message.Chat.Id;
        string userMessage = message.Text.Trim();

        if (!File.Exists($"C:\\AI\\SaveUserData\\{userId}"))
            return;

        if (!users.ContainsKey(userId))
            LoadCharacter(userId);

        var user = users[userId];
        logger?.Info($"Получено сообщение от {message.From.Username}({userId})", $"Чат {chatId}");
        user.RequestTime = DateTime.Now;

        string response = "Что-то пошло не так!";
        int lastSentMessageId = 0;

        // Обработка ожидаемых данных для команд с вводом
        if (user.PendingCommand != null)
        {
            bool isCommand = true;
            switch (user.PendingCommand)
            {
                case "setprompt":
                    user.CurrentCharacter.Prompt = userMessage;
                    response = "prompt установлен";
                    break;
                case "setun":
                    user.UserName = userMessage;
                    response = "Имя установлено";
                    break;
                case "setud":
                    user.UserDescription = userMessage;
                    response = "Описание установлено";
                    break;
                default: isCommand = false; break;
            }
            if (isCommand)
                await DeleteCommandMessage(chatId, message.Id, cancellationToken);
            user.PendingCommand = null;
            lastSentMessageId = await SendMessage(chatId, response, cancellationToken: cancellationToken);
        }
        else if (userMessage.ToLower() == "/start")
        {
            var inlineKeyboard = new InlineKeyboardMarkup(
            [
                [InlineKeyboardButton.WithCallbackData("Main Commands", "main")],
                [InlineKeyboardButton.WithCallbackData("Characters", "characters")],
                [InlineKeyboardButton.WithCallbackData("Some", "some")]
            ]);
            await DeleteCommandMessage(chatId, message.Id, cancellationToken);

            lastSentMessageId = await SendMessage(chatId, "Выбери категорию:", inlineKeyboard, cancellationToken);
        }
        else
        {
            response = await GetModelResponseAsync2(userId, userMessage);
            lastSentMessageId = await SendMessage(chatId, response, cancellationToken);
            logger?.Info($"Отправлено сообщение {message.From.Username}({userId}). Время обработки запроса: {(int)(DateTime.Now - user.RequestTime).TotalSeconds} секунд");
        }
        if (lastSentMessageId != 0)
            user.LastMessageId = lastSentMessageId;
    }
    static async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var chatId = callbackQuery.Message.Chat.Id;
        long userId = callbackQuery.From.Id;

        if (!users.ContainsKey(userId))
            LoadCharacter(userId);

        var user = users[userId];

        // Удаляем предыдущее сообщение с меню
        if (user.LastMessageId != 0)
            await DeleteCommandMessage(chatId, user.LastMessageId, cancellationToken);

        string response = "";
        InlineKeyboardMarkup keyboard = null;
        int lastSentMessageId = 0;

        switch (callbackQuery.Data)
        {
            case "main":
                keyboard = new InlineKeyboardMarkup(
                [
                    [InlineKeyboardButton.WithCallbackData("Set Prompt", "setprompt")],
                    [InlineKeyboardButton.WithCallbackData("Set User Name", "setun")],
                    [InlineKeyboardButton.WithCallbackData("Set User Description", "setud")],
                    [InlineKeyboardButton.WithCallbackData("Clear History", "clear")],
                    [InlineKeyboardButton.WithCallbackData("Save Data", "save")]
                ]);
                response = "Main Commands:";
                break;

            case "characters":
                keyboard = new InlineKeyboardMarkup();
                for (int i = 0; i < user.Characters.Count; i++)
                    keyboard.AddNewRow(new InlineKeyboardButton($"{user.Characters[i].Name} ID[{i}]", $"{i}"));
                keyboard.AddNewRow(new InlineKeyboardButton("Create new Chatecter", "newchar"));
                response = "Characters:";
                break;

            case "some":
                response = "Тут будет некая логика для Some";
                break;

            case "setprompt":
                user.PendingCommand = "setprompt";
                response = "Введите новый prompt для AI:";
                break;

            case "setun":
                user.PendingCommand = "setun";
                response = "Введите новое имя пользователя:";
                break;

            case "setud":
                user.PendingCommand = "setud";
                response = "Введите новое описание пользователя:";
                break;

            case "clear":
                user.CurrentCharacter.Chat = new List<ChatMessage>(20);
                response = $"История очищена!\n\n{user.CurrentCharacter.Greeting.Replace("{{user}}", user.UserName).Replace("{{char}}", user.CurrentCharacter.Name)}";
                break;

            case "save":
                user.SaveData(userId);
                response = "История сохранена";
                break;
            case "newchar":
                user.Characters.Add(new CharacterPreset());
                user.ChangeCurrentCharacter(user.Characters.Count - 1);
                response = $"Создан новый персонаж {user.CurrentCharacter.Name} ID: [{user.Characters.Count-1}]";
                break;
            default:
                int num = 0;
                if(Int32.TryParse(callbackQuery.Data, out num))
                {
                    user.ChangeCurrentCharacter(num);
                    response = $"Персонаж {user.CurrentCharacter.Name} ID[{num}] выбран.\nПоследнее (стандартное) сообщение:\n{(user.CurrentCharacter.Chat.Count == 0 ? user.CurrentCharacter.Greeting
                        .Replace("{{user}}", user.UserName).Replace("{{char}}", user.CurrentCharacter.Name) : user.CurrentCharacter.Chat.Last().Content)}";

                }
                break;
        }
        lastSentMessageId = await SendMessage(chatId, response, keyboard, cancellationToken);
        user.LastMessageId = lastSentMessageId;
        await botClient.AnswerCallbackQuery(callbackQuery.Id);
    }
    static void LoadCharacter(long userId)
    {
        if (File.Exists($"C:\\AI\\SaveUserData\\{userId}"))
            users.Add(userId, User.LoadData(userId));
        else
            users.Add(userId, new User());
    }   
    static async Task<string> GetModelResponseAsync(long userId, string message)
    {
        try
        {
            var user = users[userId];

            if (user.CurrentCharacter.Chat.Count > 20)
            {
                user.CurrentCharacter.Chat.RemoveAt(0);
                user.CurrentCharacter.Chat.RemoveAt(0);
            }

            string charecterPrompt = user.CurrentCharacter.Prompt
                .Replace("{{char}}", user.CurrentCharacter.Name)
                .Replace("{{user}}", user.UserName);
            string characterGreeting = user.CurrentCharacter.Greeting
                .Replace("{{char}}", user.CurrentCharacter.Name)
                .Replace("{{user}}", user.UserName);

            var messages = new List<object>
            {
                //new { role = "system", content = charecterPrompt },
                new { role = "system", content = $"[USER_CHARACTER_DESCRIPTION][{user.UserName}]=[[{user.UserDescription}] user = {user.UserName}][/USER_CHARACTER_DESCRIPTION]" },
                new { role = "system", content = $"[MODEL_PROMPT]\nContinue the chat dialogue below. Write a single reply for the character \"{user.CurrentCharacter.Name}\"\n\n[{user.CurrentCharacter.Name}]=[{charecterPrompt}][/MODEL_PROMPT]" },
                //new { role = "user", content = $"[{user.UserName}]={{{user.UserDescription}}}" },
                new { role = "assistant", content = characterGreeting }
            };
            if (user.CurrentCharacter.Chat.Count == 0)
            {
                logger?.DebugInfo(
                    $"system: {charecterPrompt}",
                    $"user: Персонаж: {user.UserDescription}. Имя: \"{user.UserName}\"",
                    $"assistant: {characterGreeting}");
            }
            user.CurrentCharacter.Chat.Add(new ChatMessage("user", message));
            logger?.DebugInfo($"user: {message}");
            foreach (var msg in user.CurrentCharacter.Chat)
            {
                messages.Add(new { role = msg.Role, content = msg.Content });
            }
            var request = new
            {
                //model = Path.GetFileName(pathToModel),
                messages,
                //add_generation_prompt = "True",
                //enable_thinking = "False",
                //add_generation_prompt = true,
                //enable_thinking = false,
                //user_character = $"{user.UserName}: {user.UserDescription}",
                //model_prompt = $"{charecterPrompt}",
                //add_generation_prompt = "True",
                //enable_thinking = "False",
                //max_tokens = 1024,
                //temperature = 1,
                //min_p = 0.05,
                //top_p = 1.0,
                //top_k = 0.0,
                repeat_penalty = 1.3,
                //presence_penalty = 0.0,
                //frequency_penalty = 0.0,
                //stop = new[] { "</s>", "\n\n" },
                //stop = new string[] { "\n\n\n" },
                //mirostat = 0,
            };

            var response = await httpClient.PostAsJsonAsync("http://10.66.66.5:8000/v1/chat/completions", request);
            //var response = await httpClient.PostAsJsonAsync("http://10.66.66.5:8000/v1/chat/completions", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>();
            string modelReply = result.choices[0].message.content ?? "Ошибка: нет ответа";

            user.CurrentCharacter.Chat.Add(new ChatMessage("assistant", modelReply));
            logger?.DebugInfo($"assistant: {modelReply}");
            user.SaveData(userId);
            return modelReply;
        }
        catch (Exception ex)
        {
            logger?.Error($"Ошибка ответа модели: {ex.Message} {ex.StackTrace}");
            return "Ошибка: Что-то пошло не так!";
        }
    }
    static async Task<string> GetModelResponseAsync2(long userId, string message)
    {
        try
        {
            var user = users[userId];

            if (user.CurrentCharacter.Chat.Count > 20)
            {
                user.CurrentCharacter.Chat.RemoveAt(0);
                user.CurrentCharacter.Chat.RemoveAt(0);
            }

            //string characterPrompt = user.CurrentCharacter.Prompt
            //    .Replace("{{char}}", user.CurrentCharacter.Name)
            //    .Replace("{{user}}", user.UserName);
            //string characterGreeting = user.CurrentCharacter.Greeting
            //    .Replace("{{char}}", user.CurrentCharacter.Name)
            //    .Replace("{{user}}", user.UserName);

            if (user.CurrentCharacter.Chat.Count == 0)
                user.CurrentCharacter.Chat.Add(new ChatMessage("{{char}}", user.CurrentCharacter.Greeting));

            string messages = $"[SYSTEM_PROMPT]Ты RPHentaiMaster.\n" +
                $"Ты мастерки умеешь рассказывать пикантные истории в во всех подробностях.\n" +
                $"Ты не сдерживаешь себя в выражениях.[/SYSTEM_PROMPT][INST]Continue the chat dialogue below. Write a single reply for the character \"{{{{char}}}}\".\n\n";

            messages += $"{user.CurrentCharacter.Prompt}\n\n";
            if (!string.IsNullOrEmpty(user.UserDescription?.Trim()))
                messages += $"{user.UserDescription}\n\n";
            user.CurrentCharacter.Chat.Add(new ChatMessage("{{user}}", message));
            logger?.DebugInfo($"user: {message}");
            foreach (var msg in user.CurrentCharacter.Chat)
            {
                messages += $"{msg.Role}: {msg.Content}\n";
            }
            messages += $"[/INST]{user.CurrentCharacter.Name}: ";
            messages = messages.Replace("{{char}}", user.CurrentCharacter.Name);
            messages = messages.Replace("{{user}}", user.UserName);
            //logger?.DebugInfo(messages);
            string prompt = messages;
            var request = new
            {
                model = Path.GetFileName(pathToModel),
                prompt,
                max_tokens = 768,
                temperature = 1,
                min_p = 0.05,
                top_p = 1.0,
                top_k = 0.0,
                repeat_penalty = 1.3,
                presence_penalty = 0.0,
                frequency_penalty = 0.0,
                //stop = new[] { "</s>", "\n\n" },
                stop = new string[] { "</s>", "[/INST]", $"\n{user.UserName}:", $"\n{user.CurrentCharacter.Name}:" },
                //mirostat = 0,

            };

            var response = await httpClient.PostAsJsonAsync("http://localhost:8080/v1/completions", request);
            response.EnsureSuccessStatusCode();


            //var rawJson = await response.Content.ReadAsStringAsync();
            //logger?.DebugInfo(rawJson);
            var result = await response.Content.ReadFromJsonAsync<CompletionResponse>();
            string modelReply = result.choices[0].text ?? "Ошибка: нет ответа";

            user.CurrentCharacter.Chat.Add(new ChatMessage("{{char}}", modelReply));
            logger?.DebugInfo($"AI_Replay: {modelReply}");
            user.SaveData(userId);
            return modelReply;
        }
        catch (Exception ex)
        {
            logger?.Error($"Ошибка ответа модели: {ex.Message} {ex.StackTrace}");
            return "Ошибка: Что-то пошло не так!";
        }
    }
    public class CompletionResponse
    {
        public List<Choice> choices { get; set; }
        public class Choice
        {
            public string text { get; set; }
        }
    }
    public class ChatCompletionResponse
    {
        public List<Choice> choices { get; set; }
        public class Choice
        {
            public MessageResponse message { get; set; }
            public class MessageResponse
            {
                public string content { get; set; }
            }
        }
    }
    static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        logger?.Error($"Ошибка: '{exception.Message}' '{exception.StackTrace}'");
        return Task.CompletedTask;
    }
}