using System;
using UnityEngine;

namespace Gameplay.Quest
{
    /// <summary>
    /// Данные задачи уровня: идентификатор, название и иконка
    /// </summary>
    [Serializable]
    public class QuestTaskData
    {
        public string Id;
        public string Title;
        public Sprite Icon;
    }
}
