using DotEnv.Core;
using Microsoft.Extensions.Configuration;
using NeuroChatBot.Core;
using NeuroChatBot.Models;
using NeuroChatBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static Telegram.Bot.TelegramBotClient;

namespace NeuroChatBot.Services
{
    public class TelegramBotService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger _logger;
        private readonly IUserService _userService;
        private readonly IModelService _modelService;

        public TelegramBotService(ITelegramBotClient botClient, ILogger logger, IUserService userService, IModelService modelService)
        {
            _botClient = botClient;
            _logger = logger;
            _userService = userService;
            _modelService = modelService;
        }

        public async Task StartReceiving()
        {
            if (_botClient == null)
            {
                _logger.Error("Bot client is not initialized.");
                throw new InvalidOperationException("Bot client is not initialized.");
            }

            ReceiverOptions receiverOptions = new()
            {
                AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
            };

            try
            {
                _botClient.StartReceiving(
                    updateHandler: new DefaultUpdateHandler(
                        HandleUpdateAsync,
                        HandlePollingErrorAsync
                    ),
                    receiverOptions: receiverOptions
                );
                _logger.Info("Telegram Bot started receiving updates.");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to start receiving: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }
        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.CallbackQuery)
            {
                await HandleCallbackQueryAsync(botClient, update.CallbackQuery!, cancellationToken);
                return;
            }

            if (update.Type != UpdateType.Message || update.Message?.Text == null)
                return;

            var message = update.Message;
            long userId = message.From!.Id;
            long chatId = message.Chat.Id;
            string userMessageText = message.Text.Trim();

            _logger.Info($"Received message from {message.From.Username ?? message.From.FirstName} ({userId}) in chat {chatId}: \"{userMessageText}\"");

            var user = await _userService.GetOrCreateUserAsync(userId);
            user.RequestTime = DateTime.Now; // Update request time

            string response = "Что-то пошло не так!";
            int lastSentMessageId = 0;

            // Handle pending commands
            if (user.PendingCommand != null)
            {
                bool isCommandHandled = true;
                switch (user.PendingCommand)
                {
                    case "setprompt":
                        user.CurrentCharacter.Prompt = userMessageText;
                        response = "Промпт установлен.";
                        break;
                    case "setun":
                        user.UserName = userMessageText;
                        response = "Имя пользователя установлено.";
                        break;
                    case "setud":
                        user.UserDescription = userMessageText;
                        response = "Описание пользователя установлено.";
                        break;
                    case "setcharname":
                        user.CurrentCharacter.Name = userMessageText;
                        response = $"Имя персонажа изменено на: {userMessageText}.";
                        break;
                    case "setgreeting":
                        user.CurrentCharacter.Greeting = userMessageText;
                        response = $"Приветствие персонажа изменено на: {userMessageText}.";
                        break;
                    default:
                        isCommandHandled = false;
                        break;
                }

                if (isCommandHandled)
                {
                    user.PendingCommand = null;
                    await _userService.SaveUserAsync(user); // Save updated user data
                    await DeleteCommandMessage(chatId, message.MessageId, cancellationToken);
                    lastSentMessageId = await SendMessage(chatId, response, cancellationToken: cancellationToken);
                }
                else
                {
                    // If pending command is not recognized, treat as regular chat
                    response = await _modelService.GetModelResponseAsync(user, userMessageText);
                    await _userService.AddChatMessageToUserAsync(userId, new ChatMessage("{{user}}", userMessageText));
                    await _userService.AddChatMessageToUserAsync(userId, new ChatMessage("{{char}}", response));
                    await _userService.SaveUserAsync(user); // Save updated chat and user data
                    lastSentMessageId = await SendMessage(chatId, response, cancellationToken: cancellationToken);
                }
            }
            else if (userMessageText.ToLower() == "/start")
            {
                var inlineKeyboard = new InlineKeyboardMarkup(
                [
                    [InlineKeyboardButton.WithCallbackData("Основные команды", "main_commands")],
                    [InlineKeyboardButton.WithCallbackData("Персонажи", "characters_menu")],
                    [InlineKeyboardButton.WithCallbackData("Другие настройки", "other_settings")]
                ]);
                await DeleteCommandMessage(chatId, message.MessageId, cancellationToken);
                lastSentMessageId = await SendMessage(chatId, "Выбери категорию:", inlineKeyboard, cancellationToken);
            }
            else
            {
                // Regular chat message
                response = await _modelService.GetModelResponseAsync(user, userMessageText);
                await _userService.AddChatMessageToUserAsync(userId, new ChatMessage("{{user}}", userMessageText));
                await _userService.AddChatMessageToUserAsync(userId, new ChatMessage("{{char}}", response));
                await _userService.SaveUserAsync(user); // Save updated chat and user data
                lastSentMessageId = await SendMessage(chatId, response, cancellationToken: cancellationToken);
                _logger.Info($"Sent message to {message.From.Username ?? message.From.FirstName} ({userId}). Request processing time: {(int)(DateTime.Now - user.RequestTime).TotalSeconds} seconds");
            }

