using System;

namespace ServerCore
{
    public enum LogLevel { Dbg, Inf, Wrn, Err, Fat }

    public static class Log
    {
        // 외부 로깅 구현체를 연결할 델리게이트
        // 인자: (LogLevel, SourceName, Template, Args)
        public static Action<LogLevel, string, string, object[]>? LoggerHandler;

        public static void Info(object source, string messageTemplate, params object[] args) 
            => Dispatch(LogLevel.Inf, source, messageTemplate, args);

        public static void Warn(object source, string messageTemplate, params object[] args) 
            => Dispatch(LogLevel.Wrn, source, messageTemplate, args);

        public static void Error(object source, string messageTemplate, params object[] args) 
            => Dispatch(LogLevel.Err, source, messageTemplate, args);
        
        public static void Fatal(object source, string messageTemplate, params object[] args)
            => Dispatch(LogLevel.Fat, source, messageTemplate, args);

        public static void Debug(object source, string messageTemplate, params object[] args)
            => Dispatch(LogLevel.Dbg, source, messageTemplate, args);

        // 정적 컨텍스트 (클래스 이름 직접 전달)
        public static void Info(Type sourceType, string messageTemplate, params object[] args) 
            => Dispatch(LogLevel.Inf, sourceType, messageTemplate, args);

        public static void Warn(Type sourceType, string messageTemplate, params object[] args) 
            => Dispatch(LogLevel.Wrn, sourceType, messageTemplate, args);

        public static void Error(Type sourceType, string messageTemplate, params object[] args) 
            => Dispatch(LogLevel.Err, sourceType, messageTemplate, args);
        
        public static void Fatal(Type sourceType, string messageTemplate, params object[] args)
            => Dispatch(LogLevel.Fat, sourceType, messageTemplate, args);

        public static void Debug(Type sourceType, string messageTemplate, params object[] args)
            => Dispatch(LogLevel.Dbg, sourceType, messageTemplate, args);

        // 기본 컨텍스트 (문자열 직접 전달)
        public static void Info(string sourceName, string messageTemplate, params object[] args) 
            => Dispatch(LogLevel.Inf, sourceName, messageTemplate, args);

        public static void Warn(string sourceName, string messageTemplate, params object[] args) 
            => Dispatch(LogLevel.Wrn, sourceName, messageTemplate, args);

        public static void Error(string sourceName, string messageTemplate, params object[] args) 
            => Dispatch(LogLevel.Err, sourceName, messageTemplate, args);

        public static void Fatal(string sourceName, string messageTemplate, params object[] args)
            => Dispatch(LogLevel.Fat, sourceName, messageTemplate, args);

        public static void Debug(string sourceName, string messageTemplate, params object[] args)
            => Dispatch(LogLevel.Dbg, sourceName, messageTemplate, args);


        private static void Dispatch(LogLevel level, object source, string template, object[] args)
        {
            var sourceName = source.GetType().Name;
            Dispatch(level, sourceName, template, args);
        }

        private static void Dispatch(LogLevel level, Type sourceType, string template, object[] args)
        {
            var sourceName = sourceType.Name;
            Dispatch(level, sourceName, template, args);
        }

        private static void Dispatch(LogLevel level, string sourceName, string template, object[] args)
        {
            if (LoggerHandler != null)
            {
                LoggerHandler.Invoke(level, sourceName, template, args);
            }
            else
            {
                var formattedMessage = template;
                if (args != null && args.Length > 0)
                {
                    try
                    {
                        formattedMessage = string.Format(template, args);
                    }
                    catch
                    {
                        formattedMessage = $"{template} [Args: {string.Join(", ", args)}]";
                    }
                }
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{level}] [{sourceName}] {formattedMessage}");
            }
        }
    }
}