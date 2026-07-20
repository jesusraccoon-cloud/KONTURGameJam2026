using Core.Presentation;
//using Infrastructure;
using UnityEngine;
using R3;
using UnityEngine.SceneManagement;

namespace Presentation.Canvases
{
    /// <summary>
    /// Отвечает за отображение пользовательского интерфейса до игры
    /// </summary>
    /// <remarks>
    /// Canvas активируется только при переходе в состояние <see cref="GlobalGameState.PreGame"/>.
    /// Подписывается на изменения глобального состояния.
    /// </remarks>
    public class MMScreen : ScreenBase
    {
        //[Inject] private SceneService sceneLoadingService;
        //[Inject] private GlobalStateService gameStateService;

        protected override void Awake()
        {
            base.Awake();

            //gameStateService.CurrentGameState
            //    .Subscribe(state => Show(state == GlobalGameState.PreGame))
            //    .AddTo(this);
        }

        public void LoadSceneAsync(string sceneName)
        {
            //sceneLoadingService.GoToGameScene(sceneName);
            SceneManager.LoadSceneAsync(sceneName);
        }

        public void OnExitButton()
        {
            Application.Quit();
        }
    }
}
