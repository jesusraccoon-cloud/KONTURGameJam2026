using UnityEngine; // Подключаем Unity-классы
using FMODUnity; // Подключаем FMOD (EventReference, RuntimeManager)
using FMOD.Studio; // Подключаем EventInstance

// Голос одной реплики диалога. Вешается на тот же объект, что и DialogueTextUI.
// Сам подписывается на показ реплики и играет FMOD-событие:
//  - Hero      -> 2D (голос главного героя «в голове», событие должно быть 2D);
//  - Companion -> 3D из позиции напарника (её задаёт SC_CompanionVoiceSource).
// Монофония: новая реплика обрывает предыдущий голос (одна реплика за раз).
[RequireComponent(typeof(DialogueTextUI))]
public class SC_DialogueVoice : MonoBehaviour
{
    public enum DialogueSpeaker { Hero, Companion } // Кто говорит

    [Header("Voice")] // Блок голоса
    public EventReference voiceEvent; // FMOD-событие реплики

    public DialogueSpeaker speaker = DialogueSpeaker.Hero; // Герой (2D) или напарник (3D)

    [Header("Debug")] // Отладка
    public bool showDebugLogs = false; // Показывать логи

    public static Transform CompanionSource; // Откуда звучит напарник (ставит SC_CompanionVoiceSource)

    private static EventInstance currentVoice; // Текущий играющий голос (общий — монофония)

    private static bool hasVoice; // Есть ли активный голос

    private DialogueTextUI line; // Реплика-субтитр, к которой привязан голос

    private void Awake() // При создании
    {
        line = GetComponent<DialogueTextUI>(); // Берём реплику с этого же объекта
    }

    private void OnEnable() // При включении
    {
        if (line != null) line.Shown += Play; // Подписываемся на показ реплики

        Debug.Log($"[DLG VOICE] {gameObject.name}: подписка на реплику, line={(line != null)}"); // ВРЕМЕННО безусловно
    }

    private void OnDisable() // При выключении
    {
        if (line != null) line.Shown -= Play; // Отписываемся
    }

    public void Play() // Проиграть голос реплики (вызывается при показе субтитра)
    {
        Debug.Log($"[DLG VOICE] {gameObject.name}: Play() speaker={speaker}, companionSrc={(CompanionSource != null)}"); // ВРЕМЕННО безусловно

        if (voiceEvent.IsNull) // Если событие не назначено
        {
            Debug.LogWarning($"[DLG VOICE] {gameObject.name}: НЕ назначено Voice Event"); // ВРЕМЕННО безусловно
            return; // Выходим
        }

        StopCurrentVoice(); // Монофония: глушим предыдущую реплику

        EventInstance inst = RuntimeManager.CreateInstance(voiceEvent); // Создаём экземпляр

        if (speaker == DialogueSpeaker.Companion) // Напарник — 3D
        {
            if (CompanionSource != null) // Если известна позиция напарника
            {
                RuntimeManager.AttachInstanceToGameObject(inst, CompanionSource.gameObject); // Голос идёт из напарника (следует за ним)
            }
            else if (showDebugLogs) // Напарника нет
            {
                Debug.LogWarning(gameObject.name + ": SC_DialogueVoice — не задан CompanionSource, реплика напарника сыграет 2D"); // Предупреждение
            }
        }
        // Hero — 2D: 3D-атрибуты не ставим (событие должно быть 2D)

        inst.start(); // Запускаем
        inst.release(); // Освободится сам, когда доиграет; хендл валиден, пока играет

        currentVoice = inst; // Запоминаем как текущий голос
        hasVoice = true; // Голос активен

        Debug.Log($"[DLG VOICE] {gameObject.name}: сыграл реплику speaker={speaker}"); // ВРЕМЕННО безусловно
    }

    private static void StopCurrentVoice() // Оборвать текущий голос (монофония)
    {
        if (!hasVoice) return; // Нечего глушить

        if (currentVoice.isValid()) currentVoice.stop(FMOD.Studio.STOP_MODE.IMMEDIATE); // Обрываем текущую реплику

        hasVoice = false; // Голоса больше нет
    }
}
