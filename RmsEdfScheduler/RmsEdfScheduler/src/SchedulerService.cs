using System;
using System.Collections.Generic;
using System.Linq;

namespace RmsEdfScheduler
{
    public static class SchedulerService
    {
        // Вычисление НОК (Наименьшего Общего Кратного) для гиперпериода
        private static int GCD(int a, int b) => b == 0 ? a : GCD(b, a % b);
        private static int LCM(int a, int b) => (a * b) / GCD(a, b);
        public static int FindHyperperiod(IEnumerable<int> numbers) => numbers.Aggregate(LCM);

        // Вычисление коэффициента загрузки процессора U = Σ (C_i / T_i)
        public static double GetUtilization(List<Task> tasks)
            => tasks.Sum(t => (double)t.C / t.T);

        // Теоретическая граница Лю-Лейланда для RMS
        public static double GetLiuLaylandBound(int n)
            => n <= 0 ? 0.0 : n * (Math.Pow(2, 1.0 / n) - 1);

        // Анализ времени отклика задачи методом итераций (RTA)
        public static int CalculateResponseTime(Task task, List<Task> higherPriorityTasks)
        {
            int r = task.C;
            while (true)
            {
                int interference = 0;
                foreach (var hpTask in higherPriorityTasks)
                {
                    interference += (int)Math.Ceiling((double)r / hpTask.T) * hpTask.C;
                }

                int nextR = task.C + interference;

                if (nextR > task.T) return nextR; // Выход за рамки дедлайна
                if (nextR == r) return r;         // Время стабилизировалось

                r = nextR;
            }
        }

        // Симуляция планировщика по тактам (построение диаграммы Ганта)
        public static (Dictionary<string, List<string>> Chart, List<string> IdleTimeline) SimulateGantt(List<Task> tasks, int interval)
        {
            int numTasks = tasks.Count;
            int[] currentC = new int[numTasks];

            var chart = tasks.ToDictionary(t => t.Name, _ => new List<string>());
            var idleTimeline = new List<string>();

            for (int t = 0; t < interval; t++)
            {
                // а) Появление новых заявок в начале периодов
                for (int i = 0; i < numTasks; i++)
                {
                    if (t % tasks[i].T == 0) currentC[i] = tasks[i].C;
                }

                // б) Выбор активной задачи с наивысшим RMS приоритетом
                int executedTaskIdx = -1;
                for (int i = 0; i < numTasks; i++)
                {
                    if (currentC[i] > 0)
                    {
                        executedTaskIdx = i;
                        break;
                    }
                }

                // в) Отрисовка текущего такта в матрицу графика
                for (int i = 0; i < numTasks; i++)
                {
                    chart[tasks[i].Name].Add(i == executedTaskIdx ? "██" : "  ");
                }

                if (executedTaskIdx != -1)
                {
                    currentC[executedTaskIdx]--;
                    idleTimeline.Add("  ");
                }
                else
                {
                    idleTimeline.Add("░░"); // Процессор пустует
                }
            }

            return (chart, idleTimeline);
        }
    }
}
