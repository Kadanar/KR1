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
            await Task.Run(() => Task1_1());

            Console.WriteLine("\n=== ЗАДАНИЕ 1.2 и 1.3 ===");
            Task1_2_1_3(); // Синхронный метод

            Console.WriteLine("\nНажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        static void Task1_1()
        {
            // Часть 4: Запуск в отдельном потоке
            Console.WriteLine("--- Запуск в отдельном потоке ---");
            var calculator1 = new IntegralCalculator();

            calculator1.ProgressChanged += (progress, threadId) =>
            {
                Console.WriteLine($"Поток {threadId}: Прогресс: {progress:F2}%");
            };

            Thread thread1 = new Thread(() =>
            {
                calculator1.CalculationCompleted += (result, ticks, threadId) =>
                {
                    Console.WriteLine($"\nПоток {threadId}: Завершен с результатом: {result:F6}");
                    Console.WriteLine($"Время выполнения: {ticks} тиков");
                };
                calculator1.CalculateIntegral(1);
            });

            thread1.Start();
            thread1.Join();

            // Часть 4: Два потока с разными приоритетами
            Console.WriteLine("\n--- Два потока с разными приоритетами ---");

            // Использую ОДИН экземпляр для демонстрации приоритетов
            var calculator = new IntegralCalculator();

            long highTicks = 0, lowTicks = 0;
            double highResult = 0, lowResult = 0;
            int highThreadId = 0, lowThreadId = 0;

            calculator.ProgressChanged += (progress, threadId) =>
            {
                if (threadId == 2)
                    Console.WriteLine($"Поток HIGH приоритета: Прогресс: {progress:F2}%");
                else if (threadId == 3)
                    Console.WriteLine($"Поток LOW приоритета: Прогресс: {progress:F2}%");
            };

            Thread threadHigh = new Thread(() =>
            {
                calculator.CalculationCompleted += (result, ticks, threadId) =>
                {
                    highResult = result;
                    highTicks = ticks;
                    highThreadId = threadId;
                };
                calculator.CalculateIntegral(2);
            });

            Thread threadLow = new Thread(() =>
            {
                calculator.CalculationCompleted += (result, ticks, threadId) =>
                {
                    lowResult = result;
                    lowTicks = ticks;
                    lowThreadId = threadId;
                };
                calculator.CalculateIntegral(3);
            });

            threadHigh.Priority = ThreadPriority.Highest;
            threadLow.Priority = ThreadPriority.Lowest;

            threadHigh.Start();
            threadLow.Start();

            threadHigh.Join();
            threadLow.Join();

            Console.WriteLine($"\nПоток HIGH приоритета {highThreadId}: Результат = {highResult:F6}, Время = {highTicks} тиков");
            Console.WriteLine($"Поток LOW приоритета {lowThreadId}: Результат = {lowResult:F6}, Время = {lowTicks} тиков");

            // Часть 5: Только один поток из 5
            Console.WriteLine("\n--- Только один поток из 5 (с lock) ---");

            var calculatorLock = new IntegralCalculator();
            List<Thread> threads = new List<Thread>();

            for (int i = 0; i < 5; i++)
            {
                int threadNum = i + 1;
                Thread t = new Thread(() =>
                {
                    Console.WriteLine($"Поток {Thread.CurrentThread.ManagedThreadId} начал выполнение");
                    calculatorLock.CalculateIntegralWithLock(threadNum);
                    Console.WriteLine($"Поток {Thread.CurrentThread.ManagedThreadId} завершил выполнение");
                });
                threads.Add(t);
            }

            foreach (var thread in threads)
            {
                thread.Start();
                Thread.Sleep(10);
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }

            // Часть 6: Семафор - ограниченное количество потоков
            Console.WriteLine("\n--- Семафор: только 2 потока из 5 ---");

            var calculatorSemaphore = new IntegralCalculator();
            threads.Clear();

            for (int i = 0; i < 5; i++)
            {
                int threadNum = i + 1;
                Thread t = new Thread(() =>
                {
                    Console.WriteLine($"Поток {Thread.CurrentThread.ManagedThreadId} ожидает семафор...");
                    calculatorSemaphore.CalculateIntegralWithSemaphore(2, threadNum);
                    Console.WriteLine($"Поток {Thread.CurrentThread.ManagedThreadId} завершил выполнение");
                });
                threads.Add(t);
            }

            foreach (var thread in threads)
            {
                thread.Start();
                Thread.Sleep(50);
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }
        }

        static void Task1_2_1_3()
        {
            // Создаю коллекцию из 1000 пациентов
            var patients = new List<Patient>();
            var diagnoses = new[] { "Грипп", "Пневмония", "Гипертония", "Диабет", "Астма", "Мигрень" };
            var random = new Random();

            for (int i = 0; i < 1000; i++)
            {
                string name = $"Пациент {i + 1}";
                int age = random.Next(18, 90);
                string diagnosis = diagnoses[random.Next(diagnoses.Length)];
                patients.Add(new Patient(i + 1, name, age, diagnosis));
            }

            Console.WriteLine($"Поток {Thread.CurrentThread.ManagedThreadId}: Начало работы");

            // Создаю сервис
            var streamService = new StreamService<Patient>();
            var progress = new Progress<string>(message => Console.WriteLine($"# {message}"));

            // Использую MemoryStream
            using (var memoryStream = new MemoryStream())
            {
                //синхронный запуск методов 1 и 2
                Console.WriteLine($"Поток {Thread.CurrentThread.ManagedThreadId}: Запуск потоков 1 и 2");

                // Метод 1: Запись в поток (синхронный запуск через Task.Run)
                var task1 = Task.Run(() => streamService.WriteToStreamAsync(memoryStream, patients, progress));

                // Задержка 100-200 мс между запусками (СИНХРОННАЯ)
                Thread.Sleep(200);

                // Метод 2: Копирование из потока в файл (синхронный запуск через Task.Run)
                var task2 = Task.Run(() => streamService.CopyFromStreamAsync(memoryStream, "patients.json", progress));

                //ожидание завершения
                Console.WriteLine($"\nПоток {Thread.CurrentThread.ManagedThreadId}: Ожидание завершения методов 1 и 2...");

                task1.Wait(); // Синхронное ожидание
                task2.Wait(); // Синхронное ожидание

                Console.WriteLine($"Поток {Thread.CurrentThread.ManagedThreadId}: Потоки 1 и 2 завершены");

                // Проверяю размер файла
                var fileInfo = new FileInfo("patients.json");
                Console.WriteLine($"Размер файла patients.json: {fileInfo.Length} байт");

                // Получаю статистику асинхронно
                Console.WriteLine($"\nПоток {Thread.CurrentThread.ManagedThreadId}: Получение статистики...");

                // Использую GetAwaiter().GetResult() для синхронного получения результата
                int countPneumonia = streamService.GetStatisticsAsync("patients.json",
                    p => p.Diagnosis == "Пневмония").GetAwaiter().GetResult();

                Console.WriteLine($"Статистика: {countPneumonia} пациентов с диагнозом 'Пневмония'");

                // Дополнительная статистика для демонстрации
                int countFlu = streamService.GetStatisticsAsync("patients.json",
                    p => p.Diagnosis == "Грипп").GetAwaiter().GetResult();
                Console.WriteLine($"Статистика: {countFlu} пациентов с диагнозом 'Грипп'");

                int countHypertension = streamService.GetStatisticsAsync("patients.json",
                    p => p.Diagnosis == "Гипертония").GetAwaiter().GetResult();
                Console.WriteLine($"Статистика: {countHypertension} пациентов с диагнозом 'Гипертония'");
            }
        }
    }
}