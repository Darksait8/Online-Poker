using System;

namespace PokerServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine("   🎮 ПОКЕРНЫЙ СЕРВЕР");
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine();
            
            int port = 8888;
            bool enableCloudSync = false;
            string syncUrl = null;
            
            // Парсим аргументы командной строки
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--port" && i + 1 < args.Length && int.TryParse(args[i + 1], out int customPort))
                {
                    port = customPort;
                    i++;
                }
                else if (args[i] == "--sync" && i + 1 < args.Length)
                {
                    syncUrl = args[i + 1];
                    enableCloudSync = true;
                    i++;
                }
                else if (args[i] == "--sync-env")
                {
                    // Используем переменные окружения
                    syncUrl = Environment.GetEnvironmentVariable("POKER_SYNC_URL");
                    enableCloudSync = !string.IsNullOrEmpty(syncUrl);
                }
            }
            
            // Проверяем переменные окружения, если не указаны в аргументах
            if (!enableCloudSync)
            {
                syncUrl = Environment.GetEnvironmentVariable("POKER_SYNC_URL") ?? "";
                enableCloudSync = !string.IsNullOrEmpty(syncUrl);
            }
            
            if (enableCloudSync)
            {
                Console.WriteLine($"☁️ Облачная синхронизация: {syncUrl}");
            }
            
            var server = new PokerServer(port, enableCloudSync);
            
            try
            {
                server.Start();
                
                Console.WriteLine();
                Console.WriteLine("Нажмите любую клавишу для остановки сервера...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Критическая ошибка: {ex.Message}");
            }
            finally
            {
                server.Stop();
            }
        }
    }
}

