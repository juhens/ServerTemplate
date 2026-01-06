using ServerCore;
using System.Net;
using DummyClient.Network;
using DummyClient.Scenarios;
using System.Diagnostics;

namespace DummyClient
{
    public class Program
    {
        enum ScenarioType
        {
            SessionCycle,
            BroadcastStress,
            LoginConflict,
            AreaTransition,
            LoginSpam,
            Exit
        }

        private static CancellationTokenSource? _cts;

        private static void Main(string[] args)
        {
            var scenarios = Enum.GetValues<ScenarioType>();
            var selectedIndex = 0;

            while (true)
            {
                // [Interactive Menu Loop]
                while (true)
                {
                    Console.Clear();
                    Console.WriteLine("=== DummyClient Scenario Selector ===");
                    Console.WriteLine("Use Up/Down arrows to select, Enter to start.\n");

                    for (var i = 0; i < scenarios.Length; i++)
                    {
                        var prefix = (i == selectedIndex) ? "> " : "  ";
                        var suffix = GetScenarioDescription(scenarios[i]);
                        if (i == selectedIndex)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"{prefix}{scenarios[i]} {suffix}");
                            Console.ResetColor();
                        }
                        else
                        {
                            Console.WriteLine($"{prefix}{scenarios[i]} {suffix}");
                        }
                    }

                    var key = Console.ReadKey(true).Key;
                    if (key == ConsoleKey.UpArrow)
                    {
                        selectedIndex = (selectedIndex == 0) ? scenarios.Length - 1 : selectedIndex - 1;
                    }
                    else if (key == ConsoleKey.DownArrow)
                    {
                        selectedIndex = (selectedIndex == scenarios.Length - 1) ? 0 : selectedIndex + 1;
                    }
                    else if (key == ConsoleKey.Enter)
                    {
                        break;
                    }
                }

                var currentScenario = scenarios[selectedIndex];
                if (currentScenario == ScenarioType.Exit)
                {
                    Console.WriteLine("Exiting DummyClient...");
                    break;
                }

                Console.Clear();
                Console.WriteLine($"Starting Scenario: {currentScenario}...");

                // [Preparation]
                _cts = new CancellationTokenSource();
                ServerSession.ResetIdCounter();
                ServerSession.TotalCount = 0;
                ServerSession.ConnectedCount = 0;

                var connectCount = currentScenario switch
                {
                    ScenarioType.SessionCycle => SessionCycleScenario.RecommendedConnectCount,
                    ScenarioType.BroadcastStress => BroadcastStressScenario.RecommendedConnectCount,
                    ScenarioType.LoginConflict => LoginConflictScenario.RecommendedConnectCount,
                    ScenarioType.AreaTransition => AreaTransitionScenario.RecommendedConnectCount,
                    ScenarioType.LoginSpam => LoginSpamScenario.RecommendedConnectCount,
                    _ => 0
                };

                if (currentScenario == ScenarioType.LoginConflict)
                {
                    if (connectCount < 2 || connectCount % 2 != 0)
                    {
                        Console.WriteLine($"[Error] LoginConflictScenario requires an even number of connections >= 2. Current: {connectCount}");
                        Console.WriteLine("Press Enter to return to menu...");
                        Console.ReadLine();
                        continue;
                    }
                }

                Thread.Sleep(1000);
                var host = Dns.GetHostName();
                var ipHost = Dns.GetHostEntry(host);
                var ip = ipHost.AddressList[0];
                var endPoint = new IPEndPoint(ip, 7777);
                var connector = new Connector();

                Console.WriteLine("Press ENTER at any time to Cancel/Stop the scenario.");

