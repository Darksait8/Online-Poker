using System;

namespace HoldemController.Logging
{
    internal static class Logger
    {
        private static FileLogger _logger;
        private static bool _writeToConsole;

        public static void SetWriteToConsole(bool writeToConsole)
        {
            _writeToConsole = writeToConsole;
        }

        public static void Initialize(string fileName)
        {
            _logger = new FileLogger(fileName);
        }

        public static void Log(string sMessage, params object[] arg)
        {
            if (_logger == null)
            {
                throw new Exception("Logger has not been initialized");
            }
            if (_writeToConsole)
            {
                Console.WriteLine(sMessage, arg);
            }
            _logger.Log(sMessage, arg);
        }

        public static void Close()
        {
            _logger?.Dispose();
            _logger = null;
        }
    }
}
