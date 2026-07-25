using System.Collections.Concurrent;
using System.Linq;

namespace TelegramBot.Services
{
    public static class SubscriberStore
    {
        private static readonly ConcurrentDictionary<long, string> _subscribers = new();

        public static void Add(long chatId, string displayName)
        {
            _subscribers.AddOrUpdate(chatId, displayName, (_, __) => displayName);
        }

        public static void Remove(long chatId)
        {
            _subscribers.TryRemove(chatId, out _);
        }

        public static List<string> GetDisplayNames()
        {
            return _subscribers.Values.ToList();
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

        public static List<KeyValuePair<long, string>> GetSubscribers()
        {
            return _subscribers.ToList();
        }
    }
}
