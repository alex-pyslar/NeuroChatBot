using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using MongoDB.Driver.Linq;
using NeuroChatBot.Core;
using NeuroChatBot.Models;
// Removed using Microsoft.Extensions.Configuration; as it's no longer directly used for model path/args

namespace NeuroChatBot.Services
{
    public class LlamaCppModelService : IModelService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        // Removed _pathToModel, _cpuMode as they are no longer relevant for a remote server

        public LlamaCppModelService(HttpClient httpClient, ILogger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            // _httpClient.Timeout is now set during DI configuration in Program.cs
        }

        public async Task<string> GetModelResponseAsync(User user, string userMessage)
        {
            try
            {
                // Ensure chat history is not empty for greeting if needed
                if (user.CurrentCharacter.Chat.Count == 0 && !string.IsNullOrEmpty(user.CurrentCharacter.Greeting?.Trim()))
                {
                    user.CurrentCharacter.Chat.Add(new ChatMessage(RoleEnums.Assistant, user.CurrentCharacter.Greeting.Replace("{{user}}", user.UserName).Replace("{{char}}", user.CurrentCharacter.Name)));
                }

                // Build the prompt string based on the original logic
                var messages = new List<object>
                {
                    new
                    {
                        role = RoleEnums.System.String(),
                        content = $"Ты RPHentaiMaster.\n" +
                                $"Ты мастерки умеешь рассказывать пикантные истории в во всех подробностях.\n" +
                                $"Ты не сдерживаешь себя в выражениях."
                    }
                };

                if (!string.IsNullOrEmpty(user.CurrentCharacter.Prompt?.Trim()))
                    messages.Add(new { role = RoleEnums.System.String(), content = user.CurrentCharacter.Prompt.Replace("{{user}}", user.UserName).Replace("{{char}}", user.CurrentCharacter.Name) });
                if (!string.IsNullOrEmpty(user.UserDescription?.Trim()))
                    messages.Add(new { role = RoleEnums.System.String(), content = user.UserDescription.Replace("{{user}}", user.UserName).Replace("{{char}}", user.CurrentCharacter.Name) });

                // Add existing chat history to the prompt
                foreach (var msg in user.CurrentCharacter.Chat)
                {
                    messages.Add(new { role = msg.Role, content = msg.Content });
                }
                // Add current user message
                messages.Add(new { role = RoleEnums.User.String(), content = userMessage });
                _logger?.DebugInfo($"user: {userMessage}");

                //messages = messages.Replace("{{char}}", user.CurrentCharacter.Name);
                //messages = messages.Replace("{{user}}", user.UserName);

                _logger?.DebugInfo($"Sending prompt to Llama-server:\n{messages}");

                var request = new
                {
                    // model property might not be needed if llama-server only serves one model,
                    // or you might need to specify it if the server supports multiple.
                    // Leaving it for now as it was in the original, but can be removed.
                    //model = "llama-model", // This might need to be adjusted based on your actual llama-server setup
                    messages,
                    max_tokens = 768,
                    temperature = 1,
                    min_p = 0.05,
                    top_p = 1.0,
                    top_k = 0.0,
                    repeat_penalty = 1.3,
                    presence_penalty = 0.0,
                    frequency_penalty = 0.0,
                    //stop = new string[] { "</s>", "[/INST]", $"\n{user.UserName}:", $"\n{user.CurrentCharacter.Name}:" },
                };

                // Endpoint is /v1/completions relative to BaseAddress set in Program.cs
                var response = await _httpClient.PostAsJsonAsync("/v1/chat/completions", request);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>();
                string modelReply = result?.choices?.FirstOrDefault()?.message.content ?? "Ошибка: нет ответа";

                _logger?.DebugInfo($"AI_Reply: {modelReply}");
                return modelReply;
            }
            catch (HttpRequestException httpEx)
            {
                _logger?.Error($"HTTP Request Error to Llama-server: {httpEx.Message}. Status Code: {httpEx.StatusCode}");
                return "Ошибка: Не удалось получить ответ от нейросети. Проверьте адрес и доступность llama-server.";
            }
            catch (Exception ex)
            {
                _logger?.Error($"Ошибка ответа модели: {ex.Message} {ex.StackTrace}");
                return "Ошибка: Что-то пошло не так при генерации ответа!";
            }
        }
    }
}