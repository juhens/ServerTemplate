using System.Text.Json;
using PacketGen.CodeGenerator;
using PacketGen.CodeGenerator.CSharp;

namespace PacketGen
{
    internal static class Program
    {
        private const string ConfigFileName = "config.json";
        private const string PacketGenFileName = "PacketGen.cs";
        private const string ServerManagerFileName = "PacketManagerGen.cs";
        private const string ClientManagerFileName = "PacketManagerGen.cs";

        private const string TestPdlContent = 
            """
            struct Test {
                guid A;
            }
            """;

        private static void Main(string[] args)
        {
            var exePath = AppDomain.CurrentDomain.BaseDirectory;

            try
            {
                var option = LoadOrInitConfig(exePath);

                if (args.Length == 0)
                {
                    RunTestMode(option);
                }
                else if (args.Length == 1)
                {
                    RunFileMode(args[0], exePath, option);
                }
                else
                {
                    Console.WriteLine("사용법: SimpleProto <PDL 파일 경로>");
                }
            }
            catch (Exception ex)
            {
                PrintException(ex);
            }

            Console.WriteLine("\n[Enter] 키를 누르면 종료합니다...");
            Console.ReadLine();
        }

        private static void RunTestMode(GenOption option)
        {
            Console.WriteLine("====================================================");
            Console.WriteLine(" DEVELOPMENT TEST MODE (No Files Saved)");
            Console.WriteLine("====================================================");

            // 1. 분석 (토큰 리스트를 반환받음)
            var (typeRegistry, definitions, tokens) = AnalyzePdlSource(TestPdlContent);

            // 토큰 출력
            Console.WriteLine("\n[Tokens]");
            foreach (var token in tokens)
            {
                Console.WriteLine(token);
            }
            Console.WriteLine("----------------------------------------------------");

            // 2. 코드 생성
            var artifacts = GenerateCode(definitions, option, typeRegistry);

            // 3. 결과 콘솔 출력
            PrintArtifactsToConsole(artifacts);
        }

        private static void RunFileMode(string pdlPath, string basePath, GenOption option)
        {
            if (!File.Exists(pdlPath))
            {
                throw new FileNotFoundException($"지정된 경로에서 PDL 파일을 찾을 수 없습니다: {pdlPath}");
            }

            var pdlContent = File.ReadAllText(pdlPath);

            // 1. 분석
            var (typeRegistry, definitions, _) = AnalyzePdlSource(pdlContent);

            Console.WriteLine("----------------------------------------------------\n");

            // 2. 코드 생성
            var artifacts = GenerateCode(definitions, option, typeRegistry);

            // 3. 파일 저장
            SaveArtifacts(basePath, option, artifacts);

            Console.WriteLine("\n모든 작업이 성공적으로 완료되었습니다.");
        }

        private static GenOption LoadOrInitConfig(string basePath)
        {
            var configPath = Path.Combine(basePath, ConfigFileName);
            var options = new JsonSerializerOptions { WriteIndented = true };
            var context = new AppJsonContext(options);

            if (!File.Exists(configPath))
            {
                var json = JsonSerializer.Serialize(new GenOption(), context.GenOption);
                File.WriteAllText(configPath, json);
                Console.WriteLine($"[Config] 기본 설정 파일 생성됨: {configPath}");
            }

            var loadedOption = JsonSerializer.Deserialize(File.ReadAllText(configPath), context.GenOption);
            return loadedOption ?? throw new InvalidOperationException("설정 파일을 로드할 수 없습니다.");
        }

        private static (TypeRegistry registry, List<Definition> definitions, List<Token> tokens) AnalyzePdlSource(string source)
        {
            var typeRegistry = new TypeRegistry();

            // 1. Lexing
            var lexer = new Lexer(source);
            var tokens = lexer.GetTokens();
            Console.WriteLine($"1단계: 렉싱 완료 ({tokens.Count} 토큰)");

            // 2. Parsing
            var parser = new Parser(tokens);
            var definitions = parser.Parse();
            Console.WriteLine($"2단계: 파싱 완료 ({definitions.Count} 정의)");

            // 3. Validation
            var validator = new SemanticValidator(typeRegistry);
            validator.Validate(definitions);
            Console.WriteLine("3단계: 검증 완료 (이상 없음)");

            return (typeRegistry, definitions, tokens);
        }

        private static (string CommonCode, string ServerMgrCode, string ClientMgrCode) GenerateCode(List<Definition> definitions, GenOption option, TypeRegistry registry)
        {
            var packetGen = new CSharpPacketGen(definitions, option, registry);
            var commonCode = packetGen.Generate();

            var packetManagerGen = new CSharpPacketManagerGen(definitions, option, registry);
            var (serverMgr, clientMgr) = packetManagerGen.Generate();

            return (commonCode, serverMgr, clientMgr);
        }

        private static void PrintArtifactsToConsole((string Common, string ServerMgr, string ClientMgr) artifacts)
        {
            Console.WriteLine("\n[Generated: Common Packet Code]");
            Console.WriteLine(artifacts.Common);
            Console.WriteLine("\n[Generated: Server Manager Code]");
            Console.WriteLine(artifacts.ServerMgr);
            Console.WriteLine("\n[Generated: Client Manager Code]");
            Console.WriteLine(artifacts.ClientMgr);
        }

        private static void SaveArtifacts(string basePath, GenOption option, (string Common, string ServerMgr, string ClientMgr) artifacts)
        {
            var serverDir = Path.GetFullPath(Path.Combine(basePath, option.ServerExportPath));
            var clientDir = Path.GetFullPath(Path.Combine(basePath, option.ClientExportPath));

            EnsureDirectoryExists(serverDir);
            EnsureDirectoryExists(clientDir);

            File.WriteAllText(Path.Combine(serverDir, PacketGenFileName), artifacts.Common);
            File.WriteAllText(Path.Combine(clientDir, PacketGenFileName), artifacts.Common);
            Console.WriteLine($"[Saved Common] {PacketGenFileName}");

            var serverManagerPath = Path.Combine(serverDir, ServerManagerFileName);
            File.WriteAllText(serverManagerPath, artifacts.ServerMgr);
            Console.WriteLine($"[Saved Server] {ServerManagerFileName}");

            var clientManagerPath = Path.Combine(clientDir, ClientManagerFileName);
            File.WriteAllText(clientManagerPath, artifacts.ClientMgr);
            Console.WriteLine($"[Saved Client] {ClientManagerFileName}");

            Console.WriteLine("\n----------------------------------------------------");
            Console.WriteLine($"Server Path: {serverDir}");
            Console.WriteLine($"Client Path: {clientDir}");
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private static void PrintException(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n오류 발생: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            Console.ResetColor();
        }
    }
}