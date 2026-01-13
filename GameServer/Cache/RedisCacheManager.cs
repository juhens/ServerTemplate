using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using GameServer.Logic.Dto;
using ServerCore;
using StackExchange.Redis;

namespace GameServer.Cache
{
    public static class MockRedis
    {
        private static readonly ConcurrentDictionary<int, WorldInfoDto> WorldInfoDict = [];


        public static long? GetUserIdByToken(string token)
        {
            // 테스트를 위해 토큰 문자열 자체가 ID라고 가정하고 파싱 시도
            // 예: "100" -> 100
            if (long.TryParse(token, out var dbId))
            {
                return dbId;
            }

            // 파싱 실패 시 (잘못된 토큰)
            return null;
        }

        public static void UpdateWorldInfoList(List<WorldInfoDto> worldInfoList)
        {
            foreach (var worldInfo in worldInfoList)
            {
                WorldInfoDict.TryAdd(worldInfo.WorldStaticId, worldInfo);
            }
        }

        public static List<WorldInfoDto> GetWorldInfoList()
        {
            var worldInfoList = new List<WorldInfoDto>();
            foreach (var worldInfo in WorldInfoDict.Values)
            {
                worldInfoList.Add(worldInfo);
            }

            return worldInfoList;
        }
    }




    public class RedisCacheManager : ICacheManager
    {

        private RedisCacheManager() {}
        public static RedisCacheManager Instance { get; } = new RedisCacheManager();

        private ConnectionMultiplexer? _connection;

        private IDatabase? _db;

