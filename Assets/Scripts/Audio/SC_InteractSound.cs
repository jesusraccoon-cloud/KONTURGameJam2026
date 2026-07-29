using UnityEngine; // Подключаем Unity-классы
using FMODUnity; // Подключаем FMOD (EventReference, RuntimeManager)

// Универсальный проигрыватель FMOD-события. Метод Play() вешается на любой UnityEvent
// (например, onInteract у ThreeStageInteractableObject) и играет звук при вызове.
public class SC_InteractSound : MonoBehaviour
{
    [Header("FMOD")] // Блок FMOD
    public EventReference sound; // Событие, которое проигрывать

    public bool attachToObject = false; // true — звук следует за объектом; false — разово в его позиции

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
            RuntimeManager.PlayOneShotAttached(sound, gameObject); // Звук из позиции объекта, следует за ним
        }
        else // Иначе разово в точке объекта
        {
            RuntimeManager.PlayOneShot(sound, transform.position); // Звук в позиции объекта
        }

        if (showDebugLogs) Debug.Log(gameObject.name + ": SC_InteractSound — играю звук"); // Лог
    }
}
