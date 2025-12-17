using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HospitalDomainLib
{
    public class StreamService<T>
    {
        private readonly object _syncObject = new object();
        private bool _isWriting = false;

        public async Task WriteToStreamAsync(Stream stream, IEnumerable<T> data, IProgress<string> progress)
        {
            lock (_syncObject)
            {
                if (_isWriting)
                {
                    throw new InvalidOperationException("Другая запись уже выполняется");
                }
                _isWriting = true;
            }

            try
            {
                progress?.Report($"Поток {Thread.CurrentThread.ManagedThreadId}: Начало записи в поток");

                // Сериализация данных
                var options = new JsonSerializerOptions { WriteIndented = true };
                var jsonData = JsonSerializer.Serialize(data, options);
                var buffer = System.Text.Encoding.UTF8.GetBytes(jsonData);

                // Очистка потока
                stream.SetLength(0);
                stream.Position = 0;

                // Медленная запись с задержкой
                int chunkSize = Math.Max(buffer.Length / 30, 1);
                for (int i = 0; i < buffer.Length; i += chunkSize)
                {
                    int currentChunkSize = Math.Min(chunkSize, buffer.Length - i);
                    await stream.WriteAsync(buffer.AsMemory(i, currentChunkSize));

                    // Задержка для имитации медленной записи
                    await Task.Delay(100);

                    if (chunkSize > 0 && i % (chunkSize * 10) == 0)
                    {
                        int percentComplete = buffer.Length > 0 ? i * 100 / buffer.Length : 0;
                        progress?.Report($"Поток {Thread.CurrentThread.ManagedThreadId}: Запись {percentComplete}% завершена");
                    }
                }

                // Важно: сбрасываем буфер
                await stream.FlushAsync();
                progress?.Report($"Поток {Thread.CurrentThread.ManagedThreadId}: Запись в поток завершена");
            }
            finally
            {
                lock (_syncObject)
                {
                    _isWriting = false;
                }
            }
        }

        public async Task CopyFromStreamAsync(Stream stream, string fileName, IProgress<string> progress)
        {
            progress?.Report($"Поток {Thread.CurrentThread.ManagedThreadId}: Начало копирования из потока в файл");

            // Ожидаем завершения записи
            while (true)
            {
                lock (_syncObject)
                {
                    if (!_isWriting)
                        break;
                }
                await Task.Delay(50);
            }

            stream.Position = 0; // Возвращаемся в начало потока

            using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                byte[] buffer = new byte[4096];
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                }
            }

            progress?.Report($"Поток {Thread.CurrentThread.ManagedThreadId}: Копирование в файл завершено");
        }

        public async Task<int> GetStatisticsAsync(string fileName, Func<T, bool> filter)
        {
            if (!File.Exists(fileName))
                return 0;

            try
            {
                string json = await File.ReadAllTextAsync(fileName);
                var data = JsonSerializer.Deserialize<List<T>>(json);

                if (data == null)
                    return 0;

                int count = 0;
                foreach (var item in data)
                {
                    if (filter(item))
                        count++;
                }

                return count;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Ошибка десериализации: {ex.Message}");
                return 0;
            }
        }
    }
}