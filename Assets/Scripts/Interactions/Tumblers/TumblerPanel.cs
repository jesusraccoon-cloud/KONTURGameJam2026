using UnityEngine; // Подключаем Unity

public class TumblerPanel : MonoBehaviour, IInteractable, ILookInteractable // Панель реагирует на взгляд и на E
{
    public Camera playerCamera; // Камера игрока

    public TumblerSwitch[] tumblers; // Все тумблеры панели

    private TumblerSwitch currentTumbler; // Текущий выбранный тумблер

    private void Start() // Старт сцены
    {
        if (playerCamera == null) // Если камера не назначена
        {
            playerCamera = Camera.main; // Берем главную камеру
        }

        if (tumblers == null || tumblers.Length == 0) // Если тумблеры не назначены
        {
            tumblers = GetComponentsInChildren<TumblerSwitch>(); // Ищем новые тумблеры внутри панели
        }
    }

    public void LookUpdate() // Пока игрок смотрит на панель
    {
        TumblerSwitch newTumbler = GetClosestToScreenCenter(playerCamera); // Ищем тумблер ближе к центру экрана

        if (newTumbler == currentTumbler) return; // Если он тот же — ничего не делаем

        if (currentTumbler != null) // Если был старый тумблер
        {
            currentTumbler.SetHighlight(false); // Убираем подсветку
        }

        currentTumbler = newTumbler; // Запоминаем новый тумблер

        if (currentTumbler != null) // Если нашли тумблер
        {
            currentTumbler.SetHighlight(true); // Включаем подсветку
        }
    }

    public void LookExit() // Когда игрок перестал смотреть на панель
    {
        if (currentTumbler != null) // Если был выбран тумблер
        {
            currentTumbler.SetHighlight(false); // Убираем подсветку
        }

        currentTumbler = null; // Очищаем выбранный тумблер
    }

    public void Interact() // Когда игрок нажал E
    {
        if (currentTumbler == null) // Если тумблер не выбран
        {
            currentTumbler = GetClosestToScreenCenter(playerCamera); // Пробуем найти ближайший
        }

        if (currentTumbler != null) // Если нашли
        {
            currentTumbler.Toggle(); // Переключаем тумблер
        }
    }

    private TumblerSwitch GetClosestToScreenCenter(Camera camera) // Ищем ближайший тумблер к центру экрана
    {
        if (camera == null) return null; // Если камеры нет — выходим
        if (tumblers == null || tumblers.Length == 0) return null; // Если тумблеров нет — выходим

        TumblerSwitch closestTumbler = null; // Лучший найденный тумблер
        float closestDistance = float.MaxValue; // Минимальная дистанция
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f); // Центр экрана

        for (int i = 0; i < tumblers.Length; i++) // Перебираем все тумблеры
        {
            if (tumblers[i] == null) continue; // Если пусто — пропускаем

            Vector3 screenPoint = camera.WorldToScreenPoint(tumblers[i].transform.position); // Переводим позицию в экранные координаты

            if (screenPoint.z < 0f) continue; // Если тумблер за камерой — пропускаем

            Vector2 tumblerScreenPosition = new Vector2(screenPoint.x, screenPoint.y); // Позиция тумблера на экране

            float distance = Vector2.Distance(screenCenter, tumblerScreenPosition); // Расстояние до центра экрана

            if (distance < closestDistance) // Если этот ближе
            {
                closestDistance = distance; // Запоминаем дистанцию
                closestTumbler = tumblers[i]; // Запоминаем тумблер
            }
        }

        return closestTumbler; // Возвращаем найденный тумблер
    }
}