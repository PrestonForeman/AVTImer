using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace PresenterTimerApp
{
    public class FileService
    {
        private readonly string _dataFolder;

        public FileService(string dataFolder)
        {
            _dataFolder = dataFolder;
        }

        public async Task<string[]> LoadMessagesAsync(string filePath)
        {
            if (File.Exists(filePath))
            {
                string json = await File.ReadAllTextAsync(filePath);
                return JsonSerializer.Deserialize<string[]>(json) ?? new string[0];
            }
            return new string[0];
        }

        public async Task SaveMessagesAsync(string filePath, string[] messages)
        {
            string json = JsonSerializer.Serialize(messages);
            await File.WriteAllTextAsync(filePath, json);
        }

        public async Task<string> LoadImagePathAsync(string filePath)
        {
            if (File.Exists(filePath))
            {
                string json = await File.ReadAllTextAsync(filePath);
                return JsonSerializer.Deserialize<string>(json) ?? "";
            }
            return "";
        }

        public async Task SaveImagePathAsync(string filePath, string imagePath)
        {
            string json = JsonSerializer.Serialize(imagePath);
            await File.WriteAllTextAsync(filePath, json);
        }

        public async Task<AppSettings> LoadSettingsAsync(string filePath)
        {
            if (File.Exists(filePath))
            {
                string json = await File.ReadAllTextAsync(filePath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            return new AppSettings();
        }

        public async Task SaveSettingsAsync(string filePath, AppSettings settings)
        {
            string json = JsonSerializer.Serialize(settings);
            await File.WriteAllTextAsync(filePath, json);
        }

        public async Task BackupSettingsAsync(string sourcePath, string backupPath)
        {
            if (File.Exists(sourcePath))
            {
                await Task.Run(() => File.Copy(sourcePath, backupPath, true));
            }
        }
    }
}