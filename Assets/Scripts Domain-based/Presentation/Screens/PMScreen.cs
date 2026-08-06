using Core.Presentation;
using Infrastructure;
using R3;
//using Infrastructure;
//using Infrastructure.Abstactions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Presentation.Screens
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
    public class PMScreen : ScreenBase
    {
        //[Inject] private IGameService gameService;
        //[Inject] private GameStateService stateService;

        protected override void Awake()
        {
            base.Awake();

            //stateService.CurrentGameState
            //    .Subscribe(state => Show(state == GameState.Paused))
            //    .AddTo(this);
        }

        private void OnEnable()
        {
            Time.timeScale = 0f;
        }

        private void OnDisable()
        {
            Time.timeScale = 1f;
        }


        private void Start()
        {
            //Show(stateService.CurrentGameState.CurrentValue == GameState.Paused);
        }

        public void Resume()
        {
            //gameService?.Resume();

            if (InputManager.Instance != null)
                InputManager.Instance.SetPauseMenuOpen(false);

            this.gameObject.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
        }

        public void Restart()
        {
            //gameService?.Restart();
            SceneManager.LoadSceneAsync("copy rotation khruchevka");
        }

        public void Exit()
        {
            //gameService?.Exit();
            SceneManager.LoadSceneAsync("Main Menu");
        }
    }
}