            if (lastSentMessageId != 0)
                user.LastMessageId = lastSentMessageId;
        }

        private async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            var chatId = callbackQuery.Message!.Chat.Id;
            long userId = callbackQuery.From!.Id;
            string callbackData = callbackQuery.Data!;

            var user = await _userService.GetOrCreateUserAsync(userId);

            // Delete previous menu message
            if (user.LastMessageId != 0)
            {
                await DeleteCommandMessage(chatId, user.LastMessageId, cancellationToken);
                user.LastMessageId = 0; // Clear it to avoid deleting an already deleted message
                await _userService.SaveUserAsync(user);
            }

            string responseText = "";
            InlineKeyboardMarkup? keyboard = null;
            bool saveUserAfterAction = true; // Flag to indicate if user data needs saving

            switch (callbackData)
            {
                case "main_commands":
                    keyboard = new InlineKeyboardMarkup(
                    [
                        [InlineKeyboardButton.WithCallbackData("Установить Prompt AI", "setprompt")],
                        [InlineKeyboardButton.WithCallbackData("Установить имя пользователя", "setun")],
                        [InlineKeyboardButton.WithCallbackData("Установить описание пользователя", "setud")],
                        [InlineKeyboardButton.WithCallbackData("Очистить историю текущего чата", "clear_history")],
                        [InlineKeyboardButton.WithCallbackData("Сохранить данные", "save_data")],
                        [InlineKeyboardButton.WithCallbackData("Назад в главное меню", "start_menu")]
                    ]);
                    responseText = "Основные команды:";
                    break;

                case "characters_menu":
                    var characterButtons = new List<List<InlineKeyboardButton>>();
                    for (int i = 0; i < user.Characters.Count; i++)
                    {
                        characterButtons.Add([InlineKeyboardButton.WithCallbackData($"{user.Characters[i].Name} [ID:{i}]", $"select_char_{i}")]);
                    }
                    characterButtons.Add([InlineKeyboardButton.WithCallbackData("Создать нового персонажа", "new_character")]);
                    characterButtons.Add([InlineKeyboardButton.WithCallbackData("Переименовать текущего персонажа", "setcharname")]);
                    characterButtons.Add([InlineKeyboardButton.WithCallbackData("Изменить приветствие текущего персонажа", "setgreeting")]);
                    characterButtons.Add([InlineKeyboardButton.WithCallbackData("Назад в главное меню", "start_menu")]);
                    keyboard = new InlineKeyboardMarkup(characterButtons);
                    responseText = "Управление персонажами:";
                    break;

                case "other_settings":
                    responseText = "Тут будут другие настройки. Пока пусто.";
                    keyboard = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Назад в главное меню", "start_menu"));
                    break;

                case "setprompt":
                    user.PendingCommand = "setprompt";
                    responseText = "Введите новый промпт для текущего персонажа AI:";
                    break;

                case "setun":
                    user.PendingCommand = "setun";
                    responseText = "Введите новое имя пользователя (отображается в чате):";
                    break;

                case "setud":
                    user.PendingCommand = "setud";
                    responseText = "Введите новое описание для вашего пользователя (будет использоваться AI):";
                    break;

                case "setcharname":
                    user.PendingCommand = "setcharname";
                    responseText = "Введите новое имя для текущего персонажа:";
                    break;
                case "setgreeting":
                    user.PendingCommand = "setgreeting";
                    responseText = "Введите новое приветствие для текущего персонажа:";
                    break;

                case "clear_history":
                    user.CurrentCharacter.Chat = new List<ChatMessage>(20);
                    responseText = $"История текущего чата очищена!\n\n{user.CurrentCharacter.Greeting.Replace("{{user}}", user.UserName).Replace("{{char}}", user.CurrentCharacter.Name)}";
                    break;

                case "save_data":
                    await _userService.SaveUserAsync(user);
                    responseText = "Ваши данные сохранены.";
                    saveUserAfterAction = false; // Already saved
                    break;

                case "new_character":
                    var newChar = new CharacterPreset();
                    user.Characters.Add(newChar);
                    user.ChangeCurrentCharacter(user.Characters.Count - 1);
                    responseText = $"Создан новый персонаж '{newChar.Name}' [ID:{user.Characters.Count - 1}]. Он выбран по умолчанию.";
                    break;

                case "start_menu":
                    keyboard = new InlineKeyboardMarkup(
                    [
                        [InlineKeyboardButton.WithCallbackData("Основные команды", "main_commands")],
                        [InlineKeyboardButton.WithCallbackData("Персонажи", "characters_menu")],
                        [InlineKeyboardButton.WithCallbackData("Другие настройки", "other_settings")]
                    ]);
                    responseText = "Выбери категорию:";
                    break;

                default:
                    // Handle dynamic character selection
                    if (callbackData.StartsWith("select_char_"))
                    {
                        if (int.TryParse(callbackData.Replace("select_char_", ""), out int charIndex))
                        {
                            if (charIndex >= 0 && charIndex < user.Characters.Count)
                            {
                                user.ChangeCurrentCharacter(charIndex);
                                responseText = $"Персонаж '{user.CurrentCharacter.Name}' [ID:{charIndex}] выбран.\n" +
                                               $"Последнее (или стандартное) сообщение: " +
                                               $"{(user.CurrentCharacter.Chat.Count == 0 ? user.CurrentCharacter.Greeting : user.CurrentCharacter.Chat.Last().Content)}"
                                               .Replace("{{user}}", user.UserName).Replace("{{char}}", user.CurrentCharacter.Name);
                            }
                            else
                            {
                                responseText = "Неверный ID персонажа.";
                            }
                        }
                        else
                        {
                            responseText = "Неизвестная команда.";
                        }
                    }
                    else
                    {
                        responseText = "Неизвестная команда.";
                    }
                    break;
            }

            if (saveUserAfterAction && user.PendingCommand == null) // Only save if no pending command is set
            {
                await _userService.SaveUserAsync(user);
            }

            int sentMessageId = await SendMessage(chatId, responseText, keyboard, cancellationToken);
            user.LastMessageId = sentMessageId;
            await _userService.SaveUserAsync(user); // Save the updated LastMessageId

            await botClient.AnswerCallbackQuery(callbackQuery.Id); // Acknowledge the callback query
        }

        private async Task<int> SendMessage(long chatId, string response, ReplyMarkup? markup = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var sentMessage = await _botClient.SendMessage(
                    chatId: chatId,
                    text: response,
                    replyMarkup: markup,
                    cancellationToken: cancellationToken
                );
                return sentMessage.MessageId;
            }
            catch (Exception e)
            {
                _logger.Error($"Error sending message to chat {chatId}: {e.Message}");
                return -1;
            }
        }

        private async Task DeleteCommandMessage(long chatId, int messageId, CancellationToken cancellationToken)
        {
            try
            {
                await _botClient.DeleteMessage(chatId, messageId, cancellationToken);
                _logger.DebugInfo($"Deleted message {messageId} in chat {chatId}.");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error deleting message {messageId} in chat {chatId}: {ex.Message}");
            }
        }

        private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            _logger.Error($"Polling Error: {exception.Message}\n{exception.StackTrace}");
            return Task.CompletedTask;
        }
    }
}