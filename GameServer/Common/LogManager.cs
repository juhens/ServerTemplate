using System.Text.RegularExpressions;
using ServerCore;

namespace GameServer.Common
{
    public static class LogManager
    {
        private static DualWriter? _dualWriter;

        public static void Initialize()
        {
            // [Log] Route ServerCore.Log to DualWriter
            _dualWriter = new DualWriter("server_log.txt");
            
            Log.LoggerHandler = (level, sourceName, template, args) =>
            {
                var message = template;
                if (args != null && args.Length > 0)
                {
                    try
                    {
                        // [Compat] Serilog 스타일({Name})을 string.Format 스타일({0})로 변환
                        var argIndex = 0;
                        var indexedTemplate = Regex.Replace(template, @"\{[a-zA-Z0-9_@]+\}", m => "{" + (argIndex++) + "}");
                        
                        message = string.Format(indexedTemplate, args);
                    }
                    catch
                    {
                        var argsStr = string.Join(", ", args);
                        message = $"{template} [Args: {argsStr}]";
                    }
                }

                // [Format] [Level] Source : Message
                var levelStr = level.ToString().PadRight(3);
                var sourceStr = sourceName.Length > 15 ? sourceName.Substring(0, 15) : sourceName.PadRight(15);

                var color = ConsoleColor.White;
                switch (level)
                {
                    case LogLevel.Inf: color = ConsoleColor.Green; break;
                    case LogLevel.Wrn: color = ConsoleColor.Yellow; break;
                    case LogLevel.Err: 
                    case LogLevel.Fat: color = ConsoleColor.Red; break;
                    case LogLevel.Dbg: color = ConsoleColor.Gray; break;
                }

                // [Time] Capture time at the moment of logging request
                _dualWriter.WriteStyledLog(DateTime.Now, levelStr, color, sourceStr, message);
            };
        }

        public static void Dispose()
        {
            _dualWriter?.Dispose();
        }
    }
}