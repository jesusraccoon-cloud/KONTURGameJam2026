using System.Collections.Generic;
using Core;
using R3;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gameplay.Quest
{
    /// <summary>
    /// Сервис задач уровня: хранит список задач и их состояние выполнения
    /// </summary>
    /// <remarks>
    /// Задачи настраиваются в Inspector (taskTemplates). Выполнение отмечается из игрового кода
    /// через <see cref="CompleteTask"/>. При загрузке новой сцены состояние сбрасывается,
    /// так как сервис живёт между сценами (MonoSingleton)
    /// </remarks>
    public class QuestService : ServiceBase<QuestService>
    {
        [Header("Tasks")]
        [Tooltip("Шаблоны задач уровня. Заполняется в Inspector")]
        [SerializeField] private List<QuestTaskData> taskTemplates = new();

        private readonly List<TaskState> tasks = new();

        public IReadOnlyList<TaskState> Tasks => tasks;

        /// <summary>
        /// Состояние одной задачи
        /// </summary>
        public class TaskState
        {
            public QuestTaskData Data { get; }
            public ReadOnlyReactiveProperty<bool> IsCompleted { get; }
            private readonly ReactiveProperty<bool> completed = new(false);

            internal TaskState(QuestTaskData data)
            {
                Data = data;
                IsCompleted = completed.ToReadOnlyReactiveProperty();
            }

            internal void SetCompleted(bool value) => completed.Value = value;
        }

        public override void Startup()
        {
            ResetAllTasks();
            SceneManager.sceneLoaded += OnSceneLoaded;
            this.status.Value = LifecycleState.Started;
        }

        protected override void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            base.OnDestroy();
        }

        /// <summary>
        /// Отметить задачу выполненной. Повторные вызовы игнорируются
        /// </summary>
        public void CompleteTask(string taskId)
        {
            if (string.IsNullOrEmpty(taskId)) return;

            foreach (TaskState task in tasks)
            {
                if (task.Data == null) continue;
                if (task.Data.Id != taskId) continue;
                if (task.IsCompleted.CurrentValue) return;

                task.SetCompleted(true);
                return;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ResetAllTasks();
        }

        private void ResetAllTasks()
        {
            tasks.Clear();

            foreach (QuestTaskData template in taskTemplates)
            {
                if (template == null) continue;

                tasks.Add(new TaskState(template));
            }
        }
    }
}
