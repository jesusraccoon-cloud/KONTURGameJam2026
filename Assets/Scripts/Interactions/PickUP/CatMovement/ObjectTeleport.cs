using System.Collections; // Нужен для корутин
using UnityEngine; // Подключаем Unity

public class ObjectTeleport : MonoBehaviour // Скрипт телепорта
{
    public Transform teleportPoint; // Точка телепорта

    public float hideTime = 0.3f; // Время исчезновения

    private Renderer[] objectRenderers; // Все рендеры объекта

    private Collider[] objectColliders; // Все коллайдеры объекта

    private bool isTeleporting = false; // Защита от повторного запуска

    private void Awake() // Вызывается при старте объекта
    {
        objectRenderers = GetComponentsInChildren<Renderer>(); // Находим все Renderer

        objectColliders = GetComponentsInChildren<Collider>(); // Находим все Collider
    }

    public void StartTeleport() // Метод, который вызывает CatMoveToPoint
    {
        if (isTeleporting) // Если телепорт уже идет
            return; // Не запускаем второй раз

        StartCoroutine(TeleportRoutine()); // Запускаем корутину телепорта
    }

    private IEnumerator TeleportRoutine() // Сама логика телепорта с задержкой
    {
        isTeleporting = true; // Помечаем, что телепорт начался

        SetObjectVisible(false); // Скрываем объект

        yield return new WaitForSeconds(hideTime); // Ждем паузу

        if (teleportPoint != null) // Если точка телепорта назначена
        {
            transform.position = teleportPoint.position; // Переносим объект

            transform.rotation = teleportPoint.rotation; // Поворачиваем объект
        }
        else // Если точка не назначена
        {
            Debug.LogWarning("Teleport Point не назначен"); // Пишем предупреждение
        }

        SetObjectVisible(true); // Показываем объект обратно

        isTeleporting = false; // Телепорт завершен
    }

    private void SetObjectVisible(bool isVisible) // Метод скрытия/показа объекта
    {
        foreach (Renderer currentRenderer in objectRenderers) // Проходим по всем Renderer
        {
            currentRenderer.enabled = isVisible; // Включаем или выключаем видимость
        }

        foreach (Collider currentCollider in objectColliders) // Проходим по всем Collider
        {
            currentCollider.enabled = isVisible; // Включаем или выключаем коллайдер
        }
    }
}