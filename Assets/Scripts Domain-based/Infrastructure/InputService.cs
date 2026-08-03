using System;
using Core;
using R3;
using UnityEngine;

namespace Infrastructure
{
    /// <summary>
    /// Сервис чтения ввода игрока для GUI-канвасов
    /// </summary>
    /// <remarks>
    /// Единственный компонент, который опрашивает Input в Update и отдаёт нажатия
    /// через R3: семантические события (блокнот, пауза) и общий поток клавиш.
    /// Клавиши настраиваются в Inspector
    /// </remarks>
    public class InputService : ServiceBase<InputService>
    {
        [Header("Keys")]
        [SerializeField] private KeyCode notepadToggleKey = KeyCode.Tab;
        [SerializeField] private KeyCode pauseToggleKey = KeyCode.Escape;

        private static readonly KeyCode[] allKeys = (KeyCode[])Enum.GetValues(typeof(KeyCode));

        private bool isPauseMenuOpen;
        private bool isNotepadOpen;

        private readonly Subject<Unit> notepadToggleRequested = new();
        private readonly Subject<Unit> pauseRequested = new();
        private readonly Subject<KeyCode> keyPressed = new();

        public Observable<Unit> NotepadToggleRequested => notepadToggleRequested;
        public Observable<Unit> PauseRequested => pauseRequested;
        public Observable<KeyCode> KeyPressed => keyPressed;

        public void SetPauseMenuOpen(bool isOpen) => isPauseMenuOpen = isOpen;

        public void SetNotepadOpen(bool isOpen) => isNotepadOpen = isOpen;

        public override void Startup()
        {
            this.status.Value = LifecycleState.Started;
        }

        private void Update()
        {
            if (!isPauseMenuOpen && Input.GetKeyDown(notepadToggleKey))
                notepadToggleRequested.OnNext(Unit.Default);

            if (!isNotepadOpen && Input.GetKeyDown(pauseToggleKey))
                pauseRequested.OnNext(Unit.Default);

            if (Input.anyKeyDown)
            {
                foreach (KeyCode code in allKeys)
                {
                    if (Input.GetKeyDown(code))
                        keyPressed.OnNext(code);
                }
            }
        }
    }
}
