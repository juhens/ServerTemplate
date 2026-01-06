namespace PacketGen
{
    public class Parser
    {
        private readonly List<Token> _tokens;
        private int _position;

        // Enum의 기반 타입(Underlying Type)으로 사용할 수 있는 정수형 토큰들
        private static readonly HashSet<TokenType> IntegerTypeTokens = new()
        {
            TokenType.TypeInt8, TokenType.TypeInt16, TokenType.TypeInt32, TokenType.TypeInt64,
            TokenType.TypeUInt8, TokenType.TypeUInt16, TokenType.TypeUInt32, TokenType.TypeUInt64
        };

        // 구조체 필드(Struct Field)로 사용할 수 있는 기본 타입 토큰들
        private static readonly HashSet<TokenType> FieldTypeTokens = new()
        {
            // 정수형
            TokenType.TypeInt8, TokenType.TypeInt16, TokenType.TypeInt32, TokenType.TypeInt64,
            TokenType.TypeUInt8, TokenType.TypeUInt16, TokenType.TypeUInt32, TokenType.TypeUInt64,
            // 실수형
            TokenType.TypeFloat32, TokenType.TypeFloat64,
            // 기타 기본 타입
            TokenType.TypeBool, TokenType.TypeString, TokenType.TypeDate, TokenType.TypeGuid
        };

        public Parser(List<Token> tokens)
        {
            _tokens = tokens;
            _position = 0;
        }

        private Token Current
        {
            get
            {
                if (_position >= _tokens.Count)
                    return _tokens[^1];
                return _tokens[_position];
            }
        }

        private Token Advance()
        {
            var currentToken = Current;
            if (currentToken.Type != TokenType.EndOfFile)
            {
                _position++;
            }
            return currentToken;
        }

        private Token Consume(TokenType expectedType, string errorMessage)
        {
            if (Current.Type == expectedType)
            {
                return Advance();
            }

            throw new ParseException(
                $"{errorMessage} (발견된 토큰: '{Current.Value}', 타입: {Current.Type})",
                Current.Line,
                Current.Column
            );
        }

        public List<Definition> Parse()
        {
            var definitions = new List<Definition>();

            while (Current.Type != TokenType.EndOfFile)
            {
                var attributes = ParseAttributes();

                switch (Current.Type)
                {
                    case TokenType.KeywordEnum:
                        definitions.Add(ParseEnum(attributes));
                        break;

                    case TokenType.KeywordStruct:
                        definitions.Add(ParseStruct(attributes));
                        break;

                    default:
                        throw new ParseException(
                            $"정의는 'enum' 또는 'struct'로 시작해야 합니다. (발견된 토큰: '{Current.Value}')",
                            Current.Line,
                            Current.Column
                        );
                }
            }

            return definitions;
        }

        private List<(string Name, string Value)> ParseAttributes()
        {
            var attributes = new List<(string Name, string Value)>();
            while (Current.Type == TokenType.Attribute)
            {
                var attrName = Consume(TokenType.Attribute, "").Value;
                var attrValue = "";

                if (Current.Type == TokenType.Colon)
                {
                    Advance();
                    attrValue = Consume(TokenType.Identifier, "어트리뷰트 값이 필요합니다.").Value;
                }

                attributes.Add((attrName, attrValue));
            }
            return attributes;
        }

        private bool IsIntegerType(TokenType type) => IntegerTypeTokens.Contains(type);

        private EnumDefinition ParseEnum(List<(string Name, string Value)> attributes)
        {
            Consume(TokenType.KeywordEnum, "'enum' 키워드가 필요합니다.");
            var nameToken = Consume(TokenType.Identifier, "Enum 이름이 필요합니다.");

            string? underlyingType = null;
            if (Current.Type == TokenType.Colon)
            {
                Advance();
                if (IsIntegerType(Current.Type))
                {
                    underlyingType = Advance().Value;
                }
                else
                {
                    throw new ParseException(
                        "Enum의 기반 타입은 정수형(int32, uint16 등)이어야 합니다.",
                        Current.Line,
                        Current.Column
                    );
                }
            }

            var enumDef = new EnumDefinition(nameToken.Value)
            {
                Attributes = attributes.Select(a => a.Name).ToList(),
                Line = nameToken.Line,
                Column = nameToken.Column
            };

            if (underlyingType != null) enumDef.UnderlyingType = underlyingType;

            Consume(TokenType.OpenBrace, "Enum 시작 브레이스 '{' 가 필요합니다.");

            long currentEnumValue = 0;

            while (Current.Type != TokenType.CloseBrace && Current.Type != TokenType.EndOfFile)
            {
                var memberName = Consume(TokenType.Identifier, "Enum 멤버 이름이 필요합니다.");
                string memberValueStr;
                bool isExplicit;

                if (Current.Type == TokenType.Equals)
                {
                    Advance();
                    var valToken = Consume(TokenType.NumberLiteral, "멤버 값(숫자)이 필요합니다.");
                    memberValueStr = valToken.Value;
                    isExplicit = true;

                    if (long.TryParse(memberValueStr, out var parsedVal))
                    {
                        currentEnumValue = parsedVal + 1;
                    }
                }
                else
                {
                    memberValueStr = currentEnumValue.ToString();
                    currentEnumValue++;
                    isExplicit = false;
                }

                var member = new EnumMember(memberName.Value, memberValueStr)
                {
                    IsExplicit = isExplicit,
                    Line = memberName.Line,
                    Column = memberName.Column
                };

                enumDef.Members.Add(member);

                if (Current.Type == TokenType.Comma)
                {
                    Advance();
                }
            }

            Consume(TokenType.CloseBrace, "Enum 종료 브레이스 '}' 가 필요합니다.");

            return enumDef;
        }

        private StructDefinition ParseStruct(List<(string Name, string Value)> attributes)
        {
            Consume(TokenType.KeywordStruct, "'struct' 키워드가 필요합니다.");
            var nameToken = Consume(TokenType.Identifier, "Struct 이름이 필요합니다.");

            var packetAttr = attributes.FirstOrDefault(a => a.Name == "packet");

            StructDefinition structDef;

            // PacketDefinition인지 일반 StructDefinition인지 결정
            if (!string.IsNullOrEmpty(packetAttr.Name))
            {
                var pDef = new PacketDefinition(nameToken.Value);

                if (string.IsNullOrEmpty(packetAttr.Value))
                {
                    pDef.Scope = PacketScope.Both;
                }
                else
                {
                    switch (packetAttr.Value.ToLower())
                    {
                        case "server": pDef.Scope = PacketScope.Server; break;
                        case "client": pDef.Scope = PacketScope.Client; break;
                        case "both": pDef.Scope = PacketScope.Both; break;
                        default:
                            throw new ParseException(
                                $"알 수 없는 패킷 범위입니다: '{packetAttr.Value}' (server, client, both 중 하나여야 합니다)",
                                Current.Line,
                                Current.Column
                            );
                    }
                }
                structDef = pDef;
            }
            else
            {
                structDef = new StructDefinition(nameToken.Value);
            }

            structDef.Attributes = attributes.Select(a => a.Name).ToList();
            structDef.Line = nameToken.Line;
            structDef.Column = nameToken.Column;

            Consume(TokenType.OpenBrace, "Struct 시작 브레이스 '{' 가 필요합니다.");

            while (Current.Type != TokenType.CloseBrace && Current.Type != TokenType.EndOfFile)
            {
                structDef.Fields.Add(ParseStructField());
            }

            Consume(TokenType.CloseBrace, "Struct 종료 브레이스 '}' 가 필요합니다.");

            return structDef;
        }

        private bool IsFieldType(TokenType type) => FieldTypeTokens.Contains(type);

        private StructField ParseStructField()
        {
            string typeName;

            if (IsFieldType(Current.Type))
            {
                typeName = Advance().Value;
            }
            else if (Current.Type == TokenType.Identifier)
            {
                typeName = Advance().Value;
            }
            else
            {
                throw new ParseException(
                    "필드 타입(int32, string 등)이나 구조체/열거형 이름이 와야 합니다.",
                    Current.Line,
                    Current.Column
                );
            }

            var isArray = false;
            if (Current.Type == TokenType.OpenBracket)
            {
                Consume(TokenType.OpenBracket, "");
                Consume(TokenType.CloseBracket, "배열을 닫는 ']'가 필요합니다.");
                isArray = true;
            }

            var nameToken = Consume(TokenType.Identifier, "필드 이름이 필요합니다.");
            Consume(TokenType.Semicolon, "필드 정의 끝에 세미콜론 ';'이 필요합니다.");

            return new StructField(typeName, nameToken.Value, isArray)
            {
                Line = nameToken.Line,
                Column = nameToken.Column
            };
        }
    }
}