        public void Connect(string connectionString = "localhost:6379", int delay = 5000)
        {
            var tryCount = 0;

            while (true)
            {
                tryCount++;
                try
                {
                    Log.Info(this, "Redis 연결 시도 중... (시도:{0})", tryCount);
                    _connection = ConnectionMultiplexer.Connect(connectionString);
                    _db = _connection.GetDatabase();
                    Log.Info(this, "Redis 연결 성공!");
                    break;
                }
                catch (Exception ex)
                {
                    Log.Error(this, "Redis 연결 실패 (시도 #{0}): {1}", tryCount, ex.Message);
                    Thread.Sleep(delay);
                }
            }
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

        public bool TrySetValue<T>(string key, in T value, TimeSpan? expiry = null) where T : unmanaged
        {
            if (_db is null) return false;
            if (string.IsNullOrEmpty(key)) return false;

            if (!CheckType<T>())
            {
#if DEBUG
                Environment.FailFast($"잘못넣었네 확인해라 T:{typeof(T)}");
#endif
                return false;
            }


            var size = Unsafe.SizeOf<T>();
            var buffer = ArrayPool<byte>.Shared.Rent(size);
            try
            {
                MemoryMarshal.Write(buffer, in value);
                var memory = new ReadOnlyMemory<byte>(buffer, 0, size);
                return _db.StringSet(key, memory, expiry, When.Always);
            }
            catch(Exception e)
            {
                Log.Error(this, "{0}", e);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
            return false;
        }
        public bool TryGetValue<T>(string key, out T value) where T : unmanaged
        {
            value = default;
            if (_db is null) return false;
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
                var val = _db.StringGet(key);
                if (val.IsNullOrEmpty) return false;

                ReadOnlyMemory<byte> memory = val;

                if (memory.Length != Unsafe.SizeOf<T>()) return false;
                value = MemoryMarshal.Read<T>(memory.Span);
                return true;
            }
            catch (Exception e)
            {
                Log.Error(this, "{0}", e);
            }

            return false;
        }

        public bool TrySetString(string key, string value, TimeSpan? expiry = null)
        {
            if (_db is null) return false;
            if (string.IsNullOrEmpty(key)) return false;

            try
            {
                return _db.StringSet(key, value, expiry, When.Always);
            }
            catch (Exception e)
            {
                Log.Error(this, "{0}", e);
            }
            return false;
        }
        public bool TryGetString(string key, out string value)
        {
            value = null!;
            if (_db is null) return false;
            if (string.IsNullOrEmpty(key)) return false;

            try
            {
                var val = _db.StringGet(key);
                if (val.IsNullOrEmpty) return false;

                value = val.ToString();

                return true;
            }
            catch (Exception e)
            {
                Log.Error(this, "{0}", e);
            }

            return false;
        }

        public bool TrySetObject<T>(string key, T value, TimeSpan? expiry = null)
        {
            if (_db is null) return false;
            if (string.IsNullOrEmpty(key)) return false;

            try
            {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(value, typeof(T), CacheModelJsonContext.Default);

                return _db.StringSet(key, bytes, expiry, When.Always);
            }
            catch (Exception e)
            {
                Log.Error(this, "{0}", e);
            }
            return false;
        }
        public bool TryGetObject<T>(string key, out T value)
        {
            value = default!;
            if (_db is null) return false;
            if (string.IsNullOrEmpty(key)) return false;

            try
            {
                var val = _db.StringGet(key);
                if (val.IsNullOrEmpty) return false;

                ReadOnlyMemory<byte> memory = val;
                var obj = JsonSerializer.Deserialize(memory.Span, typeof(T), CacheModelJsonContext.Default);

                if (obj is not T t) return false;

                value = t;
                return true;
            }
            catch (Exception e)
            {
                Log.Error(this, "{0}", e);
            }

            return false;
        }

        public bool TrySetHashValue<T>(string key, string field, in T value) where T : unmanaged
        {
            if (_db is null) return false;
            if (string.IsNullOrEmpty(key)) return false;
            if (string.IsNullOrEmpty(field)) return false;

            if (!CheckType<T>())
            {
#if DEBUG
                Environment.FailFast($"잘못넣었네 확인해라 T:{typeof(T)}");
#endif
                return false;
            }

            var size = Unsafe.SizeOf<T>();
            var buffer = ArrayPool<byte>.Shared.Rent(size);
            try
            {
                MemoryMarshal.Write(buffer, in value);
                var memory = new ReadOnlyMemory<byte>(buffer, 0, size);

                // true: 새로들어감, false: 덮어씀
                _db.HashSet(key, field, memory);

                return true;
            }
            catch (Exception e)
            {
                Log.Error(this, "{0}", e);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
            return false;
        }
        public bool TryGetHashValue<T>(string key, string field, out T value) where T : unmanaged
        {
            value = default;
            if (_db is null) return false;
            if (string.IsNullOrEmpty(key)) return false;
            if (string.IsNullOrEmpty(field)) return false;

            if (!CheckType<T>())
            {
#if DEBUG
                Environment.FailFast($"잘못넣었네 확인해라 T:{typeof(T)}");
#endif
                return false;
            }

            try
            {
                var val = _db.HashGet(key, field);
                if (val.IsNullOrEmpty) return false;

                ReadOnlyMemory<byte> memory = val;

                if (memory.Length != Unsafe.SizeOf<T>()) return false;
                value = MemoryMarshal.Read<T>(memory.Span);
                return true;
            }
            catch (Exception e)
            {
                Log.Error(this, "{0}", e);
            }
            return false;
        }
        public bool TryHashGetAllValue<T>(string key, out Dictionary<string, T> values) where T : unmanaged
        {
            values = [];

            if (_db is null) return false;
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
                var entries = _db.HashGetAll(key);
                if (entries.Length == 0) return false;

                var size = Unsafe.SizeOf<T>();

                foreach (var entry in entries)
                {
                    if (entry.Value.IsNullOrEmpty) continue;

                    ReadOnlyMemory<byte> memory = entry.Value;

                    if (memory.Length != size) continue;

                    var value = MemoryMarshal.Read<T>(memory.Span);

                    values[entry.Name.ToString()] = value;
                }

                return values.Count > 0;
            }
            catch (Exception e)
            {
                Log.Error(this, "{0}", e);
            }
            return false;
        }

        public bool TrySetHashString(string key, string field, string value)
        {
            if (_db is null) return false;
            if (string.IsNullOrEmpty(key)) return false;
            if (string.IsNullOrEmpty(field)) return false;

            try
            {
                return _db.HashSet(key, field, value);
            }
            catch (Exception e)
            {
                Log.Error(this, "{0}", e);

            }
            return false;
        }
        public bool TryGetHashString(string key, string field, out string value)
        {
            value = null!;
            if (_db is null) return false;
            if (string.IsNullOrEmpty(key)) return false;
            if (string.IsNullOrEmpty(field)) return false;

            try
            {
                var val = _db.HashGet(key, field);
                if (val.IsNullOrEmpty) return false;

                value = val.ToString();
                return true;
            }
            catch (Exception e)
            {
                Log.Error(this, "{0}", e);
            }
            return false;
        }
        public bool TryHashGetAllString(string key, out Dictionary<string, string> values)
        {
            values = [];
            if (_db is null) return false;
            if (string.IsNullOrEmpty(key)) return false;

            try
            {
                var entries = _db.HashGetAll(key);
                if (entries.Length == 0) return false;

                foreach (var entry in entries)
                {
                    if (entry.Value.IsNullOrEmpty) continue;
                    values[entry.Name.ToString()] = entry.Value.ToString();
                }

                return values.Count > 0;
            }
            catch (Exception e)
            {
                Log.Error(this, "{0}", e);
            }
            return false;
        }

        public bool TrySetHashObject<T>(string key, string field, T value)
        {
            if (_db is null) return false;
            if (string.IsNullOrEmpty(key)) return false;
            if (string.IsNullOrEmpty(field)) return false;

            try
            {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(value, typeof(T), CacheModelJsonContext.Default);
                return _db.HashSet(key, field, bytes);
            }
            catch (Exception e)
            {
                Log.Error(this, "{0}", e);
            }
            return false;
        }
        public bool TryGetHashObject<T>(string key, string field, out T value)
        {
            value = default!;
            if (_db is null) return false;
            if (string.IsNullOrEmpty(key)) return false;
            if (string.IsNullOrEmpty(field)) return false;

            try
            {
                var val = _db.HashGet(key, field);
                if (val.IsNullOrEmpty) return false;

                ReadOnlyMemory<byte> memory = val;
                var obj = JsonSerializer.Deserialize(memory.Span, typeof(T), CacheModelJsonContext.Default);

                if (obj is not T t) return false;

                value = t;
                return true;
            }
            catch (Exception e)
            {
                Log.Error(this, "{0}", e);
            }
            return false;
        }
        public bool TryHashGetAllObject<T>(string key, out Dictionary<string, T> values)
        {
            values = [];
            if (_db is null) return false;
            if (string.IsNullOrEmpty(key)) return false;

            try
            {
                var entries = _db.HashGetAll(key);
                if (entries.Length == 0) return false;

                foreach (var entry in entries)
                {
                    if (entry.Value.IsNullOrEmpty) continue;

                    ReadOnlyMemory<byte> memory = entry.Value;
                    var obj = JsonSerializer.Deserialize(memory.Span, typeof(T), CacheModelJsonContext.Default);

                    if (obj is T t)
                    {
                        values[entry.Name.ToString()] = t;
                    }
                }

                return values.Count > 0;
            }
            catch (Exception e)
            {
                Log.Error(this, "{0}", e);
            }
            return false;
        }

        public bool TryKeyExists(string key, out bool result)
        {
            result = false;
            if (_db is null) return false;
            if (string.IsNullOrEmpty(key)) return false;

            try
            {
                result = _db.KeyExists(key);
                return true;
            }
            catch (Exception e)
            {
                Log.Error(this, "{0}", e);
            }
            return false;
        }
        public bool TryKeyDelete(string key, out bool result)
        {
            result = false;
            if (_db is null) return false;
            if (string.IsNullOrEmpty(key)) return false;

            try
            {
                result = _db.KeyDelete(key);
                return true;
            }
            catch (Exception e)
            {
                Log.Error(this, "{0}", e);
            }
            return false;
        }
        public bool TryKeyExpire(string key, TimeSpan expiry)
        {
            if (_db is null) return false;
            if (string.IsNullOrEmpty(key)) return false;

            try
            {
                return _db.KeyExpire(key, expiry);
            }
            catch (Exception e)
            {
                Log.Error(this, "{0}", e);
            }
            return false;
        }

        public bool TrySetAddValue<T>(string key, in T value) where T : unmanaged
        {
            if (_db is null) return false;
            if (string.IsNullOrEmpty(key)) return false;

            if (!CheckType<T>())
            {
#if DEBUG
                Environment.FailFast($"잘못넣었네 확인해라 T:{typeof(T)}");
#endif
                return false;
            }

            var size = Unsafe.SizeOf<T>();
            var buffer = ArrayPool<byte>.Shared.Rent(size);
            try
            {
                MemoryMarshal.Write(buffer, in value);
                return _db.SetAdd(key, new ReadOnlyMemory<byte>(buffer, 0, size));
            }
            catch (Exception e) { Log.Error(this, "{0}", e); return false; }
            finally { ArrayPool<byte>.Shared.Return(buffer); }
        }
        public bool TrySetRemoveValue<T>(string key, in T value) where T : unmanaged
        {
            if (_db is null) return false;
            if (string.IsNullOrEmpty(key)) return false;

            if (!CheckType<T>())
            {
#if DEBUG
                Environment.FailFast($"잘못넣었네 확인해라 T:{typeof(T)}");
#endif
                return false;
            }

            var size = Unsafe.SizeOf<T>();
            var buffer = ArrayPool<byte>.Shared.Rent(size);
            try
            {
                MemoryMarshal.Write(buffer, in value);
                return _db.SetRemove(key, new ReadOnlyMemory<byte>(buffer, 0, size));
            }
            catch (Exception e) { Log.Error(this, "{0}", e); return false; }
            finally { ArrayPool<byte>.Shared.Return(buffer); }
        }
        public bool TrySetMembersValue<T>(string key, out List<T> values) where T : unmanaged
        {
            values = [];
            if (_db is null) return false;
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
                var members = _db.SetMembers(key);
                var size = Unsafe.SizeOf<T>();

                foreach (var member in members)
                {
                    if (member.IsNullOrEmpty) continue;
                    ReadOnlyMemory<byte> memory = member;
                    if (memory.Length != size) continue;

                    var value = MemoryMarshal.Read<T>(memory.Span);
                    values.Add(value);
                }
                return true;
            }
            catch (Exception e) { Log.Error(this, "{0}", e); return false; }
        }

