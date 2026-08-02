using Core.Presentation;
using Gameplay.Quest;
using Presentation.Elements;
using UnityEngine;

namespace Presentation.Canvases
{
    /// <summary>
    /// Блокнот с задачами уровня. Открывается/закрывается клавишей Tab
    /// </summary>
    /// <remarks>
    /// Строит строки из <see cref="QuestService"/> и при открытии разблокирует курсор.
    /// Canvas должен быть активен в сцене, чтобы слушать ввод
    /// </remarks>
    public class NotepadCanvas : CanvasBase
    {
        [Header("Bindings")]
        [SerializeField] private Transform tasksContainer;
        [SerializeField] private TaskItemView taskItemPrefab;

        [Header("Settings")]
        [SerializeField] private KeyCode toggleKey = KeyCode.Tab;
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
            Show(false);
        }

        private void Start()
        {
            BuildTaskList();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                Toggle();
        }

        private void OnDestroy()
        {
            if (isPausedByNotepad)
            {
                Time.timeScale = previousTimeScale;
                isPausedByNotepad = false;
            }
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

            Show(false);
        }

        private void BuildTaskList()
        {
            if (isInitialized) return;

            if (QuestService.Instance == null)
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

            foreach (QuestService.TaskState task in QuestService.Instance.Tasks)
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
