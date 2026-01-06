namespace PacketGen
{
    public abstract class PacketGenException : Exception
    {
        public int Line { get; }
        public int Column { get; }

        protected PacketGenException(string message, int line, int column)
            : base($"{message} (Line: {line}, Col: {column})")
        {
            Line = line;
            Column = column;
        }
    }

    public class ParseException : PacketGenException
    {
        public ParseException(string message, int line, int column) : base($"[구문 오류] {message}", line, column) { }
    }

    public class SemanticException : PacketGenException
    {
        public SemanticException(string message, int line, int column) : base($"[의미 오류] {message}", line, column) { }
    }
}