        public bool TrySetAddString(string key, string value)
        {
            if (_db is null) return false;
            if (string.IsNullOrEmpty(key)) return false;

            try
            {
                return _db.SetAdd(key, value);
            }
            catch (Exception e)
            {
                Log.Error(this, "{0}", e);
            }
            return false;
        }
        public bool TrySetRemoveString(string key, string value)
        {
            if (_db is null) return false;
            if (string.IsNullOrEmpty(key)) return false;

            try
            {
                return _db.SetRemove(key, value);
            }
            catch (Exception e)
            {
                Log.Error(this, "{0}", e);
            }
            return false;
        }
        public bool TrySetMembersString(string key, out List<string> values)
        {
            values = [];
            if (_db is null) return false;
            if (string.IsNullOrEmpty(key)) return false;

            try
            {
                var members = _db.SetMembers(key);
                foreach (var member in members)
                {
                    if (member.HasValue) values.Add(member.ToString());
                }

                return true;
            }
            catch (Exception e)
            {
                Log.Error(this, "{0}", e);
            }
            return false;
        }

        public bool TrySetAddObject<T>(string key, T value)
        {
            if (_db is null) return false;
            if (string.IsNullOrEmpty(key)) return false;

            try
            {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(value, typeof(T), CacheModelJsonContext.Default);
                return _db.SetAdd(key, bytes);
            }
            catch (Exception e)
            {
                Log.Error(this, "{0}", e);
                return false;
            }
        }
        public bool TrySetRemoveObject<T>(string key, T value)
        {
            if (_db is null) return false;
            if (string.IsNullOrEmpty(key)) return false;

            try
            {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(value, typeof(T), CacheModelJsonContext.Default);
                return _db.SetRemove(key, bytes);
            }
            catch (Exception e)
            {
                Log.Error(this, "{0}", e);
            }
            return false;
        }
        public bool TrySetMembersObject<T>(string key, out List<T> values)
        {
            values = [];
            if (_db is null) return false;
            if (string.IsNullOrEmpty(key)) return false;

            try
            {
                var members = _db.SetMembers(key);

                foreach (var member in members)
                {
                    if (member.IsNullOrEmpty) continue;

                    var obj = JsonSerializer.Deserialize(member!, typeof(T), CacheModelJsonContext.Default);
                    if (obj is T t)
                    {
                        values.Add(t);
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                Log.Error(this, "{0}", e);
                return false;
            }
        }

        public bool TryListRightPushValue<T>(string key, in T value) where T : unmanaged
        {
            if (_db is null) return false;
            if (string.IsNullOrEmpty(key)) return false;

            if (!CheckType<T>())
            {
#if DEBUG
                Environment.FailFast($"잘못넣었네 확인해라 T:{typeof(T)}");
#endif
                return false;
            }

            var size = Unsafe.SizeOf<T>();
            var buffer = ArrayPool<byte>.Shared.Rent(size);
            try
            {
                MemoryMarshal.Write(buffer, in value);
                _db.ListRightPush(key, new ReadOnlyMemory<byte>(buffer, 0, size));
                return true;
            }
            catch (Exception e)
            {
                Log.Error(this, "{0}", e);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
            return false;
        }
        public bool TryListRangeValue<T>(string key, out List<T> values, int start = 0, int stop = -1) where T : unmanaged
        {
            values = [];
            if (_db is null) return false;
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
                // Redis에서 범위 조회 (start=0, stop=-1이면 전체 조회)
                var list = _db.ListRange(key, start, stop);
                if (list.Length == 0) return true;

                var size = Unsafe.SizeOf<T>();
                values.EnsureCapacity(list.Length);

                foreach (var item in list)
                {
                    if (item.IsNullOrEmpty) continue;
                    ReadOnlyMemory<byte> memory = item;

                    if (memory.Length != size) continue;

                    var value = MemoryMarshal.Read<T>(memory.Span);
                    values.Add(value);
                }
                return true;
            }
            catch (Exception e)
            {
                Log.Error(this, "{0}", e);
            }
            return false;
        }

        public bool TryListRightPushString(string key, string value)
        {
            if (_db is null) return false;
            if (string.IsNullOrEmpty(key)) return false;

            try
            {
                _db.ListRightPush(key, value);
                return true;
            }
            catch (Exception e)
            {
                Log.Error(this, "{0}", e);
            }
            return false;
        }
        public bool TryListRangeString(string key, out List<string> values, int start = 0, int stop = -1)
        {
            values = [];
            if (_db is null) return false;
            if (string.IsNullOrEmpty(key)) return false;

            try
            {
                var list = _db.ListRange(key, start, stop);
                values.EnsureCapacity(list.Length);

                foreach (var item in list)
                {
                    if (!item.HasValue) continue;

                    values.Add(item.ToString());
                }
                return true;
            }
            catch (Exception e)
            {
                Log.Error(this, "{0}", e);
            }
            return false;
        }

        public bool TryListRightPushObject<T>(string key, T value)
        {
            if (_db is null) return false;
            if (string.IsNullOrEmpty(key)) return false;

            try
            {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(value, typeof(T), CacheModelJsonContext.Default);
                _db.ListRightPush(key, bytes);
                return true;
            }
            catch (Exception e)
            {
                Log.Error(this, "{0}", e);
            }
            return false;
        }
        public bool TryListRangeObject<T>(string key, out List<T> values, int start = 0, int stop = -1)
        {
            values = [];
            if (_db is null) return false;
            if (string.IsNullOrEmpty(key)) return false;

            try
            {
                var list = _db.ListRange(key, start, stop);
                values.EnsureCapacity(list.Length);

                foreach (var item in list)
                {
                    if (item.IsNullOrEmpty) continue;

                    var obj = JsonSerializer.Deserialize(item!, typeof(T), CacheModelJsonContext.Default);
                    if (obj is T t)
                    {
                        values.Add(t);
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                Log.Error(this, "{0}", e);
            }
            return false;
        }
    }
}