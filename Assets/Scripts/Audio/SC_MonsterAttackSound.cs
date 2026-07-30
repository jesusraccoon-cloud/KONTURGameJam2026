using UnityEngine; // Подключаем Unity-классы
using FMODUnity; // Подключаем FMOD (EventReference, RuntimeManager)
using FMOD.Studio; // Подключаем EventInstance

// Звук атаки монстра. Момент атаки вызывается из Animation Event -> метод Attack().
// One-shot с окклюзией. Скрипт должен висеть на объекте с Animator.
public class SC_MonsterAttackSound : MonoBehaviour
{
    [Header("References")] // Блок ссылок
    public MonsterAI monster; // ИИ монстра (нужен, чтобы не играть, пока монстр не активирован)

    [Header("FMOD")] // Блок FMOD
    public EventReference attackEvent; // Событие атаки монстра

    [Header("Occlusion")] // Приглушение за стенами
    public bool occlude = true; // Глушить, если монстр за стеной от игрока

    public string occlusionParameter = "Occlusion"; // Имя параметра окклюзии

    [Header("Debug")] // Отладка
    public bool showDebugLogs = false; // Показывать логи

    private void Awake() // При создании
    {
        if (monster == null) monster = GetComponent<MonsterAI>(); // Ищем ИИ на этом объекте

        if (monster == null) monster = GetComponentInParent<MonsterAI>(); // Иначе выше по иерархии
    }

    // Вызывается из Animation Event на анимации атаки.
    public void Attack()
    {
        Debug.Log($"[MONSTER ATTACK] Attack() вызван на {gameObject.name}"); // ВРЕМЕННО безусловно — проверяем, что Animation Event дёргает метод

        if (monster != null && !monster.isActivated) // Монстр ещё не активирован (напр. загрузка сцены)
        {
            Debug.Log("[MONSTER ATTACK] пропущен — монстр не активен"); // ВРЕМЕННО безусловно
            return; // Не играем
        }

        if (attackEvent.IsNull) // Если событие не назначено
        {
            Debug.LogWarning($"[MONSTER ATTACK] {gameObject.name}: НЕ назначено FMOD-событие Attack Event"); // ВРЕМЕННО безусловно
            return; // Выходим
        }

        Vector3 pos = transform.position; // Позиция монстра

        EventInstance inst = RuntimeManager.CreateInstance(attackEvent); // Создаём экземпляр

        RuntimeManager.AttachInstanceToGameObject(inst, gameObject); // Привязываем к монстру — звук из его позиции

        if (occlude) // Если нужна окклюзия
        {
            float occ = SC_OcclusionListener.Sample(pos, transform); // Замер окклюзии в точке монстра (его коллайдеры игнорируются)
            inst.setParameterByName(occlusionParameter, occ); // Ставим окклюзию
        }

        inst.start(); // Запускаем
        inst.release(); // Освобождаем (one-shot доиграет и очистится)

        Debug.Log($"[MONSTER ATTACK] сыграл атаку occlude={occlude} pos={pos}"); // ВРЕМЕННО безусловно
    }
}
