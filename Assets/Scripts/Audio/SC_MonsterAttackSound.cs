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
        if (monster != null) // Если ИИ известен — проверяем, что монстр реально атакует
        {
            bool attacking = (monster.attack != null && monster.attack.IsAttacking) // Идёт атака
                || monster.currentState == MonsterState.Attack; // Или состояние — атака

            if (!monster.isActivated || !attacking) // Не активирован ИЛИ сейчас не в атаке (шальной Animation Event на респавне/загрузке)
            {
                if (showDebugLogs) Debug.Log($"[MONSTER ATTACK] пропущен: activated={monster.isActivated}, attacking={attacking}"); // Лог
                return; // Не играем звук атаки
            }
        }

        if (attackEvent.IsNull) // Если событие не назначено
        {
            if (showDebugLogs) Debug.LogWarning(gameObject.name + ": SC_MonsterAttackSound — не назначено Attack Event"); // Предупреждение
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

        if (showDebugLogs) Debug.Log(gameObject.name + ": атака (звук)"); // Лог
    }
}
