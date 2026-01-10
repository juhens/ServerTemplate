using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using ServerCore;

namespace GameServer.Cache
{
    public class MockCacheManager : ICacheManager
    {
        private enum EntryType
        {
            String,
            Hash,
            Set,
            List,
        }

        private class MockEntry
        {
            public EntryType Type { get; init; }
            public DateTime? ExpiredDateTime { get; set; }
            public byte[]? Payload { get; init; }
            public ConcurrentDictionary<string, byte[]>? HashFields { get; init; }
            public ConcurrentDictionary<string, byte>? SetItems { get; init; }
            public List<byte[]>? ListItems { get; init; }
        }

        private readonly ConcurrentDictionary<string, MockEntry> _storage = new();

        private MockCacheManager() { }
        public static MockCacheManager Instance { get; } = new MockCacheManager();

        public void Connect(string connectionString = "localhost:6379", int delay = 5000)
        {
            Log.Info(this, "MockRedis 연결 성공! (In-Memory Mode)");
        }

        private static bool CheckType<T>() where T : unmanaged
        {
            var type = typeof(T);
            return type.IsPrimitive
                   || type.IsEnum
                   || type == typeof(decimal)
                   || type == typeof(Guid)
                   || type == typeof(DateTime)
                   || type == typeof(TimeSpan)
                   || typeof(IValue).IsAssignableFrom(type);
        }

        private bool TryGetValidEntry(string key, out MockEntry entry)
        {
            entry = null!;
            if (!_storage.TryGetValue(key, out var storedEntry)) return false;

            if (storedEntry.ExpiredDateTime.HasValue && storedEntry.ExpiredDateTime.Value < DateTime.UtcNow)
            {
                _storage.TryRemove(key, out _);
                return false;
            }

            entry = storedEntry;
            return true;
        }

        public bool TrySetValue<T>(string key, in T t, TimeSpan? expiry = null) where T : unmanaged
        {
            if (string.IsNullOrEmpty(key)) return false;

            if (!CheckType<T>())
            {
#if DEBUG
                Environment.FailFast($"잘못넣었네 확인해라 T:{typeof(T)}");
#endif
                return false;
            }

            try
            {
                var bytes = new byte[Unsafe.SizeOf<T>()];
                Unsafe.WriteUnaligned(ref bytes[0], t);
                _storage[key] = new MockEntry
                {
                    Type = EntryType.String,
                    Payload = bytes,
                    ExpiredDateTime = expiry.HasValue ? DateTime.UtcNow + expiry : null
                };
                return true;
            }
            catch (Exception e) { Log.Error(this, "{0}", e); return false; }
        }
        public bool TryGetValue<T>(string key, out T t) where T : unmanaged
        {
            t = default;
            if (string.IsNullOrEmpty(key)) return false;

            if (!CheckType<T>())
            {
#if DEBUG
                Environment.FailFast($"잘못넣었네 확인해라 T:{typeof(T)}");
#endif
                return false;
            }

            if (TryGetValidEntry(key, out var entry) && entry.Type == EntryType.String)
            {
                if (entry.Payload!.Length != Unsafe.SizeOf<T>()) return false;
                t = Unsafe.ReadUnaligned<T>(ref entry.Payload[0]);
                return true;
            }
            return false;
        }

        public bool TrySetString(string key, string value, TimeSpan? expiry = null)
        {
            if (string.IsNullOrEmpty(key)) return false;
            try
            {
                _storage[key] = new MockEntry
                {
                    Type = EntryType.String,
                    Payload = Encoding.UTF8.GetBytes(value),
                    ExpiredDateTime = expiry.HasValue ? DateTime.UtcNow + expiry : null
                };
                return true;
            }
            catch (Exception e) { Log.Error(this, "{0}", e); return false; }
        }
        public bool TryGetString(string key, out string value)
        {
            value = null!;
            if (string.IsNullOrEmpty(key)) return false;
            if (TryGetValidEntry(key, out var entry) && entry.Type == EntryType.String)
            {
                value = Encoding.UTF8.GetString(entry.Payload!);
                return true;
            }
            return false;
        }

