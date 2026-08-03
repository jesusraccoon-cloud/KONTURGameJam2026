using Core.Presentation;
using Infrastructure;
using UnityEngine;

using R3;

namespace Presentation.Canvases
{
    /// <summary>
    /// Отвечает за отображение экрана паузы во время паузы игры
    /// </summary>
    /// <remarks>
    /// Canvas активируется только при переходе в состояние <see cref="GameState.Paused"/>.
    /// Подписывается на изменения состояния игры.
    /// Использует <see cref="IGameService"/> для возобновления, рестарта и выхода 
    /// из состояния <see cref="GlobalGameState.Gameplay"/> в <see cref="GlobalGameState.PreGame"/>
    /// </remarks>
    public class PMCanvas : CanvasBase
    {
        [Header("Bindings")]
        [Tooltip("Первый экран включается по-умолчанию")]
        [SerializeField] private ScreenBase[] screens;

        //[Inject] private IGameService gameService;
        //[Inject] private GameStateService stateService;

        private void Awake()
        {
            //stateService.CurrentGameState
            //    .Subscribe(state =>
            //    {
            //        Show(state == GameState.Paused);

            //        if (state != GameState.Paused && screens.Length > 0) // TODO: не оптимально будет производить вычисления при каждом чихе
            //        {
            //            foreach (var screen in screens)
            //                screen.gameObject.SetActive(false);

            //            screens[0].gameObject.SetActive(true);
            //        }
            //    })
            //    .AddTo(this);
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            SubscribeToInput();
        }

        private void SubscribeToInput()
        {
            if (InputService.Instance == null)
            {
                Debug.LogWarning("PMCanvas: InputService не найден в сцене", this);
                return;
            }

            InputService.Instance.PauseRequested
                .Subscribe(_ => TogglePauseScreen())
                .AddTo(this);
        }

        private void TogglePauseScreen()
        {
            if (screens == null || screens.Length == 0) return;

            bool isOpen = !screens[0].gameObject.activeInHierarchy;
            screens[0].gameObject.SetActive(isOpen);

            InputService.Instance?.SetPauseMenuOpen(isOpen);

            if (Cursor.lockState == CursorLockMode.Locked)
                Cursor.lockState = CursorLockMode.None;
            else
                Cursor.lockState = CursorLockMode.Locked;
        }

        private void OnDestroy()
        {
            if (InputService.Instance != null)
                InputService.Instance.SetPauseMenuOpen(false);
        }

        public void Resume()
        {
            //gameService?.Resume();
        }

        public void RestartCurrentScene()
        {
            //gameService?.Restart();
        }

        public void Exit()
        {
            //gameService?.Exit();
        }
    }
}
