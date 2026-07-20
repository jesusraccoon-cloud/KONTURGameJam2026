using Core.Presentation;
using Infrastructure;
//using Gameplay;
using UnityEngine;
using UnityEngine.UI;
using R3;
using TMPro;

namespace Presentation.Canvases
{
    /// <summary>
    /// Отвечает за отображение пользовательского интерфейса во время игры
    /// </summary>
    /// <remarks>
    /// Canvas активируется только при переходе в состояние <see cref="GlobalGameState.Gameplay"/>.
    /// Подписывается на изменения глобального состояния, взаимодействий и сенсоров.
    /// </remarks>
    public class HUDCanvas : CanvasBase
    {
        // Ссылки на элементы GUI

        //[Inject] private GlobalStateService gameStateService;

        private void Awake()
        {
            //gameStateService.CurrentGameState
            //    .Subscribe(state => Show(state == GlobalGameState.Gameplay))
            //    .AddTo(this);
        }

        private void Start()
        {

        }
    }
}
