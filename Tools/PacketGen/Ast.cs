namespace PacketGen
{
    public interface ISourceLocatable
    {
        int Line { get; set; }
        int Column { get; set; }
    }
    public abstract class Definition : ISourceLocatable
    {
        public string Name { get; set; }
        public List<string> Attributes { get; set; } = new List<string>();
        public int Line { get; set; }
        public int Column { get; set; }

        protected Definition(string name) { Name = name; }
    }

    // 2. Enum 정의
    public class EnumDefinition : Definition
    {
        public string UnderlyingType { get; set; } = "int32";
        public List<EnumMember> Members { get; set; } = new List<EnumMember>();
        public EnumDefinition(string name) : base(name) { }
    }

    public class EnumMember : ISourceLocatable
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public bool IsExplicit { get; set; } = false;
        public int Line { get; set; }
        public int Column { get; set; }

        public EnumMember(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }


    public enum PacketScope
    {
        Both,
        Server,
        Client
    }

    // Struct 정의
    public class StructDefinition : Definition
    {
        public List<StructField> Fields { get; set; } = new List<StructField>();
        public StructDefinition(string name) : base(name) { }
    }

    public class PacketDefinition : StructDefinition
    {
        public PacketScope Scope { get; set; } = PacketScope.Both;
        public PacketDefinition(string name) : base(name) { }
    }

    // Struct 필드
    public class StructField : ISourceLocatable
    {
        public string TypeName { get; set; }
        public string Name { get; set; }
        public bool IsArray { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }

        public StructField(string typeName, string name, bool isArray)
        {
            TypeName = typeName;
            Name = name;
            IsArray = isArray;
        }
    }


}