        public bool TrySetObject<T>(string key, T t, TimeSpan? expiry = null)
        {
            if (string.IsNullOrEmpty(key)) return false;
            try
            {
                _storage[key] = new MockEntry
                {
                    Type = EntryType.String,
                    Payload = JsonSerializer.SerializeToUtf8Bytes(t, typeof(T), CacheModelJsonContext.Default),
                    ExpiredDateTime = expiry.HasValue ? DateTime.UtcNow + expiry : null
                };
                return true;
            }
            catch (Exception e) { Log.Error(this, "{0}", e); return false; }
        }
        public bool TryGetObject<T>(string key, out T value)
        {
            value = default!;
            if (string.IsNullOrEmpty(key)) return false;
            if (TryGetValidEntry(key, out var entry) && entry.Type == EntryType.String)
            {
                try
                {
                    var result = JsonSerializer.Deserialize(entry.Payload!, typeof(T), CacheModelJsonContext.Default);
                    if (result != null)
                    {
                        value = (T)result;
                        return true;
                    }
                }
                catch (Exception e) { Log.Error(this, "{0}", e); }
            }
            return false;
        }

        public bool TrySetHashValue<T>(string key, string field, in T value) where T : unmanaged
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(field)) return false;
            if (!CheckType<T>())
            {
#if DEBUG
                Environment.FailFast($"잘못넣었네 확인해라 T:{typeof(T)}");
#endif
                return false;
            }

