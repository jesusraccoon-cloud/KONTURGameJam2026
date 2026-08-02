using UnityEngine; // Подключаем Unity-классы
using FMODUnity; // Подключаем FMOD (EventReference, RuntimeManager)
using FMOD.Studio; // Подключаем EventInstance

// Переиспользуемый 3D-звук-cue. Ставишь объект ТУДА, откуда должен идти звук
// (у двери, у окна и т.п.), назначаешь событие и вызываешь Play() из UnityEvent
// (например, onHallDoorBreak в ApartmentFinalSequence). Игрок по 3D-позиции
// поймёт, откуда звук. Поддерживает окклюзию (глухо через стены).
public class SC_SoundCue3D : MonoBehaviour
{
    [Header("FMOD")] // Блок FMOD
    public EventReference soundEvent; // 3D-событие (со спатиалайзером)

    public Transform origin; // Откуда звучит (если пусто — этот объект)

    [Header("Occlusion")] // Приглушение за стенами
    public bool occlude = true; // Глушить, если точка за стеной от игрока

    public string occlusionParameter = "Occlusion"; // Непрерывный параметр 0..1 в событии

    [Header("Debug")] // Отладка
    public bool showDebugLogs = false; // Показывать логи

    // Публичный метод для UnityEvent (или вызова из кода).
    public void Play()
    {
        if (soundEvent.IsNull) // Если событие не назначено
        {
            if (showDebugLogs) Debug.LogWarning(gameObject.name + ": SC_SoundCue3D — событие не назначено"); // Предупреждение
            return; // Выходим
        }

        Transform o = origin != null ? origin : transform; // Точка источника
        Vector3 pos = o.position; // Позиция звука

        EventInstance inst = RuntimeManager.CreateInstance(soundEvent); // Создаём экземпляр

        RuntimeManager.AttachInstanceToGameObject(inst, o.gameObject); // Привязываем к точке (звук из неё)

        if (occlude) // Если нужна окклюзия
        {
            inst.setParameterByName(occlusionParameter, SC_OcclusionListener.Sample(pos, o)); // Замер окклюзии в точке
        }

        inst.start(); // Запускаем
        inst.release(); // Освобождаем (one-shot доиграет и очистится)

        if (showDebugLogs) Debug.Log(gameObject.name + ": SC_SoundCue3D — играю в " + pos); // Лог
    }
}
