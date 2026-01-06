namespace GameServer.Common
{
    public interface IRuntimeId
    {
        long RuntimeId { get; init; }
    }


    public static class GameObjRuntimeIdGen
    {
        private static long _current = 0;

        public static long Generate()
        {
            return Interlocked.Increment(ref _current);
        }
    }

    public static class SessionRuntimeIdGen
    {
        private static long _current = 0;

        public static long Generate()
        {
            return Interlocked.Increment(ref _current);
        }
    }
}