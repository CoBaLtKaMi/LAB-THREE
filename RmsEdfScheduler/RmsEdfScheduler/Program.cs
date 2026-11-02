using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace RmsEdfScheduler
{
    class Program
    {
        static void Main(string[] args)
        {
            // Включаем поддержку UTF-8 в консоли Windows для вывода блоков ██ и ░░
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // Чтение конфигурационного JSON-файла
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string dataPath = Path.Combine(baseDir, "..", "..", "..", "data", "tasks.json");

            if (!File.Exists(dataPath))
            {
                Console.WriteLine($"Ошибка! Файл задач не найден по пути: {Path.GetFullPath(dataPath)}");
                return;
            }

            string jsonString = File.ReadAllText(dataPath);
            var rawTasks = JsonSerializer.Deserialize<List<Task>>(jsonString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Правило RMS: Сортировка по возрастанию периода T (меньше период -> выше приоритет)
            var tasks = rawTasks.OrderBy(t => t.T).ToList();

            // Расчет основных системных критериев
            double u = SchedulerService.GetUtilization(tasks);
            double llBound = SchedulerService.GetLiuLaylandBound(tasks.Count);
            int hyperperiod = SchedulerService.FindHyperperiod(tasks.Select(t => t.T));

            // Вывод красивой аналитики
            Console.WriteLine(new string('=', 70));
            Console.WriteLine(" АНАЛИЗ ПЛАНИРУЕМОСТИ ПЕРИОДИЧЕСКИХ ЗАДАЧ: БОРТОВАЯ СИСТЕМА (C#)");
            Console.WriteLine(new string('=', 70));
            Console.WriteLine($"1. Загрузка процессора (U):       {u:F3} (занято {u * 100:F1}% мощностей)");
            Console.WriteLine($"2. Граница Лю-Лейланда для RMS:   {llBound:F3}");
            Console.WriteLine($"3. Математический тест RMS:      {(u <= llBound ? "ПРОЙДЕН (Загрузка в норме)" : "ТРЕБУЕТ ДОП. ПРОВЕРКИ")}");
            Console.WriteLine($"4. Математический тест EDF:      {(u <= 1.0 ? "ПРОЙДЕН (Система стабильна)" : "КРИТИЧЕСКАЯ ПЕРЕГРУЗКА")}");
            Console.WriteLine($"5. Длина цикла (Гиперпериод):    {hyperperiod} мс");
            Console.WriteLine(new string('-', 70));

            Console.WriteLine($"{"Название задачи",-18} | {"C",-2} | {"T",-2} | {"D",-2} | {"R (Отклик)",-10} | {"Результат"}");
            Console.WriteLine(new string('-', 70));

            bool allMeet = true;
            for (int i = 0; i < tasks.Count; i++)
            {
                var higherTasks = tasks.Take(i).ToList();
                int rTime = SchedulerService.CalculateResponseTime(tasks[i], higherTasks);
                bool meets = rTime <= tasks[i].D;
                if (!meets) allMeet = false;

                string statusStr = meets ? "Укладывается" : "ОПОЗДАНИЕ!";
                Console.WriteLine($"{tasks[i].Name,-18} | {tasks[i].C,-2} | {tasks[i].T,-2} | {tasks[i].D,-2} | {rTime,-10} | {statusStr}");
            }

            Console.WriteLine(new string('-', 70));
            Console.WriteLine($"ИТОГ АНАЛИЗА: СИСТЕМА {(allMeet ? "АБСОЛЮТНО ПЛАНИРУЕМА" : "НЕ ПЛАНИРУЕМА")}");

            // Отрисовка графической диаграммы Ганта
            var (chart, idleTimeline) = SchedulerService.SimulateGantt(tasks, hyperperiod);

            Console.WriteLine("\n" + new string('=', 70));
            Console.WriteLine(" ВИЗУАЛЬНОЕ РАСПИСАНИЕ РАБОТЫ ПРОЦЕССОРА (ДИАГРАММА ГАНТА)");
            Console.WriteLine(new string('=', 70));
            Console.WriteLine("Символы на графике:  ██ — выполнение задачи  ░░ — простой (IDLE)");
            Console.WriteLine(new string('-', 70));

            string timeScale = string.Concat(Enumerable.Range(0, hyperperiod).Select(t => $"{t,-3}"));
            Console.WriteLine($"{"Время (мс)",-18} | {timeScale}");
            Console.WriteLine(new string('-', 21 + hyperperiod * 3));

            foreach (var task in tasks)
            {
                string rowVisual = string.Join(" ", chart[task.Name]);
                Console.WriteLine($"{task.Name,-18} | {rowVisual}");
            }

            string idleVisual = string.Join(" ", idleTimeline);
            Console.WriteLine($"{"Простой (IDLE)",-18} | {idleVisual}");
            Console.WriteLine(new string('=', 70));

            Console.WriteLine("\nНажмите любую клавишу для завершения работы...");
            Console.ReadKey();
        }
    }
}
