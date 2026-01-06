using System.Text;

namespace PacketGen
{
    public enum TokenType
    {
        EndOfFile, Unknown, Identifier,

        OpenBrace, CloseBrace, OpenBracket, CloseBracket, Equals, Semicolon, Colon, Comma,

        KeywordStruct, KeywordEnum,

        TypeBool,
        TypeInt8, TypeInt16, TypeInt32, TypeInt64,
        TypeUInt8, TypeUInt16, TypeUInt32, TypeUInt64,
        TypeFloat32, TypeFloat64,
        TypeString, TypeDate, TypeGuid,

        NumberLiteral,

        Attribute,
    }

    public class Token
    {
        public TokenType Type { get; }
        public string Value { get; }

        // [New] 위치 추적 속성
        public int Line { get; }
        public int Column { get; }

        public Token(TokenType type, string value, int line, int column)
        {
            Type = type;
            Value = value;
            Line = line;
            Column = column;
        }

        public override string ToString() => $"Line {Line}:{Column} [{Type}] '{Value}'";
    }

    public class Lexer
    {
        private readonly string _input;
        private int _position;

        private int _line;
        private int _column;

        private readonly StringBuilder _sb = new();

        private static readonly Dictionary<string, TokenType> Keywords = new()
        {
            { "enum", TokenType.KeywordEnum },
            { "struct", TokenType.KeywordStruct },
            { "bool", TokenType.TypeBool },
            { "int8", TokenType.TypeInt8 }, { "int16", TokenType.TypeInt16 }, { "int32", TokenType.TypeInt32 }, { "int64", TokenType.TypeInt64 },
            { "uint8", TokenType.TypeUInt8 }, { "uint16", TokenType.TypeUInt16 }, { "uint32", TokenType.TypeUInt32 }, { "uint64", TokenType.TypeUInt64 },
            { "float32", TokenType.TypeFloat32 }, { "float64", TokenType.TypeFloat64 },
            { "string", TokenType.TypeString }, { "date", TokenType.TypeDate }, { "guid", TokenType.TypeGuid },
        };

        public Lexer(string input)
        {
            _input = input;
            _position = 0;
            _line = 1;   // 줄 번호는 1부터 시작
            _column = 1; // 열 번호는 1부터 시작
        }

        private char CurrentChar => _position >= _input.Length ? '\0' : _input[_position];

        private char PeekNextChar()
        {
            return _position + 1 >= _input.Length ? '\0' : _input[_position + 1];
        }

        private void Advance()
        {
            if (CurrentChar == '\0') return;

            if (CurrentChar == '\n')
            {
                _line++;
                _column = 1;
            }
            else
            {
                _column++;
            }
            _position++;
        }

        public List<Token> GetTokens()
        {
            var tokens = new List<Token>();
            while (true)
            {
                var token = NextToken();
                tokens.Add(token);
                if (token.Type == TokenType.EndOfFile) break;
            }
            return tokens;
        }

        private Token NextToken()
        {
            while (char.IsWhiteSpace(CurrentChar)) Advance();

            var startLine = _line;
            var startCol = _column;

            if (CurrentChar == '\0') return new Token(TokenType.EndOfFile, "", startLine, startCol);

            // 주석 처리
            if (CurrentChar == '/')
            {
                if (PeekNextChar() == '/')
                {
                    Advance(); Advance(); // '//' 소비
                    while (CurrentChar != '\n' && CurrentChar != '\0') Advance();
                    return NextToken(); // 재귀 호출로 다음 유효 토큰 찾기
                }
                else if (PeekNextChar() == '*')
                {
                    Advance(); Advance(); // '/*' 소비
                    while (true)
                    {
                        if (CurrentChar == '\0') throw new Exception($"[Lexer 오류] Line {startLine}:{startCol} 주석이 닫히지 않았습니다.");
                        if (CurrentChar == '*' && PeekNextChar() == '/')
                        {
                            Advance(); Advance(); // '*/' 소비
                            break;
                        }
                        Advance();
                    }
                    return NextToken();
                }
            }

            // 어트리뷰트 인식 (@...)
            if (CurrentChar == '@')
            {
                Advance(); // '@' 소비

                _sb.Clear();
                while (char.IsLetterOrDigit(CurrentChar) || CurrentChar == '_')
                {
                    _sb.Append(CurrentChar);
                    Advance();
                }
                return new Token(TokenType.Attribute, _sb.ToString(), startLine, startCol);
            }

            // 기호 인식
            // 기호는 1글자이므로 미리 값을 저장하고 Advance
            var symbolChar = CurrentChar.ToString();
            switch (CurrentChar)
            {
                case '{': Advance(); return new Token(TokenType.OpenBrace, symbolChar, startLine, startCol);
                case '}': Advance(); return new Token(TokenType.CloseBrace, symbolChar, startLine, startCol);
                case '[': Advance(); return new Token(TokenType.OpenBracket, symbolChar, startLine, startCol);
                case ']': Advance(); return new Token(TokenType.CloseBracket, symbolChar, startLine, startCol);
                case '=': Advance(); return new Token(TokenType.Equals, symbolChar, startLine, startCol);
                case ';': Advance(); return new Token(TokenType.Semicolon, symbolChar, startLine, startCol);
                case ':': Advance(); return new Token(TokenType.Colon, symbolChar, startLine, startCol);
                case ',': Advance(); return new Token(TokenType.Comma, symbolChar, startLine, startCol);
            }

            // 숫자 인식
            if (char.IsDigit(CurrentChar))
            {
                if (CurrentChar == '0' && (PeekNextChar() == 'x' || PeekNextChar() == 'X'))
                {
                    return ReadHexNumber(startLine, startCol);
                }

                _sb.Clear();
                while (char.IsDigit(CurrentChar))
                {
                    _sb.Append(CurrentChar);
                    Advance();
                }
                return new Token(TokenType.NumberLiteral, _sb.ToString(), startLine, startCol);
            }

            // 식별자, 키워드 인식
            if (char.IsLetter(CurrentChar) || CurrentChar == '_')
            {
                _sb.Clear();
                while (char.IsLetterOrDigit(CurrentChar) || CurrentChar == '_')
                {
                    _sb.Append(CurrentChar);
                    Advance();
                }

                var word = _sb.ToString();
                if (Keywords.TryGetValue(word, out var type))
                    return new Token(type, word, startLine, startCol);

                return new Token(TokenType.Identifier, word, startLine, startCol);
            }

            // 알 수 없는 문자
            var unknownChar = CurrentChar.ToString();
            Advance();
            return new Token(TokenType.Unknown, unknownChar, startLine, startCol);
        }

        private Token ReadHexNumber(int startLine, int startCol)
        {
            _sb.Clear();
            _sb.Append(CurrentChar); Advance(); // '0'
            _sb.Append(CurrentChar); Advance(); // 'x'

            while (IsHexDigit(CurrentChar))
            {
                _sb.Append(CurrentChar);
                Advance();
            }
            return new Token(TokenType.NumberLiteral, _sb.ToString(), startLine, startCol);
        }

        private bool IsHexDigit(char c)
        {
            return char.IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
        }
    }
}