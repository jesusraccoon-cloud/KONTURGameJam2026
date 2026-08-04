using Core.Presentation;
using Gameplay.Quest;
using Infrastructure;
using Presentation.Elements;
using UnityEngine;
using R3;

namespace Presentation.Canvases
{
    /// <summary>
    /// Блокнот с задачами уровня. Открывается/закрывается клавишей Tab через <see cref="InputManager"/>
    /// </summary>
    /// <remarks>
    /// Строит строки из <see cref="QuestManager"/> и при открытии разблокирует курсор.
    /// Подписывается на событие ввода из <see cref="InputManager"/> через R3
    /// </remarks>
    public class NotepadCanvas : CanvasBase
    {
        [Header("Bindings")]
        [SerializeField] private Transform tasksContainer;
        [SerializeField] private TaskItemView taskItemPrefab;

        [Header("Settings")]
        [SerializeField] private bool pauseGameWhileOpen = true;
        [Tooltip("Объекты, которые выключаются при открытом блокноте (например, корень игрока)")]
        [SerializeField] private GameObject[] objectsToDisableWhileOpen;

        private CursorLockMode previousCursorMode = CursorLockMode.Locked;
        private bool previousCursorVisible;
        private float previousTimeScale = 1f;
        private bool isPausedByNotepad;
        private bool isInitialized;

        private void Awake()
        {
            
        }

        private void Start()
        {
            BuildTaskList();
            SubscribeToInput();

            Show(false);
        }

        private void OnDestroy()
        {
            if (InputManager.Instance != null)
                InputManager.Instance.SetNotepadOpen(false);

            if (isPausedByNotepad)
            {
                Time.timeScale = previousTimeScale;
                isPausedByNotepad = false;
            }
        }

        private void SubscribeToInput()
        {
            if (InputManager.Instance == null)
            {
                Debug.LogWarning("NotepadCanvas: InputService не найден в сцене", this);
                return;
            }

            InputManager.Instance.NotepadToggleRequested
                .Subscribe(_ => Toggle())
                .AddTo(this);
        }

        public void Toggle()
        {
            if (IsVisible)
                CloseNotepad();
            else
                OpenNotepad();
        }

        private void OpenNotepad()
        {
            previousCursorMode = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (pauseGameWhileOpen && !Mathf.Approximately(Time.timeScale, 0f))
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                isPausedByNotepad = true;
            }

            SetObjectsActive(objectsToDisableWhileOpen, false);

            InputManager.Instance?.SetNotepadOpen(true);

            Show(true);
        }

        private void CloseNotepad()
        {
            Cursor.lockState = previousCursorMode;
            Cursor.visible = previousCursorVisible;

            if (isPausedByNotepad)
            {
                Time.timeScale = previousTimeScale;
                isPausedByNotepad = false;
            }

            SetObjectsActive(objectsToDisableWhileOpen, true);

            InputManager.Instance?.SetNotepadOpen(false);

            Show(false);
        }

        private void BuildTaskList()
        {
            if (isInitialized) return;

            if (QuestManager.Instance == null)
            {
                Debug.LogWarning("NotepadCanvas: QuestService не назначен", this);
                return;
            }

            if (tasksContainer == null || taskItemPrefab == null)
            {
                Debug.LogWarning("NotepadCanvas: tasksContainer или taskItemPrefab не назначены", this);
                return;
            }

            for (int i = tasksContainer.childCount - 1; i >= 0; i--)
                Destroy(tasksContainer.GetChild(i).gameObject);

            foreach (QuestManager.TaskState task in QuestManager.Instance.Tasks)
            {
                TaskItemView item = Instantiate(taskItemPrefab, tasksContainer);
                item.Setup(task);
            }

            isInitialized = true;
        }

        private void SetObjectsActive(GameObject[] objects, bool isActive)
        {
            if (objects == null) return;

            foreach (GameObject go in objects)
            {
                if (go != null)
                    go.SetActive(isActive);
            }
        }
    }
}
