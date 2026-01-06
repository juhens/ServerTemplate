using System;

namespace ServerCore.Packet
{
    // 임시로 만든거 패킷정책을 둘지 Job단위 정책을 둘지 둘다 둘지 못정함
    public enum ThrottlePolicyType
    {
        None = 0,
        Social = 1,     
        Action = 2,
        System = 3, 
        Critical = 4,   
        Debug = 99 
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public class PacketPolicyAttribute : Attribute
    {
        public ThrottlePolicyType Type { get; }
        public int Limit { get; }
        public int Burst { get; set; }
        public int CooldownMs { get; set; }

        public PacketPolicyAttribute(ThrottlePolicyType type, int limit)
        {
            Type = type;
            Limit = limit;
            Burst = 0;
            CooldownMs = 0;
        }
    }
}