import os
import json
import math
from dataclasses import dataclass
from typing import List

@dataclass
class Task:
    name: str
    C: int  # Время выполнения
    T: int  # Период
    D: int  # Дедлайн

def utilization(tasks: List[Task]) -> float:
    """Вычисляет коэффициент загрузки процессора U."""
    return sum(task.C / task.T for task in tasks)

def liu_layland_bound(n: int) -> float:
    """Вычисляет теоретическую границу Лю-Лейланда."""
    if n <= 0:
        return 0.0
    return n * (2 ** (1 / n) - 1)

def rms_schedulable(tasks: List[Task]) -> bool:
    """Проверяет утилизационный тест Лю-Лейланда для RMS."""
    return utilization(tasks) <= liu_layland_bound(len(tasks))

def edf_schedulable(tasks: List[Task]) -> bool:
    """Проверяет планируемость по EDF."""
    return utilization(tasks) <= 1.0

def response_time(task: Task, higher_priority_tasks: List[Task]) -> int:
    """Вычисляет время отклика задачи методом итераций (RTA)."""
    r = task.C
    while True:
        interference = sum(math.ceil(r / hp_task.T) * hp_task.C for hp_task in higher_priority_tasks)
        next_r = task.C + interference
        
        if next_r > task.T:
            return next_r
        if next_r == r:
            return r
        r = next_r

def gantt(tasks: List[Task], interval: int):
    """Строит понятную и наглядную графическую диаграмму выполнения."""
    print("\n" + "="*70)
    print(" ВИЗУАЛЬНОЕ РАСПИСАНИЕ РАБОТЫ ПРОЦЕССОРА (ДИАГРАММА ГАНТА)")
    print("="*70)
    print("Символы на графике:  ██ — выполнение задачи  ░░ — простой (IDLE)")
    print("-"*70)
    
    num_tasks = len(tasks)
    current_c = [0] * num_tasks
    
    # Инициализируем хронологию для каждой задачи и для простоя
    chart = {task.name: [] for task in tasks}
    idle_timeline = []
    
    # Моделируем такты процессора
    for t in range(interval):
        # Проверяем появление новых периодов задач
        for i, task in enumerate(tasks):
            if t % task.T == 0:
                current_c[i] = task.C
        
        # Выбираем задачу с наивысшим приоритетом
        executed_task_idx = -1
        for i in range(num_tasks):
            if current_c[i] > 0:
                executed_task_idx = i
                break
        
        # Заполняем графическую сетку для текущего такта
        for i, task in enumerate(tasks):
            if i == executed_task_idx:
                chart[task.name].append("██")
            else:
                chart[task.name].append("  ")
                
        if executed_task_idx != -1:
            current_c[executed_task_idx] -= 1
            idle_timeline.append("  ")
        else:
            idle_timeline.append("░░")

    # Печатаем шкалу времени
    time_scale = "".join(f"{t:<3}" for t in range(interval))
    print(f"{'Время (мс)':<18} | {time_scale}")
    print("-" * (21 + interval * 3))
    
    # Выводим строки для каждой задачи
    for task_name, timeline in chart.items():
        row_visual = " ".join(timeline)
        print(f"{task_name:<18} | {row_visual}")
        
    # Выводим строку простоя
    idle_visual = " ".join(idle_timeline)
    print(f"{'Простой (IDLE)':<18} | {idle_visual}")
    print("="*70)

def main():
    current_dir = os.path.dirname(os.path.abspath(__file__))
    data_path = os.path.join(current_dir, '..', 'data', 'tasks.json')
    
    if not os.path.exists(data_path):
        print(f"Ошибка! Файл не найден: {data_path}")
        return

    with open(data_path, 'r', encoding='utf-8') as f:
        data = json.load(f)
    
    raw_tasks = [Task(name=item['name'], C=item['C'], T=item['T'], D=item['D']) for item in data]
    
    # RMS автоматическая сортировка: меньший период -> выше приоритет
    tasks = sorted(raw_tasks, key=lambda x: x.T)
    
    u = utilization(tasks)
    ll_bound = liu_layland_bound(len(tasks))
    hyperperiod = math.lcm(*[t.T for t in tasks])
    
    print("="*70)
    print(" АНАЛИЗ ПЛАНИРУЕМОСТИ ПЕРИОДИЧЕСКИХ ЗАДАЧ: БОРТОВАЯ СИСТЕМА")
    print("="*70)
    print(f"1. Загрузка процессора (U):       {u:.3f} (используется {u*100:.1f}% мощности)")
    print(f"2. Граница Лю-Лейланда для RMS:   {ll_bound:.3f}")
    print(f"3. Математический тест RMS:      {'ПРОЙДЕН (Загрузка в норме)' if u <= ll_bound else 'ТРЕБУЕТ ДОП. ПРОВЕРКИ'}")
    print(f"4. Математический тест EDF:      {'ПРОЙДЕН (Система стабильна)' if u <= 1.0 else 'КРИТИЧЕСКАЯ ПЕРЕГРУЗКА'}")
    print(f"5. Длина цикла (Гиперпериод):    {hyperperiod} мс")
    print("-"*70)
    
    print(f"{'Название задачи':<18} | {'C':<2} | {'T':<2} | {'D':<2} | {'R (Отклик)':<10} | {'Результат'}")
    print("-"*70)
    
    all_meet = True
    for i, task in enumerate(tasks):
        higher_tasks = tasks[:i]
        r_time = response_time(task, higher_tasks)
        meets = r_time <= task.D
        if not meets:
            all_meet = False
        status_str = "Укладывается" if meets else "ОПОЗДАНИЕ!"
        print(f"{task.name:<18} | {task.C:<2} | {task.T:<2} | {task.D:<2} | {r_time:<10} | {status_str}")
        
    print("-"*70)
    print(f"ИТОГ АНАЛИЗА: СИСТЕМА {'АБСОЛЮТНО ПЛАНИРУЕМА' if all_meet else 'НЕ ПЛАНИРУЕМА (ЕСТЬ СБОИ)'}")
    
    gantt(tasks, hyperperiod)

if __name__ == "__main__":
    main()