                // [Scenario Execution]
                if (currentScenario == ScenarioType.SessionCycle)
                {
                    Console.WriteLine("Running Sequential Login-Disconnect Loop...");

                    Task.Run(async () =>
                    {
                        while (!_cts.Token.IsCancellationRequested)
                        {
                            // 1. 세션 종료 대기용 TCS 생성
                            var disconnectTcs = new TaskCompletionSource();

                            // 2. 세션 팩토리 정의 (종료 시 TCS 신호)
                            Func<ServerSession> sessionFactory = () =>
                            {
                                var session = SessionManager.Instance.Generate();
                                var scenario = new SessionCycleScenario();

                                // 시나리오가 끝났을 때(Disconnected) TCS 완료 처리
                                scenario.OnSessionDisconnected = () =>
                                {
                                    disconnectTcs.TrySetResult();
                                };

                                session.Scenario = scenario;
                                return session;
                            };

                            // 3. 연결 시작
                            connector.Connect(endPoint, sessionFactory, 1);

                            // 4. 해당 세션이 완전히 끊길 때까지 대기
                            // 사용자가 취소(_cts)하면 즉시 빠져나옴
                            try
                            {
                                await Task.WhenAny(disconnectTcs.Task, Task.Delay(-1, _cts.Token));
                            }
                            catch { /* Ignore cancellation */ }

                            // 5. 너무 빠르면 보기 힘들고 부하가 심하므로 약간의 텀
                            if (!_cts.Token.IsCancellationRequested)
                            {
                                await Task.Delay(100);
                            }
                        }
                    }, _cts.Token);

                    // Enter 대기
                    WaitUntil(() => false, _cts.Token);
                }
                else if (currentScenario == ScenarioType.BroadcastStress)
                {
                    // 일반적인 동시 접속 시나리오용 Factory
                    Func<ServerSession> normalFactory = () =>
                    {
                        var session = SessionManager.Instance.Generate();
                        session.Scenario = new BroadcastStressScenario();
                        return session;
                    };

                    connector.Connect(endPoint, normalFactory, connectCount: connectCount);

                    if (WaitUntil(() => ServerSession.ConnectedCount >= connectCount, _cts.Token) == false)
                    {
                        goto Cleanup;
                    }

                    Console.WriteLine("All Connected and Logged In. Starting Chatting...");

                    _ = Task.Run(() =>
                    {
                        while (!_cts.Token.IsCancellationRequested)
                        {
                            var sessions = SessionManager.Instance.Sessions;
                            foreach (var session in sessions)
                            {
                                session.Scenario?.Update(session);
                            }
                            Thread.Sleep(200);
                        }
                    }, _cts.Token);

                    var monitorTask = RunBroadcastMonitoring(connectCount, _cts.Token);
                    WaitUntil(() => monitorTask.IsCompleted, _cts.Token);
                }
                else
                {
                    // 나머지 시나리오들 공통 Factory 처리
                    Func<ServerSession> factory = () =>
                    {
                        var session = SessionManager.Instance.Generate();
                        session.Scenario = currentScenario switch
                        {
                            ScenarioType.LoginConflict => new LoginConflictScenario(),
                            ScenarioType.AreaTransition => new AreaTransitionScenario(),
                            ScenarioType.LoginSpam => new LoginSpamScenario(),
                            _ => throw new NotImplementedException()
                        };
                        return session;
                    };

                    connector.Connect(endPoint, factory, connectCount: connectCount);

                    if (currentScenario == ScenarioType.LoginConflict)
                    {
                        var targetSuccessCount = connectCount / 2;
                        Console.WriteLine($"Waiting for {targetSuccessCount} attackers to succeed...");

                        WaitUntil(() =>
                        {
                            Console.WriteLine($"Success Count: {ServerSession.ConnectedCount} / {targetSuccessCount}");
                            return ServerSession.ConnectedCount >= targetSuccessCount;
                        }, _cts.Token, intervalMs: 1000);

                        if (!_cts.Token.IsCancellationRequested)
                            Console.WriteLine("All Attackers Succeeded! Scenario Passed.");
                    }
                    else if (currentScenario == ScenarioType.AreaTransition)
                    {
                        Console.WriteLine("Running Zombie Test Sequence (5 seconds)...");
                        var endTime = DateTime.Now.AddSeconds(5);
                        WaitUntil(() => DateTime.Now >= endTime, _cts.Token);
                        Console.WriteLine("Test Finished. Check Server Console for 'Zombie Count'.");
                    }
                    else if (currentScenario == ScenarioType.LoginSpam)
                    {
                        Console.WriteLine("Spamming Login Packets... (5 seconds)");
                        var endTime = DateTime.Now.AddSeconds(5);
                        WaitUntil(() => DateTime.Now >= endTime, _cts.Token);
                    }
                }

            Cleanup:
                Console.WriteLine("\n[Finished] Press Enter to return to menu...");
                while (Console.KeyAvailable) Console.ReadKey(true);
                Console.ReadLine();

                Console.WriteLine("Cleaning up sessions...");
                _cts?.Cancel();
                SessionManager.Instance.Clear();
                Thread.Sleep(500);
            }
        }

        private static bool WaitUntil(Func<bool> condition, CancellationToken token, int intervalMs = 100)
        {
            while (!token.IsCancellationRequested)
            {
                if (condition()) return true;

                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Enter)
                    {
                        Console.WriteLine(" [Cancelled by User]");
                        _cts?.Cancel();
                        return false;
                    }
                }
                Thread.Sleep(intervalMs);
            }
            return false;
        }

        private static string GetScenarioDescription(ScenarioType type)
        {
            return type switch
            {
                ScenarioType.SessionCycle => $"(Sequential Loop, 1 User)",
                ScenarioType.BroadcastStress => $"({BroadcastStressScenario.RecommendedConnectCount} Users, Chat Broadcast)",
                ScenarioType.LoginConflict => $"({LoginConflictScenario.RecommendedConnectCount} Users, Duplicate Login Check)",
                ScenarioType.AreaTransition => $"({AreaTransitionScenario.RecommendedConnectCount} Users, Zombie Player Check)",
                ScenarioType.LoginSpam => $"({LoginSpamScenario.RecommendedConnectCount} Users, Login Packet Spamming)",
                ScenarioType.Exit => "(Close the application)",
                _ => ""
            };
        }

        private static Task RunBroadcastMonitoring(int connectCount, CancellationToken token)
        {
            return Task.Run(() =>
            {
                long last = 0;
                var totalPacketsExpected = (long)connectCount * BroadcastStressScenario.MaxSendCount * connectCount;

                var sw = Stopwatch.StartNew();

                while (ServerSession.TotalCount < totalPacketsExpected)
                {
                    if (token.IsCancellationRequested) return;

                    var now = ServerSession.TotalCount;
                    Console.WriteLine($"{now:N0} (+ {now - last:N0})");
                    last = now;
                    Thread.Sleep(1000);
                }

                sw.Stop();
                var seconds = sw.Elapsed.TotalSeconds;
                var pps = ServerSession.TotalCount / seconds;

                Console.WriteLine($"All Expected Chat Recv: {ServerSession.TotalCount:N0}");
                Console.WriteLine($"Time: {seconds:F2}s | Avg PPS: {pps:N0}");
            }, token);
        }
    }
}