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
            if (args.Length > 0 && int.TryParse(args[0], out int customPort))
            {
                port = customPort;
            }
            
            var server = new PokerServer(port);
            
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

