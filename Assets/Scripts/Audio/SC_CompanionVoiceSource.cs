using UnityEngine; // Подключаем Unity-классы

// Вешается на объект напарника. Сообщает системе диалогов, откуда звучит его 3D-голос.
// Пока компонент активен — реплики Companion идут из этой точки.
public class SC_CompanionVoiceSource : MonoBehaviour
{
    [Tooltip("Точка, откуда звучит голос напарника (напр. кость головы). Пусто — этот объект.")]
    public Transform voiceOrigin; // Позиция голоса напарника

    private Transform Resolved => voiceOrigin != null ? voiceOrigin : transform; // Итоговая точка

    private void OnEnable() // При включении
    {
        SC_DialogueVoice.CompanionSource = Resolved; // Регистрируем напарника как источник 3D-голоса
    }

    private void OnDisable() // При выключении
    {
        if (SC_DialogueVoice.CompanionSource == Resolved) // Если всё ещё указывает на нас
        {
            SC_DialogueVoice.CompanionSource = null; // Снимаем регистрацию
        }
    }
}
