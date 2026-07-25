using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TelegramBot.Services
{
    public class SubscriberInfo
    {
        public string? Username { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? LanguageCode { get; set; }
        public string? ChatType { get; set; }
        public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;
    }
    public static class SubscriberStore
    {
        private static readonly ConcurrentDictionary<long, SubscriberInfo> _subscribers = new();

        private static readonly string DataFolder = Path.Combine(AppContext.BaseDirectory, "Data");
        private static readonly string DataFile = Path.Combine(DataFolder, "subscribers.txt");

        static SubscriberStore()
        {
            try
            {
                if (!Directory.Exists(DataFolder))
                    Directory.CreateDirectory(DataFolder);

                if (File.Exists(DataFile))
                {
                    foreach (var line in File.ReadAllLines(DataFile, Encoding.UTF8))
                    {
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        // format: chatId\tusername\tfirstName\tlastName\tdisplayName\tphone\tlanguage\tchatType\tsubscribedAt
                        var parts = line.Split('\t');
                        if (parts.Length >= 1 && long.TryParse(parts[0], out var cid))
                        {
                            var info = new SubscriberInfo
                            {
                                Username = parts.Length > 1 ? parts[1] : null,
                                FirstName = parts.Length > 2 ? parts[2] : null,
                                LastName = parts.Length > 3 ? parts[3] : null,
                                DisplayName = parts.Length > 4 ? parts[4] : string.Empty,
                                Phone = parts.Length > 5 ? (string.IsNullOrWhiteSpace(parts[5]) ? null : parts[5]) : null,
                                LanguageCode = parts.Length > 6 ? parts[6] : null,
                                ChatType = parts.Length > 7 ? parts[7] : null,
                                SubscribedAt = parts.Length > 8 && DateTime.TryParse(parts[8], out var dt) ? dt : DateTime.UtcNow
                            };

                            _subscribers[cid] = info;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("SubscriberStore load error: " + ex.Message);
            }
        }

        private static void SaveToFile()
        {
            try
            {
                var lines = _subscribers.Select(kvp =>
                {
                    var chatId = kvp.Key;
                    var info = kvp.Value;
                    string Safe(string? s) => (s ?? string.Empty).Replace('\t', ' ').Replace('\n', ' ');
                    var subscribedAt = info.SubscribedAt.ToString("o");
                    return $"{chatId}\t{Safe(info.Username)}\t{Safe(info.FirstName)}\t{Safe(info.LastName)}\t{Safe(info.DisplayName)}\t{Safe(info.Phone)}\t{Safe(info.LanguageCode)}\t{Safe(info.ChatType)}\t{subscribedAt}";
                }).ToArray();

                File.WriteAllLines(DataFile, lines, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Console.WriteLine("SubscriberStore save error: " + ex.Message);
            }
        }

        public static void Add(long chatId,
            string displayName,
            string? phone = null,
            string? username = null,
            string? firstName = null,
            string? lastName = null,
            string? language = null,
            string? chatType = null)
        {
            var info = new SubscriberInfo
            {
                Username = username,
                FirstName = firstName,
                LastName = lastName,
                DisplayName = displayName,
                Phone = phone,
                LanguageCode = language,
                ChatType = chatType,
                SubscribedAt = DateTime.UtcNow
            };

            _subscribers.AddOrUpdate(chatId, info, (k, v) =>
            {
                // keep existing subscribedAt
                info.SubscribedAt = v.SubscribedAt;
                return info;
            });

            SaveToFile();
        }

        public static void Remove(long chatId)
        {
            _subscribers.TryRemove(chatId, out _);
            SaveToFile();
        }

        public static void SetPhone(long chatId, string phone)
        {
            _subscribers.AddOrUpdate(chatId,
                new SubscriberInfo { DisplayName = string.Empty, Phone = phone, SubscribedAt = DateTime.UtcNow },
                (k, v) => { v.Phone = phone; return v; });
            SaveToFile();
        }

        public static List<string> GetDisplayNames()
        {
            return _subscribers.Values.Select(v => v.DisplayName).Where(x => !string.IsNullOrEmpty(x)).ToList();
        }

        public static string GetDisplayListText()
        {
            var list = GetDisplayNames();
            return list.Count == 0 ? "Нет подписчиков" : string.Join(", ", list);
        }

        public static List<long> GetSubscriberIds()
        {
            return _subscribers.Keys.ToList();
        }

        public static List<KeyValuePair<long, SubscriberInfo>> GetSubscribers()
        {
            return _subscribers.ToList();
        }
    }
}
