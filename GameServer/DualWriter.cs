using System.Collections.Concurrent;
using System.Text;

namespace GameServer
{
    public class DualWriter : TextWriter
    {
        private readonly TextWriter _consoleWriter;
        private readonly StreamWriter _fileWriter;
        private readonly BlockingCollection<LogEntry> _logQueue;
        private readonly Task _logTask;
        private readonly CancellationTokenSource _cts;

        private struct LogEntry
        {
            public DateTime Timestamp;
            public string LevelStr;
            public ConsoleColor LevelColor;
            public string Source;
            public string Message;
            public string RawMessage; // 기본 WriteLine용
            public bool IsStyled;
        }

        public DualWriter(string path)
        {
            _consoleWriter = Console.Out;
            _fileWriter = new StreamWriter(path, append: false, Encoding.UTF8) { AutoFlush = true };
            _logQueue = new BlockingCollection<LogEntry>(new ConcurrentQueue<LogEntry>());
            _cts = new CancellationTokenSource();
            
            // 백그라운드 로그 처리 스레드 시작
            _logTask = Task.Factory.StartNew(ProcessLogQueue, TaskCreationOptions.LongRunning);
        }

        public override Encoding Encoding => _consoleWriter.Encoding;

        // 동기적인 기본 Write들은 비동기 로거 컨텍스트에서 사용하지 않음
        public override void Write(char value) { }
        public override void Write(string? value) { } 

        public override void WriteLine(string? value)
        {
            if (value == null) return;
            // 기본 WriteLine도 큐에 태움 (스타일 없음)
            _logQueue.Add(new LogEntry
            {
                Timestamp = DateTime.Now,
                RawMessage = value,
                IsStyled = false
            });
        }

        public void WriteLine(string value, DateTime timestamp)
        {
            _logQueue.Add(new LogEntry
            {
                Timestamp = timestamp,
                RawMessage = value,
                IsStyled = false
            });
        }

        // [Main] 스타일 있는 로그 (큐에 추가)
        public void WriteStyledLog(DateTime timestamp, string levelStr, ConsoleColor levelColor, string source, string message)
        {
            _logQueue.Add(new LogEntry
            {
                Timestamp = timestamp,
                LevelStr = levelStr,
                LevelColor = levelColor,
                Source = source,
                Message = message,
                IsStyled = true
            });
        }

        private void ProcessLogQueue()
        {
            try
            {
                foreach (var entry in _logQueue.GetConsumingEnumerable(_cts.Token))
                {
                    try
                    {
                        if (entry.IsStyled)
                        {
                            WriteStyledLogInternal(entry);
                        }
                        else
                        {
                            WriteRawLogInternal(entry);
                        }
                    }
                    catch
                    {
                        // 로그 쓰다 에러나면 무시 (로거가 죽으면 안됨)
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 종료 시점
            }
            finally
            {
                // 남은 로그 비우기
                FlushRemainingLogs();
            }
        }

        private void FlushRemainingLogs()
        {
            while (_logQueue.TryTake(out var entry))
            {
                try
                {
                    if (entry.IsStyled) WriteStyledLogInternal(entry);
                    else WriteRawLogInternal(entry);
                }
                catch { }
            }
        }

        // [Internal] 실제 출력 담당 (단일 스레드에서 실행됨)
        private void WriteStyledLogInternal(LogEntry entry)
        {
            var timeStr = entry.Timestamp.ToString("HH:mm:ss.fff");
            var fullLogLine = $"[{timeStr} {entry.LevelStr}] {entry.Source}: {entry.Message}";

            // 1. [Time Level]
            Console.ForegroundColor = ConsoleColor.DarkGray;
            _consoleWriter.Write($"[{timeStr} "); 

            Console.ForegroundColor = entry.LevelColor;
            _consoleWriter.Write($"{entry.LevelStr}");

            Console.ForegroundColor = ConsoleColor.DarkGray;
            _consoleWriter.Write("] ");

            // 2. Source (DarkGreen)
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            _consoleWriter.Write($"{entry.Source}: ");

            // 3. Message
            if (entry.LevelColor == ConsoleColor.Red || entry.LevelColor == ConsoleColor.DarkRed)
                Console.ForegroundColor = ConsoleColor.Red;
            else
                Console.ForegroundColor = ConsoleColor.Gray;

            _consoleWriter.WriteLine(entry.Message);
            Console.ResetColor();

            // File Output
            _fileWriter.WriteLine(fullLogLine);
        }

        private void WriteRawLogInternal(LogEntry entry)
        {
            var timeStr = entry.Timestamp.ToString("HH:mm:ss.fff");
            var fullLogLine = $"[{timeStr}] {entry.RawMessage}";

            _consoleWriter.WriteLine(fullLogLine);
            _fileWriter.WriteLine(fullLogLine);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _logQueue.CompleteAdding(); // 더 이상 추가 불가
                _cts.Cancel(); 
                _logTask.Wait(1000); // 1초 정도 대기 (잔여 로그 처리)
                
                _fileWriter.Dispose();
                _cts.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}