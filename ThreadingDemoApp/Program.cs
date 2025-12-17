using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HospitalDomainLib;
using IntegralCalculatorLib;

namespace ThreadingDemoApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== ЗАДАНИЕ 1.1 ===");
            await Task1_1();

            Console.WriteLine("\n=== ЗАДАНИЕ 1.2 и 1.3 ===");
            await Task1_2_1_3();

            Console.WriteLine("\nНажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        static async Task Task1_1()
        {
            // Часть 4: Запуск в отдельном потоке
            Console.WriteLine("--- Запуск в отдельном потоке ---");
            var calculator1 = new IntegralCalculator();

            calculator1.ProgressChanged += (progress, threadId) =>
            {
                Console.WriteLine($"Поток {threadId}: Прогресс: {progress:F2}%");
            };

            // Используем Task.Run для асинхронного выполнения
            var task1 = Task.Run(async () =>
            {
                calculator1.CalculationCompleted += (result, ticks, threadId) =>
                {
                    Console.WriteLine($"Поток {threadId}: Завершен с результатом: {result:F6}");
                    Console.WriteLine($"Поток {threadId}: Время выполнения: {ticks} тиков");
                };

                // Теперь это асинхронный метод
                await calculator1.CalculateIntegralAsync(1);
            });

            await task1;

            // Часть 4: Два потока с разными приоритетами
            Console.WriteLine("\n--- Два потока с разными приоритетами ---");

            var calculatorHigh = new IntegralCalculator();
            var calculatorLow = new IntegralCalculator();

            calculatorHigh.ProgressChanged += (progress, threadId) =>
            {
                Console.WriteLine($"Поток HIGH ({threadId}): Прогресс: {progress:F2}%");
            };

            calculatorLow.ProgressChanged += (progress, threadId) =>
            {
                Console.WriteLine($"Поток LOW ({threadId}): Прогресс: {progress:F2}%");
            };

            // Создаем задачи вместо потоков для более современного подхода
            var highPriorityTask = Task.Run(async () =>
            {
                calculatorHigh.CalculationCompleted += (result, ticks, threadId) =>
                {
                    Console.WriteLine($"Поток HIGH приоритета {threadId}: Результат = {result:F6}, Время = {ticks} тиков");
                };
                await calculatorHigh.CalculateIntegralAsync(2);
            });

            var lowPriorityTask = Task.Run(async () =>
            {
                calculatorLow.CalculationCompleted += (result, ticks, threadId) =>
                {
                    Console.WriteLine($"Поток LOW приоритета {threadId}: Результат = {result:F6}, Время = {ticks} тиков");
                };
                await calculatorLow.CalculateIntegralAsync(3);
            });

            await Task.WhenAll(highPriorityTask, lowPriorityTask);

            // Часть 5: Только один поток из 5 (с lock)
            Console.WriteLine("\n--- Только один поток из 5 (с lock) ---");

            var calculatorLock = new IntegralCalculator();
            var lockTasks = new List<Task>();

            for (int i = 0; i < 5; i++)
            {
                int threadNum = i + 1;
                var task = Task.Run(async () =>
                {
                    Console.WriteLine($"Поток {Thread.CurrentThread.ManagedThreadId} начал выполнение");

                    // Асинхронная версия с lock
                    await Task.Run(() =>
                    {
                        calculatorLock.CalculateIntegralWithLock(threadNum);
                    });

                    Console.WriteLine($"Поток {Thread.CurrentThread.ManagedThreadId} завершил выполнение");
                });

                lockTasks.Add(task);
                await Task.Delay(10); // Небольшая задержка между запусками
            }

            await Task.WhenAll(lockTasks);

            // Часть 6: Семафор - ограниченное количество потоков
            Console.WriteLine("\n--- Семафор: только 2 потока из 5 ---");

            var calculatorSemaphore = new IntegralCalculator();
            var semaphoreTasks = new List<Task>();

            for (int i = 0; i < 5; i++)
            {
                int threadNum = i + 1;
                var task = Task.Run(async () =>
                {
                    await Task.Run(() =>
                    {
                        calculatorSemaphore.CalculateIntegralWithSemaphore(2, threadNum);
                    });
                });

                semaphoreTasks.Add(task);
                await Task.Delay(50); // Задержка между запуском задач
            }

            await Task.WhenAll(semaphoreTasks);
        }

        static async Task Task1_2_1_3()
        {
            // Создаем коллекцию из 1000 пациентов
            var patients = new List<Patient>();
            var diagnoses = new[] { "Грипп", "Пневмония", "Гипертония", "Диабет", "Астма", "Мигрень" };
            var random = new Random();

            for (int i = 0; i < 1000; i++)
            {
                string name = $"Пациент {i + 1}";
                int age = random.Next(18, 90);
                string diagnosis = diagnoses[random.Next(diagnoses.Length)];
                patients.Add(new Patient(i + 1, name, age, diagnosis));

                // Добавляем небольшую асинхронную задержку
                if (i % 100 == 0)
                    await Task.Yield();
            }

            Console.WriteLine($"Поток {Thread.CurrentThread.ManagedThreadId}: Начало работы");

            // Создаем сервис
            var streamService = new StreamService<Patient>();
            var progress = new Progress<string>(message => Console.WriteLine($"# {message}"));

            // Используем MemoryStream
            using (var memoryStream = new MemoryStream())
            {
                // Синхронный запуск методов 1 и 2
                Console.WriteLine($"Поток {Thread.CurrentThread.ManagedThreadId}: Запуск потоков 1 и 2");

                var task1 = Task.Run(() => streamService.WriteToStreamAsync(memoryStream, patients, progress));
                await Task.Delay(500); // Увеличиваем задержку до 500 мс
                var task2 = Task.Run(() => streamService.CopyFromStreamAsync(memoryStream, "patients.json", progress));

                // Ожидаем завершения
                await Task.WhenAll(task1, task2);

                Console.WriteLine($"\nПоток {Thread.CurrentThread.ManagedThreadId}: Потоки 1 и 2 завершены");

                // Проверяем размер файла
                var fileInfo = new FileInfo("patients.json");
                Console.WriteLine($"Размер файла patients.json: {fileInfo.Length} байт");

                // Получаем статистику асинхронно
                int countPneumonia = await streamService.GetStatisticsAsync("patients.json",
                    p => p.Diagnosis == "Пневмония");

                Console.WriteLine($"\nСтатистика: {countPneumonia} пациентов с диагнозом 'Пневмония'");

                // Дополнительная статистика для демонстрации
                int countFlu = await streamService.GetStatisticsAsync("patients.json",
                    p => p.Diagnosis == "Грипп");
                Console.WriteLine($"Статистика: {countFlu} пациентов с диагнозом 'Грипп'");

                int countHypertension = await streamService.GetStatisticsAsync("patients.json",
                    p => p.Diagnosis == "Гипертония");
                Console.WriteLine($"Статистика: {countHypertension} пациентов с диагнозом 'Гипертония'");
            }
        }
    }
}