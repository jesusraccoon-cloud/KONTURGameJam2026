using Core.Presentation;
//using Infrastructure;
//using Infrastructure.Abstactions;
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
            //Show(stateService.CurrentGameState.CurrentValue == GameState.Paused);
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
