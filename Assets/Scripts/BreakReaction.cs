using UnityEngine; // Подключаем Unity-классы
using UnityEngine.Events; // Подключаем UnityEvent для событий в Inspector

public class BreakReaction : MonoBehaviour // Универсальная реакция на разрушение объекта
{
    [Header("Objects")] // Блок объектов
    public GameObject[] objectsToEnable; // Объекты, которые нужно включить при реакции

    public GameObject[] objectsToDisable; // Объекты, которые нужно выключить при реакции

    [Header("Spawn / Move Object")] // Блок появления или переноса объекта
    public GameObject objectToMove; // Объект, который нужно перенести в точку, например кассета

    public Transform movePoint; // Точка, куда нужно перенести objectToMove

    public bool enableMovedObject = true; // Нужно ли включить objectToMove после переноса

    [Header("Physics Push")] // Блок физического толчка
    public Rigidbody[] rigidbodiesToPush; // Rigidbody-объекты, которые нужно толкнуть

    public Transform pushOrigin; // Точка, откуда идёт толчок

    public float pushForce = 3f; // Сила толчка в стороны

    public float upwardForce = 1f; // Дополнительная сила вверх

    [Header("Audio")] // Блок звука
    public AudioSource audioSource; // AudioSource, через который проигрывается звук

    public AudioClip reactionClip; // Звук реакции, например взрыв, треск, искры

    [Header("Events")] // Блок дополнительных событий
    public UnityEvent onReaction; // Дополнительное событие после реакции

    [Header("Debug")] // Блок отладки
    public bool showDebugLogs = true; // Показывать ли сообщения в Console

    public void PlayReaction() // Метод, который вызывается из BreakableObject OnBreak
    {
        EnableObjects(); // Включаем нужные объекты

        DisableObjects(); // Выключаем нужные объекты

        MoveObjectToPoint(); // Переносим объект в точку, если назначено

        PushRigidbodies(); // Толкаем физические объекты

        PlayAudio(); // Проигрываем звук

        onReaction.Invoke(); // Вызываем дополнительные события

        if (showDebugLogs) // Если debug включён
        {
            Debug.Log(gameObject.name + ": BreakReaction выполнен"); // Пишем лог
        }
    }

    private void EnableObjects() // Метод включения объектов
    {
        for (int i = 0; i < objectsToEnable.Length; i++) // Проходим по всем объектам включения
        {
            if (objectsToEnable[i] == null) continue; // Если элемент пустой — пропускаем

            objectsToEnable[i].SetActive(true); // Включаем объект
        }
    }

    private void DisableObjects() // Метод выключения объектов
    {
        for (int i = 0; i < objectsToDisable.Length; i++) // Проходим по всем объектам выключения
        {
            if (objectsToDisable[i] == null) continue; // Если элемент пустой — пропускаем

            objectsToDisable[i].SetActive(false); // Выключаем объект
        }
    }

    private void MoveObjectToPoint() // Метод переноса объекта
    {
        if (objectToMove == null) return; // Если объекта для переноса нет — выходим

        if (movePoint != null) // Если точка переноса назначена
        {
            objectToMove.transform.position = movePoint.position; // Ставим объект в позицию точки

            objectToMove.transform.rotation = movePoint.rotation; // Ставим объекту поворот точки
        }

        if (enableMovedObject) // Если объект нужно включить
        {
            objectToMove.SetActive(true); // Включаем объект
        }
    }

    private void PushRigidbodies() // Метод толчка Rigidbody
    {
        Transform origin = pushOrigin != null ? pushOrigin : transform; // Если точка толчка не назначена, используем этот объект

        for (int i = 0; i < rigidbodiesToPush.Length; i++) // Проходим по всем Rigidbody
        {
            if (rigidbodiesToPush[i] == null) continue; // Если Rigidbody пустой — пропускаем

            rigidbodiesToPush[i].isKinematic = false; // Включаем физику объекта

            Vector3 direction = rigidbodiesToPush[i].transform.position - origin.position; // Считаем направление от точки толчка

            direction.y += upwardForce; // Добавляем вертикальную силу

            if (direction.sqrMagnitude < 0.01f) // Если направление почти нулевое
            {
                direction = origin.forward + Vector3.up * upwardForce; // Используем направление вперёд плюс вверх
            }

            rigidbodiesToPush[i].AddForce(direction.normalized * pushForce, ForceMode.Impulse); // Толкаем объект импульсом
        }
    }

    private void PlayAudio() // Метод проигрывания звука
    {
        if (audioSource == null) return; // Если AudioSource нет — выходим

        if (reactionClip == null) return; // Если клип не назначен — выходим

        audioSource.PlayOneShot(reactionClip); // Проигрываем звук один раз
    }
}