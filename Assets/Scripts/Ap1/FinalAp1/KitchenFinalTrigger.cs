using UnityEngine; // Подключаем Unity-классы

public class KitchenFinalTrigger : MonoBehaviour // Триггер кухонной финальной сцены
{
    public MonsterAI monsterAI; // Ссылка на AI монстра

    public Transform monsterKitchenDoorStopPoint; // Точка перед дверью кухни

    private bool triggered = false; // Защита от повторного запуска

    private void OnTriggerEnter(Collider other) // Срабатывает, когда объект входит в триггер
    {
        if (triggered) return; // Если уже сработал — выходим

        if (!other.CompareTag("Player")) return; // Если вошёл не игрок — выходим

        triggered = true; // Запоминаем, что триггер сработал
        gameObject.SetActive(false); // Выключаем триггер после срабатывания

        if (monsterAI != null) // Если монстр назначен
        {
            monsterAI.StartFinalKitchenChase();
        }
        else // Если монстр не назначен
        {
            Debug.LogWarning("MonsterAI не назначен в KitchenFinalTrigger"); // Предупреждение
        }

        Debug.Log("Игрок вошёл на кухню. Монстр идёт к кухне."); // Сообщение в Console
    }
}