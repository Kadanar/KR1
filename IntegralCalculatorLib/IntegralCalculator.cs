using System;
using System.Diagnostics;
using System.Threading;

namespace IntegralCalculatorLib
{
    public class IntegralCalculator
    {
        public delegate void CalculationCompletedHandler(double result, long elapsedTicks, int threadId);
        public delegate void ProgressChangedHandler(double progress, int threadId);

        public event CalculationCompletedHandler? CalculationCompleted;
        public event ProgressChangedHandler? ProgressChanged;

        private static readonly object _lockObject = new object();
        private static SemaphoreSlim? _semaphore;

        public void CalculateIntegral(int threadId = 0)
        {
            var stopwatch = Stopwatch.StartNew();
            double a = 0.0;
            double b = 1.0;
            double step = 0.000001; // Для тестирования. Для точности тут надо вернуть 0.00000001 и будет как у вас в задании
            int totalIterations = (int)((b - a) / step);
            double sum = 0.0;

            if (totalIterations < 10) totalIterations = 10;

            for (int i = 0; i < totalIterations; i++)
            {
                double x = a + i * step;
                sum += Math.Sin(x) * step;

                // Искусственная задержка
                for (int j = 0; j < 1000; j++)
                {
                    double dummy = j * 1.0;
                    if (dummy > 1000000)
                    {
                        sum += 0.0000000001;
                    }
                }

                // Отчет о прогрессе каждые 10% если бы сделал 1%, то слишком летала консоль
                if (i % (totalIterations / 10) == 0)
                {
                    double progress = (double)i / totalIterations * 100;
                    ProgressChanged?.Invoke(progress, threadId);
                }
            }

            stopwatch.Stop();
            CalculationCompleted?.Invoke(sum, stopwatch.ElapsedTicks, threadId);
        }

        public void CalculateIntegralWithLock(int threadId = 0)
        {
            lock (_lockObject)
            {
                Console.WriteLine($"Поток {Thread.CurrentThread.ManagedThreadId} получил lock");
                CalculateIntegral(threadId);
            }
        }

        public void CalculateIntegralWithSemaphore(int maxConcurrent, int threadId = 0)
        {
            // Ленивая инициализация семафора
            if (_semaphore == null)
            {
                lock (_lockObject)
                {
                    _semaphore ??= new SemaphoreSlim(maxConcurrent, maxConcurrent);
                }
            }

            Console.WriteLine($"Поток {Thread.CurrentThread.ManagedThreadId} ожидает семафор...");
            _semaphore!.Wait(); // ! - мы уверены, что семафор не null
            try
            {
                Console.WriteLine($"Поток {Thread.CurrentThread.ManagedThreadId} получил семафор");
                CalculateIntegral(threadId);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}