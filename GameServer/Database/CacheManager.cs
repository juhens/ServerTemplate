using GameServer.Database.Entities.Redis;
using GameServer.Game.Dto;
using ServerCore;
using StackExchange.Redis;
using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace GameServer.Database
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

    public interface ICacheManager
    {
        void Connect(string connectionString, int delay);

        public bool TrySetValue<T>(string key, in T t, TimeSpan? expiry = null) where T : unmanaged;
        public bool TryGetValue<T>(string key, out T t) where T : unmanaged;

        public bool TrySetString(string key, string value, TimeSpan? expiry = null);
        public bool TryGetString(string key, out string value);

        public bool TrySetObject<T>(string key, T t, TimeSpan? expiry = null);
        public bool TryGetObject<T>(string key, out T value);

        public bool TryKeyExists(string key, out bool result);
        public bool TryDelete(string key, out bool result);
    }


    public class RedisManager : ICacheManager
    {
        private RedisManager() {}
        public static RedisManager Instance { get; } = new RedisManager();

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
                   || typeof(IPacked).IsAssignableFrom(type);
        }

        public bool TrySetValue<T>(string key, in T t, TimeSpan? expiry = null) where T : unmanaged
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
                MemoryMarshal.Write(buffer, in t);
                var memory = new ReadOnlyMemory<byte>(buffer, 0, size);
                return _db.StringSet(key, memory, expiry, When.Always);
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        public bool TryGetValue<T>(string key, out T t) where T : unmanaged
        {
            t = default;
            if (_db is null) return false;
            if (string.IsNullOrEmpty(key)) return false;

            if (!CheckType<T>())
            {
#if DEBUG
                Environment.FailFast("잘못넣었네 확인해라");
#endif
                return false;
            }

            try
            {
                var val = _db.StringGet(key);
                if (val.IsNullOrEmpty) return false;

                ReadOnlyMemory<byte> memory = val;

                if (memory.Length != Unsafe.SizeOf<T>()) return false;
                ref var p = ref MemoryMarshal.GetReference(memory.Span);
                t = Unsafe.ReadUnaligned<T>(ref p);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
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
                Console.WriteLine(e);
                return false;
            }
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
                Console.WriteLine(e);
            }

            return false;
        }

        public bool TrySetObject<T>(string key, T t, TimeSpan? expiry = null)
        {
            if (_db is null) return false;
            if (string.IsNullOrEmpty(key)) return false;

            try
            {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(t);

                return _db.StringSet(key, bytes, expiry, When.Always);
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Redis SetObject Error] {e.Message}");
                return false;
            }
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
                var result = JsonSerializer.Deserialize<T>(memory.Span);

                if (result is null) return false;

                value = result;
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Redis GetObject Error] {e.Message}");
            }

            return false;
        }

        public bool TryKeyExists(string key, out bool result)
        {
            result = false;
            if (_db is null) return false;
            try
            {
                result = _db.KeyExists(key);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }
        public bool TryDelete(string key, out bool result)
        {
            result = false;
            if (_db is null) return false;
            try
            {
                result = _db.KeyDelete(key);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }
    }

    public class MockRedisManager : ICacheManager
    {
        private struct MockData
        {
            public required byte[] Payload;
            public required DateTime? ExpiredDateTime;
        }

        private readonly ConcurrentDictionary<string, MockData> _storage = new();

        private MockRedisManager() { }
        public static MockRedisManager Instance { get; } = new MockRedisManager();

        public void Connect(string connectionString = "localhost:6379", int delay = 5000)
        {
            Console.WriteLine($"MockRedis 연결 성공!");
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
                    || typeof(IPacked).IsAssignableFrom(type);
        }


        private bool TryGetValidData(string key, out MockData data)
        {
            data = default;

            if (!_storage.TryGetValue(key, out var storedData))
            {
                return false;
            }

            if (storedData.ExpiredDateTime.HasValue && storedData.ExpiredDateTime.Value < DateTime.Now)
            {
                _storage.TryRemove(key, out _);
                return false;
            }

            data = storedData;
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

            var size = Unsafe.SizeOf<T>();
            var bytes = new byte[size];
            Unsafe.WriteUnaligned(ref bytes[0], t);

            var expiredDateTime = expiry.HasValue ? DateTime.Now + expiry : null;

            _storage[key] = new MockData { Payload = bytes, ExpiredDateTime = expiredDateTime };
            return true;
        }
        public bool TryGetValue<T>(string key, out T t) where T : unmanaged
        {
            t = default;
            if (string.IsNullOrEmpty(key)) return false;
            if (!CheckType<T>()) return false;

            if (TryGetValidData(key, out var data))
            {
                if (data.Payload.Length != Unsafe.SizeOf<T>()) return false;

                t = Unsafe.ReadUnaligned<T>(ref data.Payload[0]);
                return true;
            }

            return false;
        }

        public bool TrySetString(string key, string value, TimeSpan? expiry = null)
        {
            if (string.IsNullOrEmpty(key)) return false;

            var bytes = System.Text.Encoding.UTF8.GetBytes(value);
            var expiredDateTime = expiry.HasValue ? DateTime.Now + expiry : null;

            _storage[key] = new MockData { Payload = bytes, ExpiredDateTime = expiredDateTime };
            return true;
        }
        public bool TryGetString(string key, out string value)
        {
            value = null!;
            if (TryGetValidData(key, out var data))
            {
                value = System.Text.Encoding.UTF8.GetString(data.Payload);
                return true;
            }
            return false;
        }

        public bool TrySetObject<T>(string key, T t, TimeSpan? expiry = null)
        {
            if (string.IsNullOrEmpty(key)) return false;

            try
            {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(t);
                var expiredDateTime = expiry.HasValue ? DateTime.Now + expiry : null;

                _storage[key] = new MockData { Payload = bytes, ExpiredDateTime = expiredDateTime };
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Mock Error] SetObject: {e.Message}");
                return false;
            }
        }
        public bool TryGetObject<T>(string key, out T value)
        {
            value = default!;
            if (TryGetValidData(key, out var data))
            {
                try
                {
                    var result = JsonSerializer.Deserialize<T>(data.Payload);
                    if (result != null)
                    {
                        value = result;
                        return true;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[Mock Error] GetObject: {e.Message}");
                }
            }
            return false;
        }

        public bool TryKeyExists(string key, out bool result)
        {
            result = TryGetValidData(key, out _);
            return true;
        }

        public bool TryDelete(string key, out bool result)
        {
            result = _storage.TryRemove(key, out _);
            return true;
        }

        public void Clear()
        {
            _storage.Clear();
        }
    }
}