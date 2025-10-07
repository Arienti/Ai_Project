using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Ai_Project.DTOs;

namespace Ai_Project.Services
{
    public class ChatStorageService
    {
        private readonly string _storagePath;
        private readonly JsonSerializerOptions _jsonOptions;

        public ChatStorageService()
        {
            _storagePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AiProject",
                "Chats"
            );

            Directory.CreateDirectory(_storagePath);

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };
        }

        public void SaveChat(ChatDTO chat)
        {
            chat.LastModified = DateTime.Now;
            var fileName = $"{chat.Id}.json";
            var filePath = Path.Combine(_storagePath, fileName);
            var json = JsonSerializer.Serialize(chat, _jsonOptions);
            File.WriteAllText(filePath, json);
        }

        public ChatDTO? LoadChat(string chatId)
        {
            var filePath = Path.Combine(_storagePath, $"{chatId}.json");
            if (!File.Exists(filePath))
                return null;

            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<ChatDTO>(json);
        }

        public List<ChatDTO> LoadAllChats()
        {
            var chats = new List<ChatDTO>();
            var files = Directory.GetFiles(_storagePath, "*.json");

            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var chat = JsonSerializer.Deserialize<ChatDTO>(json);
                    if (chat != null)
                        chats.Add(chat);
                }
                catch
                {
                    // Skip corrupted files
                }
            }

            return chats.OrderByDescending(c => c.LastModified).ToList();
        }

        public void DeleteChat(string chatId)
        {
            var filePath = Path.Combine(_storagePath, $"{chatId}.json");
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        public void DeleteAllChats()
        {
            var files = Directory.GetFiles(_storagePath, "*.json");
            foreach (var file in files)
                File.Delete(file);
        }
    }
}