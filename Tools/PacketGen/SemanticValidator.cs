namespace PacketGen
{
    public class SemanticValidator
    {
        private readonly TypeRegistry _typeRegistry;

        public SemanticValidator(TypeRegistry typeRegistry)
        {
            _typeRegistry = typeRegistry;
        }

        public void Validate(List<Definition> definitions)
        {
            // 1. 타입 이름 중복 검사
            var definedTypeNames = new HashSet<string>();
            foreach (var def in definitions)
            {
                if (!definedTypeNames.Add(def.Name))
                {
                    throw new SemanticException(
                        $"타입 이름 '{def.Name}'이(가) 이미 정의되어 있습니다.",
                        def.Line, def.Column);
                }
            }

            // 2. 내부 멤버 검증
            foreach (var def in definitions)
            {
                switch (def)
                {
                    case StructDefinition structDef:
                        ValidateStruct(structDef, definedTypeNames);
                        break;
                    case EnumDefinition enumDef:
                        ValidateEnum(enumDef);
                        break;
                }
            }
        }

        private void ValidateStruct(StructDefinition structDef, HashSet<string> definedTypeNames)
        {
            // [Struct] 어트리뷰트 검사
            if (structDef.Attributes.Contains("flags"))
            {
                throw new SemanticException(
                    $"구조체 '{structDef.Name}'에는 '@flags' 어트리뷰트를 사용할 수 없습니다.",
                    structDef.Line, structDef.Column);
            }

            // [Struct] 필드 검사
            var fieldNames = new HashSet<string>();

            foreach (var field in structDef.Fields)
            {
                // 필드 이름 중복 검사
                if (!fieldNames.Add(field.Name))
                {
                    throw new SemanticException(
                        $"구조체 '{structDef.Name}'에 필드 이름 '{field.Name}'이(가) 중복되어 정의되었습니다.",
                        field.Line, field.Column);
                }

                // 타입 존재 여부 검사
                if (_typeRegistry.IsPrimitive(field.TypeName)) continue;

                if (!definedTypeNames.Contains(field.TypeName))
                {
                    throw new SemanticException(
                        $"구조체 '{structDef.Name}'의 필드 '{field.Name}'에서 사용된 타입 '{field.TypeName}'은(는) 정의되지 않았습니다.",
                        field.Line, field.Column);
                }
            }
        }

        private void ValidateEnum(EnumDefinition enumDef)
        {
            // [Enum] 어트리뷰트 검사
            if (enumDef.Attributes.Contains("packet"))
            {
                throw new SemanticException(
                    $"열거형 '{enumDef.Name}'에는 '@packet' 어트리뷰트를 사용할 수 없습니다.",
                    enumDef.Line, enumDef.Column);
            }

            // [Enum] 멤버 이름 중복 검사
            var memberNames = new HashSet<string>();

            foreach (var member in enumDef.Members)
            {
                if (!memberNames.Add(member.Name))
                {
                    throw new SemanticException(
                        $"열거형 '{enumDef.Name}'에 멤버 이름 '{member.Name}'이(가) 중복되어 정의되었습니다.",
                        member.Line, member.Column);
                }
            }
        }
    }
}