            try
            {
                var entry = _storage.GetOrAdd(key, _ => new MockEntry { Type = EntryType.Hash, HashFields = new ConcurrentDictionary<string, byte[]>() });
                if (entry.Type != EntryType.Hash) return false;

                var size = Unsafe.SizeOf<T>();
                var bytes = new byte[size];

                MemoryMarshal.Write(bytes, in value);
                entry.HashFields![field] = bytes;
                return true;
            }
            catch (Exception e) { Log.Error(this, "{0}", e); return false; }
        }
        public bool TryGetHashValue<T>(string key, string field, out T value) where T : unmanaged
        {
            value = default;
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(field)) return false;
            if (!CheckType<T>())
            {
#if DEBUG
                Environment.FailFast($"잘못넣었네 확인해라 T:{typeof(T)}");
#endif
                return false;
            }

            if (TryGetValidEntry(key, out var entry) && entry.Type == EntryType.Hash)
            {
                if (entry.HashFields!.TryGetValue(field, out var bytes) && bytes.Length == Unsafe.SizeOf<T>())
                {
                    value = Unsafe.ReadUnaligned<T>(ref bytes[0]);
                    return true;
                }
            }
            return false;
        }
        public bool TryHashGetAllValue<T>(string key, out Dictionary<string, T> values) where T : unmanaged
        {
            values = [];
            if (string.IsNullOrEmpty(key)) return false;
            if (!CheckType<T>())
            {
#if DEBUG
                Environment.FailFast($"잘못넣었네 확인해라 T:{typeof(T)}");
#endif
                return false;
            }

            if (TryGetValidEntry(key, out var entry) && entry.Type == EntryType.Hash)
            {
                var size = Unsafe.SizeOf<T>();
                foreach (var kvp in entry.HashFields!)
                {
                    if (kvp.Value.Length == size) values[kvp.Key] = Unsafe.ReadUnaligned<T>(ref kvp.Value[0]);
                    else Log.Error(this, $"[Data Size Mismatch] Key:{key}, Field:{kvp.Key} Exp:{size}, Act:{kvp.Value.Length}");
                }
                return values.Count > 0;
            }
            return false;
        }

        public bool TrySetHashString(string key, string field, string value)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(field)) return false;
            try
            {
                var entry = _storage.GetOrAdd(key, _ => new MockEntry { Type = EntryType.Hash, HashFields = new ConcurrentDictionary<string, byte[]>() });
                if (entry.Type != EntryType.Hash) return false;
                entry.HashFields![field] = Encoding.UTF8.GetBytes(value);
                return true;
            }
            catch (Exception e) { Log.Error(this, "{0}", e); return false; }
        }
        public bool TryGetHashString(string key, string field, out string value)
        {
            value = null!;
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(field)) return false;
            if (TryGetValidEntry(key, out var entry) && entry.Type == EntryType.Hash)
            {
                if (entry.HashFields!.TryGetValue(field, out var bytes)) { value = Encoding.UTF8.GetString(bytes); return true; }
            }
            return false;
        }
        public bool TryHashGetAllString(string key, out Dictionary<string, string> values)
        {
            values = [];
            if (string.IsNullOrEmpty(key)) return false;
            if (TryGetValidEntry(key, out var entry) && entry.Type == EntryType.Hash)
            {
                foreach (var kvp in entry.HashFields!) values[kvp.Key] = Encoding.UTF8.GetString(kvp.Value);
                return values.Count > 0;
            }
            return false;
        }

        public bool TrySetHashObject<T>(string key, string field, T value)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(field)) return false;
            try
            {
                var entry = _storage.GetOrAdd(key, _ => new MockEntry { Type = EntryType.Hash, HashFields = new ConcurrentDictionary<string, byte[]>() });
                if (entry.Type != EntryType.Hash) return false;
                entry.HashFields![field] = JsonSerializer.SerializeToUtf8Bytes(value, typeof(T), CacheModelJsonContext.Default);
                return true;
            }
            catch (Exception e) { Log.Error(this, "{0}", e); return false; }
        }
        public bool TryGetHashObject<T>(string key, string field, out T value)
        {
            value = default!;
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(field)) return false;
            if (TryGetValidEntry(key, out var entry) && entry.Type == EntryType.Hash)
            {
                if (entry.HashFields!.TryGetValue(field, out var bytes))
                {
                    try
                    {
                        value = (T)JsonSerializer.Deserialize(bytes, typeof(T), CacheModelJsonContext.Default)!;
                        return true;
                    }
                    catch (Exception e) { Log.Error(this, "{0}", e); }
                }
            }
            return false;
        }
        public bool TryHashGetAllObject<T>(string key, out Dictionary<string, T> values)
        {
            values = [];
            if (string.IsNullOrEmpty(key)) return false;
            if (TryGetValidEntry(key, out var entry) && entry.Type == EntryType.Hash)
            {
                foreach (var kvp in entry.HashFields!)
                {
                    try
                    {
                        var obj = JsonSerializer.Deserialize(kvp.Value, typeof(T), CacheModelJsonContext.Default);
                        if (obj != null) values[kvp.Key] = (T)obj;
                    }
                    catch (Exception e) { Log.Error(this, "{0}", e); }
                }
                return values.Count > 0;
            }
            return false;
        }

        public bool TryKeyExists(string key, out bool result)
        {
            result = false;
            if (string.IsNullOrEmpty(key)) return false;
            result = TryGetValidEntry(key, out _);
            return true;
        }
        public bool TryKeyDelete(string key, out bool result)
        {
            result = false;
            if (string.IsNullOrEmpty(key)) return false;
            result = _storage.TryRemove(key, out _);
            return true;
        }
        public bool TryKeyExpire(string key, TimeSpan expiry)
        {
            if (string.IsNullOrEmpty(key)) return false;
            if (TryGetValidEntry(key, out var entry)) { entry.ExpiredDateTime = DateTime.UtcNow + expiry; return true; }
            return false;
        }

        public bool TrySetAddValue<T>(string key, in T value) where T : unmanaged
        {
            if (string.IsNullOrEmpty(key)) return false;
            if (!CheckType<T>())
            {
#if DEBUG
                Environment.FailFast($"잘못넣었네 확인해라 T:{typeof(T)}");
#endif
                return false;
            }

            try
            {
                var entry = _storage.GetOrAdd(key, _ => new MockEntry { Type = EntryType.Set, SetItems = new ConcurrentDictionary<string, byte>() });
                if (entry.Type != EntryType.Set) return false;

                var size = Unsafe.SizeOf<T>();
                var bytes = new byte[size];

                MemoryMarshal.Write(bytes, in value);
                return entry.SetItems!.TryAdd(Convert.ToBase64String(bytes), 0);
            }
            catch (Exception e) { Log.Error(this, "{0}", e); return false; }
        }
        public bool TrySetRemoveValue<T>(string key, in T value) where T : unmanaged
        {
            if (string.IsNullOrEmpty(key)) return false;
            if (!CheckType<T>())
            {
#if DEBUG
                Environment.FailFast($"잘못넣었네 확인해라 T:{typeof(T)}");
#endif
                return false;
            }

            if (TryGetValidEntry(key, out var entry) && entry.Type == EntryType.Set)
            {
                var size = Unsafe.SizeOf<T>();
                var bytes = new byte[size];

                MemoryMarshal.Write(bytes, in value);
                return entry.SetItems!.TryRemove(Convert.ToBase64String(bytes), out _);
            }
            return false;
        }
        public bool TrySetMembersValue<T>(string key, out List<T> values) where T : unmanaged
        {
            values = [];
            if (string.IsNullOrEmpty(key)) return false;
            if (!CheckType<T>())
            {
#if DEBUG
                Environment.FailFast($"잘못넣었네 확인해라 T:{typeof(T)}");
#endif
                return false;
            }

            if (TryGetValidEntry(key, out var entry) && entry.Type == EntryType.Set)
            {
                var size = Unsafe.SizeOf<T>();
                foreach (var item in entry.SetItems!.Keys)
                {
                    var bytes = Convert.FromBase64String(item);
                    if (bytes.Length == size) values.Add(Unsafe.ReadUnaligned<T>(ref bytes[0]));
                }
                return true;
            }
            return false;
        }

        public bool TrySetAddString(string key, string value)
        {
            if (string.IsNullOrEmpty(key)) return false;
            try
            {
                var entry = _storage.GetOrAdd(key, _ => new MockEntry { Type = EntryType.Set, SetItems = new ConcurrentDictionary<string, byte>() });
                if (entry.Type != EntryType.Set) return false;
                return entry.SetItems!.TryAdd(value, 0);
            }
            catch (Exception e) { Log.Error(this, "{0}", e); return false; }
        }
        public bool TrySetRemoveString(string key, string value)
        {
            if (string.IsNullOrEmpty(key)) return false;
            if (TryGetValidEntry(key, out var entry) && entry.Type == EntryType.Set) return entry.SetItems!.TryRemove(value, out _);
            return false;
        }
        public bool TrySetMembersString(string key, out List<string> values)
        {
            values = [];
            if (string.IsNullOrEmpty(key)) return false;
            if (TryGetValidEntry(key, out var entry) && entry.Type == EntryType.Set) { values.AddRange(entry.SetItems!.Keys); return true; }
            return false;
        }

        public bool TrySetAddObject<T>(string key, T value)
        {
            if (string.IsNullOrEmpty(key)) return false;
            try
            {
                var entry = _storage.GetOrAdd(key, _ => new MockEntry { Type = EntryType.Set, SetItems = new ConcurrentDictionary<string, byte>() });
                if (entry.Type != EntryType.Set) return false;
                return entry.SetItems!.TryAdd(JsonSerializer.Serialize(value, typeof(T), CacheModelJsonContext.Default), 0);
            }
            catch (Exception e) { Log.Error(this, "{0}", e); return false; }
        }
        public bool TrySetRemoveObject<T>(string key, T value)
        {
            if (string.IsNullOrEmpty(key)) return false;
            if (TryGetValidEntry(key, out var entry) && entry.Type == EntryType.Set) return entry.SetItems!.TryRemove(JsonSerializer.Serialize(value, typeof(T), CacheModelJsonContext.Default), out _);
            return false;
        }
        public bool TrySetMembersObject<T>(string key, out List<T> values)
        {
            values = [];
            if (string.IsNullOrEmpty(key)) return false;
            if (TryGetValidEntry(key, out var entry) && entry.Type == EntryType.Set)
            {
                foreach (var item in entry.SetItems!.Keys)
                {
                    try
                    {
                        var obj = JsonSerializer.Deserialize(item, typeof(T), CacheModelJsonContext.Default);
                        if (obj != null) values.Add((T)obj);
                    }
                    catch (Exception e) { Log.Error(this, "{0}", e); }
                }
                return true;
            }
            return false;
        }

        public bool TryListRightPushValue<T>(string key, in T value) where T : unmanaged
        {
            if (string.IsNullOrEmpty(key)) return false;
            if (!CheckType<T>())
            {
#if DEBUG
                Environment.FailFast($"잘못넣었네 확인해라 T:{typeof(T)}");
#endif
                return false;
            }

            try
            {
                var entry = _storage.GetOrAdd(key, _ => new MockEntry { Type = EntryType.List, ListItems = [] });
                if (entry.Type != EntryType.List)
                    return false;

                var size = Unsafe.SizeOf<T>();
                var bytes = new byte[size];

                MemoryMarshal.Write(bytes, in value);
                lock (entry.ListItems!)
                {
                    entry.ListItems.Add(bytes);
                }

                return true;
            }
            catch (Exception e)
            {
                Log.Error(this, "{0}", e);
            }
            return false;
        }
        public bool TryListRangeValue<T>(string key, out List<T> values, int start = 0, int stop = -1) where T : unmanaged
        {
            values = [];
            if (string.IsNullOrEmpty(key)) return false;
            if (!CheckType<T>())
            {
#if DEBUG
                Environment.FailFast($"잘못넣었네 확인해라 T:{typeof(T)}");
#endif
                return false;
            }

            if (TryGetValidEntry(key, out var entry) && entry.Type == EntryType.List)
            {
                lock (entry.ListItems!)
                {
                    var count = entry.ListItems.Count;
                    if (stop < 0) stop = count + stop;
                    var size = Unsafe.SizeOf<T>();
                    for (var i = start; i <= stop && i < count; i++)
                    {
                        if (entry.ListItems[i].Length != size) continue;

                        values.Add(Unsafe.ReadUnaligned<T>(ref entry.ListItems[i][0]));
                    }
                }
                return true;
            }
            return false;
        }

        public bool TryListRightPushString(string key, string value)
        {
            if (string.IsNullOrEmpty(key)) return false;
            try
            {
                var entry = _storage.GetOrAdd(key, _ => new MockEntry { Type = EntryType.List, ListItems = [] });
                if (entry.Type != EntryType.List) return false;
                lock (entry.ListItems!)
                {
                    entry.ListItems.Add(Encoding.UTF8.GetBytes(value));
                }
                return true;
            }
            catch (Exception e) { Log.Error(this, "{0}", e); return false; }
        }
        public bool TryListRangeString(string key, out List<string> values, int start = 0, int stop = -1)
        {
            values = [];
            if (string.IsNullOrEmpty(key)) return false;
            if (TryGetValidEntry(key, out var entry) && entry.Type == EntryType.List)
            {
                lock (entry.ListItems!)
                {
                    var count = entry.ListItems.Count;
                    if (stop < 0)
                        stop = count + stop;
                    for (var i = start; i <= stop && i < count; i++)
                        values.Add(Encoding.UTF8.GetString(entry.ListItems[i]));
                }
                return true;
            }
            return false;
        }

        public bool TryListRightPushObject<T>(string key, T value)
        {
            if (string.IsNullOrEmpty(key)) return false;
            try
            {
                var entry = _storage.GetOrAdd(key, _ => new MockEntry { Type = EntryType.List, ListItems = [] });
                if (entry.Type != EntryType.List) return false;
                lock (entry.ListItems!)
                {
                    entry.ListItems.Add(JsonSerializer.SerializeToUtf8Bytes(value, typeof(T), CacheModelJsonContext.Default));
                }
                return true;
            }
            catch (Exception e) { Log.Error(this, "{0}", e); return false; }
        }
        public bool TryListRangeObject<T>(string key, out List<T> values, int start = 0, int stop = -1)
        {
            values = [];
            if (string.IsNullOrEmpty(key)) return false;
            if (TryGetValidEntry(key, out var entry) && entry.Type == EntryType.List)
            {
                lock (entry.ListItems!)
                {
                    var count = entry.ListItems.Count;
                    if (stop < 0) stop = count + stop;
                    for (var i = start; i <= stop && i < count; i++)
                    {
                        try
                        {
                            var obj = JsonSerializer.Deserialize(entry.ListItems[i], typeof(T), CacheModelJsonContext.Default);
                            if (obj is T t)
                            {
                                values.Add(t);
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Error(this, "{0}", e);
                        }
                    }
                }
                return true;
            }
            return false;
        }

        public void Clear() => _storage.Clear();
    }
}
