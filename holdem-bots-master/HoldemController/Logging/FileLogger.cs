using System;
using System.IO;

namespace HoldemController.Logging
{
    public class FileLogger : IDisposable
    {
        private readonly TextWriter _writer;

        public FileLogger(string fileName)
        {
            var directory = Path.GetDirectoryName(fileName);
            if (directory != null)
            {
                Directory.CreateDirectory(directory);
            }
            _writer = new StreamWriter(fileName, false);
        }

        public void Log(string sMessage, params object[] arg)
        {
            _writer.WriteLine(sMessage, arg);
        }

        public void Dispose()
        {
            _writer.Close();
        }
    }
}