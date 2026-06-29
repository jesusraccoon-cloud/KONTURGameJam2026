using UnityEngine; // Подключаем Unity-классы: MonoBehaviour, Gizmos, Color и т.д.

public class RoomZone : MonoBehaviour // Скрипт комнаты/зоны, например Hall, Kitchen, Bathroom
{
    [Header("Room Settings")] // Заголовок в Inspector
    public string roomId = "Room"; // ID комнаты: Hall, Kitchen, Bathroom, Room1 и т.д.

    [Header("Debug")] // Заголовок для отладки
    public bool drawGizmos = true; // Показывать ли зону комнаты в Scene через Gizmos

    public string GetRoomId() // Метод, который возвращает ID комнаты
    {
        return roomId; // Отдаём название комнаты другим скриптам
    }

    private void OnDrawGizmos() // Unity вызывает это для рисования подсказок в Scene
    {
        if (!drawGizmos) return; // Если отрисовка выключена — выходим

        Gizmos.color = new Color(0f, 1f, 1f, 0.15f); // Задаём полупрозрачный голубой цвет

        Collider[] colliders = GetComponentsInChildren<Collider>(); // Берём все Collider внутри этой RoomZone

        foreach (Collider col in colliders) // Перебираем каждый Collider
        {
            if (col == null) continue; // Если Collider пустой — пропускаем

            Gizmos.matrix = col.transform.localToWorldMatrix; // Учитываем позицию/поворот/размер объекта Collider

            BoxCollider box = col as BoxCollider; // Пробуем привести Collider к BoxCollider

            if (box != null) // Если это BoxCollider
            {
                Gizmos.DrawCube(box.center, box.size); // Рисуем куб по размеру BoxCollider
            }
        }

        Gizmos.matrix = Matrix4x4.identity; // Возвращаем матрицу Gizmos в нормальное состояние
    }
}