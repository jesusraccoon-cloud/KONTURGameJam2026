using System; // Для Serializable
using System.Collections.Generic; // Для List
using UnityEngine; // Для Vector3

// Данные одного сохранения (чекпоинт + прогресс). Сериализуется в JSON.
[Serializable]
public class SC_SaveData
{
    public int checkpoint = 0; // Номер чекпоинта: 0 — нет, 1 — вход в квартиру, 2 — 4/6, 3 — 6/6

    public string checkpointLabel = ""; // Подпись чекпоинта (для UI)

    public string sceneName = ""; // Имя сцены сохранения (для Continue после перезапуска)

    public string savedAt = ""; // Когда сохранено (для UI)

    public Vector3 playerPos = Vector3.zero; // Позиция игрока в момент чекпоинта (авто-запоминание)

    public float playerYaw = 0f; // Поворот игрока по Y в момент чекпоинта

    public bool monsterActive = false; // Был ли монстр активен в момент чекпоинта

    public Vector3 monsterPos = Vector3.zero; // Позиция монстра в момент чекпоинта

    public float monsterYaw = 0f; // Поворот монстра по Y

    public List<string> used = new List<string>(); // ID «использованных» объектов (триггеры/коллайдеры не сработают снова)

    // Числовое состояние объектов по ID (стадия ThreeStage, флаги радио, открыта/закрыта дверь и т.п.).
    // JsonUtility не умеет Dictionary, поэтому храним двумя параллельными списками: stateKeys[i] -> stateValues[i].
    public List<string> stateKeys = new List<string>(); // ID объектов с сохранённым состоянием

    public List<int> stateValues = new List<int>(); // Значения состояний (в том же порядке, что и ключи)
}
