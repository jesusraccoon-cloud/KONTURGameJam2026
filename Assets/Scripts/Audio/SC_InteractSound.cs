using UnityEngine; // Подключаем Unity-классы
using FMODUnity; // Подключаем FMOD (EventReference, RuntimeManager)

// Универсальный проигрыватель FMOD-события. Метод Play() вешается на любой UnityEvent
// (например, onInteract у ThreeStageInteractableObject) и играет звук при вызове.
public class SC_InteractSound : MonoBehaviour
{
    [Header("FMOD")] // Блок FMOD
    public EventReference sound; // Событие, которое проигрывать

    public bool attachToObject = false; // true — звук следует за объектом; false — разово в его позиции

    public Transform soundOrigin; // Откуда играть звук. Пусто — позиция этого объекта. ЗАДАЙ, если объект стоит не там, где слышно (например в «складе»/стейджинге координат).

    [Header("Debug")] // Блок отладки
    public bool showDebugLogs = false; // Показывать логи

    public void Play() // Публичный метод для UnityEvent
    {
        if (sound.IsNull) // Если событие не назначено
        {
            if (showDebugLogs) Debug.LogWarning(gameObject.name + ": SC_InteractSound — событие не назначено"); // Предупреждение
            return; // Выходим
        }

        if (attachToObject) // Если привязываем к объекту
        {
            GameObject target = soundOrigin != null ? soundOrigin.gameObject : gameObject; // К чему привязать
            RuntimeManager.PlayOneShotAttached(sound, target); // Звук следует за объектом-источником
        }
        else // Иначе разово в точке источника
        {
            Vector3 pos = soundOrigin != null ? soundOrigin.position : transform.position; // Откуда играть
            RuntimeManager.PlayOneShot(sound, pos); // Звук в нужной точке
        }

        if (showDebugLogs) Debug.Log(gameObject.name + ": SC_InteractSound — играю звук"); // Лог
    }
}
