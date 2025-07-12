using System;
using System.IO;
using System.Threading.Tasks;

namespace PresenterTimerApp
{
    public class Logger
    {
        private readonly string _logFilePath;
        private readonly long _maxLogSizeBytes = 1024 * 1024; // 1MB

        public Logger(string dataFolder)
        {
            _logFilePath = Path.Combine(dataFolder, "errors.log");
        }

        public async Task LogErrorAsync(string message)
        {
            try
            {
                var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}\n";
                if (File.Exists(_logFilePath) && new FileInfo(_logFilePath).Length > _maxLogSizeBytes)
                {
                    File.Move(_logFilePath, _logFilePath + ".bak", true);
                }
                await File.AppendAllTextAsync(_logFilePath, logEntry);
            }
            catch
            {
                // Swallow exceptions to avoid crashing
            }
        }
    }
}