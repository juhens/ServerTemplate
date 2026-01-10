namespace GameServer.Cache
{
    public interface ICacheManager
    {
        public void Connect(string connectionString = "localhost:6379", int delay = 5000);

        public bool TrySetValue<T>(string key, in T value, TimeSpan? expiry = null) where T : unmanaged;
        public bool TryGetValue<T>(string key, out T value) where T : unmanaged;

        public bool TrySetString(string key, string value, TimeSpan? expiry = null);
        public bool TryGetString(string key, out string value);

        public bool TrySetObject<T>(string key, T value, TimeSpan? expiry = null);
        public bool TryGetObject<T>(string key, out T value);

        public bool TrySetHashValue<T>(string key, string field, in T value) where T : unmanaged;
        public bool TryGetHashValue<T>(string key, string field, out T value) where T : unmanaged;
        public bool TryHashGetAllValue<T>(string key, out Dictionary<string, T> values) where T : unmanaged;

        public bool TrySetHashString(string key, string field, string value);
        public bool TryGetHashString(string key, string field, out string value);
        public bool TryHashGetAllString(string key, out Dictionary<string, string> values);

        public bool TrySetHashObject<T>(string key, string field, T value);
        public bool TryGetHashObject<T>(string key, string field, out T value);
        public bool TryHashGetAllObject<T>(string key, out Dictionary<string, T> values);

        public bool TryKeyExists(string key, out bool result);
        public bool TryKeyDelete(string key, out bool result);
        public bool TryKeyExpire(string key, TimeSpan expiry);

        public bool TrySetAddValue<T>(string key, in T value) where T : unmanaged;
        public bool TrySetRemoveValue<T>(string key, in T value) where T : unmanaged;
        public bool TrySetMembersValue<T>(string key, out List<T> values) where T : unmanaged;

        public bool TrySetAddString(string key, string value);
        public bool TrySetRemoveString(string key, string value);
        public bool TrySetMembersString(string key, out List<string> values);

        public bool TrySetAddObject<T>(string key, T value);
        public bool TrySetRemoveObject<T>(string key, T value);
        public bool TrySetMembersObject<T>(string key, out List<T> values);

        public bool TryListRightPushValue<T>(string key, in T value) where T : unmanaged;
        public bool TryListRangeValue<T>(string key, out List<T> values, int start = 0, int stop = -1) where T : unmanaged;

        public bool TryListRightPushString(string key, string value);
        public bool TryListRangeString(string key, out List<string> values, int start = 0, int stop = -1);

        public bool TryListRightPushObject<T>(string key, T value);
        public bool TryListRangeObject<T>(string key, out List<T> values, int start = 0, int stop = -1);
    }
}
