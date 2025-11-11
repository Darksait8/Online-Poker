using System;

namespace HoldemController.Logging
{
    internal static class TimingLogger
    {
        private static FileLogger _logger;

        public static void Initialize(string fileName)
        {
            _logger = new FileLogger(fileName);
            _logger.Log("Hand,Stage,PlayerNum,MethodName, MillisecondsTaken, OtherInfo1, OtherInfo2, OtherInfo3");
        }

        public static void Log(string sMessage, params object[] arg)
        {
            if (_logger == null)
            {
                throw new Exception("Logger has not been initialized");
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