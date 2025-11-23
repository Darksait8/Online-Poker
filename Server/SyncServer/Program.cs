using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PokerServer;

namespace SyncServer
{
    /// <summary>
    /// Отдельный сервер для синхронизации данных пользователей
    /// Можно запустить на отдельном сервере или хостинге
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine("   ☁️ СЕРВЕР СИНХРОНИЗАЦИИ ПОКЕРА");
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine();
            
            int port = 8889;
            string dataFilePath = "sync_users.json";
            string apiKey = "default-key-change-me";
            
            // Парсим аргументы
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--port" && i + 1 < args.Length && int.TryParse(args[i + 1], out int customPort))
                {
                    port = customPort;
                    i++;
                }
                else if (args[i] == "--data" && i + 1 < args.Length)
                {
                    dataFilePath = args[i + 1];
                    i++;
                }
                else if (args[i] == "--key" && i + 1 < args.Length)
                {
                    apiKey = args[i + 1];
                    i++;
                }
            }
            
            // Проверяем переменные окружения
            if (Environment.GetEnvironmentVariable("SYNC_PORT") != null)
                int.TryParse(Environment.GetEnvironmentVariable("SYNC_PORT"), out port);
            
            if (Environment.GetEnvironmentVariable("SYNC_DATA_FILE") != null)
                dataFilePath = Environment.GetEnvironmentVariable("SYNC_DATA_FILE");
            
            if (Environment.GetEnvironmentVariable("SYNC_API_KEY") != null)
                apiKey = Environment.GetEnvironmentVariable("SYNC_API_KEY");
            
            var syncServer = new SimpleSyncServer(port, dataFilePath, apiKey);
            
            try
            {
                syncServer.Start();
                
                Console.WriteLine();
                Console.WriteLine("Нажмите любую клавишу для остановки сервера синхронизации...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Критическая ошибка: {ex.Message}");
            }
            finally
            {
                syncServer.Stop();
            }
        }
    }
}

