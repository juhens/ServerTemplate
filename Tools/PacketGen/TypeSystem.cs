namespace PacketGen
{
    public enum TypeKind
    {
        Primitive,
        String,
        DateTime,
        Guid,
        Struct,
        Enum
    }

    public class ProtoTypeInfo
    {
        public string Name { get; }
        public int WireSize { get; }
        public TypeKind Kind { get; }

        public ProtoTypeInfo(string name, int wireSize, TypeKind kind)
        {
            Name = name;
            WireSize = wireSize;
            Kind = kind;
        }
    }

    public class TypeRegistry
    {
        private readonly Dictionary<string, ProtoTypeInfo> _registry = new Dictionary<string, ProtoTypeInfo>();

        public TypeRegistry()
        {
            Register("bool", 1, TypeKind.Primitive);
            Register("int8", 1, TypeKind.Primitive);
            Register("uint8", 1, TypeKind.Primitive);

            Register("int16", 2, TypeKind.Primitive);
            Register("uint16", 2, TypeKind.Primitive);

            Register("int32", 4, TypeKind.Primitive);
            Register("uint32", 4, TypeKind.Primitive);
            Register("float32", 4, TypeKind.Primitive);

            Register("int64", 8, TypeKind.Primitive);
            Register("uint64", 8, TypeKind.Primitive);
            Register("float64", 8, TypeKind.Primitive);
            Register("date", 8, TypeKind.DateTime);

            Register("string", 0, TypeKind.String);
            Register("guid", 16, TypeKind.Guid);
        }

        private void Register(string name, int size, TypeKind kind)
        {
            _registry[name] = new ProtoTypeInfo(name, size, kind);
        }

        public ProtoTypeInfo? Get(string name)
        {
            return _registry.GetValueOrDefault(name);
        }

        public bool IsPrimitive(string name)
        {
            return _registry.ContainsKey(name);
        }
    }
}