namespace PacketGen.CodeGenerator.CSharp
{
    public class CSharpTypeTraits
    {
        public string CsTypeName { get; }
        public string WriteMethod { get; }
        public string ReadMethod { get; }
        public string? CastType { get; }
        public int? SizeExpr { get; }
        public string? HeaderParseExpr { get; }

        public CSharpTypeTraits(
            string csTypeName,
            string writeMethod,
            string readMethod,
            string? castType = null,
            int? sizeExpr = null,
            string? headerParseExpr = null
        )
        {
            CsTypeName = csTypeName;
            WriteMethod = writeMethod;
            ReadMethod = readMethod;
            CastType = castType;
            SizeExpr = sizeExpr;
            HeaderParseExpr = headerParseExpr;
        }
    }

    public class CSharpTypeProvider
    {
        private readonly List<Definition> _definitions;
        private readonly TypeRegistry _typeRegistry;
        private readonly Dictionary<string, (ProtoTypeInfo Proto, CSharpTypeTraits Cs)> _cache = new Dictionary<string, (ProtoTypeInfo Proto, CSharpTypeTraits Cs)>();

        private const string Arg = "buffer.Array!, buffer.Offset + count";

        private static readonly Dictionary<string, CSharpTypeTraits> PrimitiveTraitsMap = new Dictionary<string, CSharpTypeTraits>
        {
            // 1바이트
            { "bool",    new CSharpTypeTraits("bool",     "WriteBool",    "ReadBool",    sizeExpr: sizeof(bool),   headerParseExpr: "ERROR") },
            { "int8",    new CSharpTypeTraits("sbyte",    "WriteInt8",    "ReadInt8",    sizeExpr: sizeof(sbyte),  headerParseExpr: $"(sbyte)buffer.Array![buffer.Offset + count]") },
            { "uint8",   new CSharpTypeTraits("byte",     "WriteUInt8",   "ReadUInt8",   sizeExpr: sizeof(byte),   headerParseExpr: $"buffer.Array![buffer.Offset + count]") },
            
            // 2바이트
            { "int16",   new CSharpTypeTraits("short",    "WriteInt16",   "ReadInt16",   sizeExpr: sizeof(short),  headerParseExpr: $"BitConverter.ToInt16({Arg})") },
            { "uint16",  new CSharpTypeTraits("ushort",   "WriteUInt16",  "ReadUInt16",  sizeExpr: sizeof(ushort), headerParseExpr: $"BitConverter.ToUInt16({Arg})") },
            
            // 4바이트
            { "int32",   new CSharpTypeTraits("int",      "WriteInt32",   "ReadInt32",   sizeExpr: sizeof(int),    headerParseExpr: $"BitConverter.ToInt32({Arg})") },
            { "uint32",  new CSharpTypeTraits("uint",     "WriteUInt32",  "ReadUInt32",  sizeExpr: sizeof(uint),   headerParseExpr: $"BitConverter.ToUInt32({Arg})") },
            { "float32", new CSharpTypeTraits("float",    "WriteFloat32", "ReadFloat32", sizeExpr: sizeof(float),  headerParseExpr: "ERROR") },

            // 8바이트
            { "int64",   new CSharpTypeTraits("long",     "WriteInt64",   "ReadInt64",   sizeExpr: sizeof(long),   headerParseExpr: $"BitConverter.ToInt64({Arg})") },
            { "uint64",  new CSharpTypeTraits("ulong",    "WriteUInt64",  "ReadUInt64",  sizeExpr: sizeof(ulong),  headerParseExpr: $"BitConverter.ToUInt64({Arg})") },
            { "float64", new CSharpTypeTraits("double",   "WriteFloat64", "ReadFloat64", sizeExpr: sizeof(double), headerParseExpr: "ERROR") },
            { "date",    new CSharpTypeTraits("DateTime", "WriteDate",    "ReadDate",    sizeExpr: 8,              headerParseExpr: "ERROR") },
            
            // 16바이트
            { "guid",    new CSharpTypeTraits("Guid",     "WriteGuid",    "ReadGuid",    sizeExpr: 16,             headerParseExpr: "ERROR") },
            
            // 가변
            { "string",  new CSharpTypeTraits("string",   "WriteString",  "ReadString",  sizeExpr: null,           headerParseExpr: "ERROR") }
        };

        public CSharpTypeProvider(List<Definition> definitions, TypeRegistry typeRegistry)
        {
            _definitions = definitions;
            _typeRegistry = typeRegistry;

            foreach (var kvp in PrimitiveTraitsMap)
            {
                var proto = _typeRegistry.Get(kvp.Key);
                if (proto != null) _cache[kvp.Key] = (proto, kvp.Value);
            }
        }

        public (ProtoTypeInfo Proto, CSharpTypeTraits Cs) GetTypeContext(string typeName)
        {
            if (_cache.TryGetValue(typeName, out var cachedCtx)) return cachedCtx;
            var result = ResolveUserType(typeName);
            _cache[typeName] = result;
            return result;
        }

        private (ProtoTypeInfo Proto, CSharpTypeTraits Cs) ResolveUserType(string typeName)
        {
            var def = _definitions.FirstOrDefault(d => d.Name == typeName);

            if (def is EnumDefinition eDef)
            {
                var underlyingCtx = GetTypeContext(eDef.UnderlyingType);
                var enumTraits = new CSharpTypeTraits(
                    typeName,
                    underlyingCtx.Cs.WriteMethod,
                    underlyingCtx.Cs.ReadMethod,
                    underlyingCtx.Cs.CsTypeName, // CastType
                    underlyingCtx.Cs.SizeExpr,
                    underlyingCtx.Cs.HeaderParseExpr
                );

                return (new ProtoTypeInfo(typeName, underlyingCtx.Proto.WireSize, TypeKind.Enum), enumTraits);
            }

            if (def is StructDefinition)
            {
                return (
                    new ProtoTypeInfo(typeName, 0, TypeKind.Struct),
                    new CSharpTypeTraits(typeName, "ERROR", "ERROR", castType: null, sizeExpr: null, headerParseExpr: "ERROR")
                );
            }

            return (
                new ProtoTypeInfo("ERROR", 0, TypeKind.Primitive),
                new CSharpTypeTraits("ERROR", "ERROR", "ERROR", headerParseExpr: "ERROR")
            );
        }
    }
}