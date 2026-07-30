using UnityEngine; // Подключаем Unity-классы
using FMODUnity; // Подключаем FMOD (EventReference, RuntimeManager)
using FMOD.Studio; // Подключаем EventInstance

// Шаги монстра. Момент шага вызывается из Animation Event -> метод Footstep().
// Проигрывает FMOD-событие с параметром Surface (по полу под ногами) и Occlusion (глухо за стенами).
// Скрипт должен висеть на объекте с Animator (иначе Animation Event не найдёт Footstep()).
public class SC_MonsterFootsteps : MonoBehaviour
{
    [Header("References")] // Блок ссылок
    public MonsterAI monster; // ИИ монстра (нужен, чтобы не играть, пока монстр не активирован)

    public Transform footOrigin; // Точка ног (луч поверхности и позиция звука). Если пусто — этот объект

    [Header("FMOD")] // Блок FMOD
    public EventReference footstepEvent; // Событие шага монстра

    public string surfaceParameter = "Surface"; // Имя параметра поверхности (как у шагов игрока)

    [Header("Surface Detection")] // Определение поверхности
    public bool autoDetectSurface = true; // Определять поверхность лучом вниз по тегу

    public SC_Footsteps.FootstepSurface defaultSurface = SC_Footsteps.FootstepSurface.Concrete; // Поверхность по умолчанию / fallback

    public SC_Footsteps.SurfaceTag[] surfaceTags; // Маппинг тег -> поверхность (те же теги полов, что у игрока)

    public float surfaceRayLength = 1.5f; // Длина луча вниз

    public float surfaceRayUpOffset = 0.3f; // Насколько поднять старт луча над ногами

    public LayerMask surfaceRayMask = ~0; // По каким слоям искать пол

    [Header("Occlusion")] // Приглушение за стенами
    public bool occludeFootsteps = true; // Глушить шаги, если монстр за стеной от игрока

    public string occlusionParameter = "Occlusion"; // Имя параметра окклюзии

    [Header("Debug")] // Отладка
    public bool showDebugLogs = false; // Показывать логи

    // Вызывается из Animation Event на анимации ходьбы/бега монстра (на кадрах касания стопы).
    private void Awake() // При создании
    {
        if (monster == null) monster = GetComponent<MonsterAI>(); // Ищем ИИ на этом объекте

        if (monster == null) monster = GetComponentInParent<MonsterAI>(); // Иначе выше по иерархии
    }

    public void Footstep()
    {
        Debug.Log($"[MONSTER FOOTSTEP] Footstep() вызван на {gameObject.name}"); // ВРЕМЕННО безусловно — проверяем, что Animation Event дёргает метод

        if (monster != null && !monster.isActivated) // Монстр ещё не активирован (напр. загрузка сцены)
        {
            Debug.Log("[MONSTER FOOTSTEP] пропущен — монстр не активен"); // ВРЕМЕННО безусловно
            return; // Не играем
        }

        if (footstepEvent.IsNull) // Если событие не назначено
        {
            Debug.LogWarning($"[MONSTER FOOTSTEP] {gameObject.name}: НЕ назначено FMOD-событие Footstep Event"); // ВРЕМЕННО безусловно
            return; // Выходим
        }

        Transform origin = footOrigin != null ? footOrigin : transform; // Откуда шаг
        Vector3 pos = origin.position; // Позиция шага

        EventInstance inst = RuntimeManager.CreateInstance(footstepEvent); // Создаём экземпляр

        RuntimeManager.AttachInstanceToGameObject(inst, gameObject); // Привязываем к монстру — звук из его позиции

        SC_Footsteps.FootstepSurface surface = DetectSurface(pos); // Определяем поверхность под ногами

        inst.setParameterByNameWithLabel(surfaceParameter, SC_Footsteps.GetSurfaceLabel(surface)); // Ставим поверхность по лейблу

        if (occludeFootsteps) // Если нужна окклюзия
        {
            float occ = SC_OcclusionListener.Sample(pos, transform); // Замер окклюзии в точке шага (коллайдеры монстра игнорируются)
            inst.setParameterByName(occlusionParameter, occ); // Ставим окклюзию
        }

        inst.start(); // Запускаем
        inst.release(); // Освобождаем (one-shot доиграет и очистится)

        Debug.Log($"[MONSTER FOOTSTEP] сыграл: surface={surface} label='{SC_Footsteps.GetSurfaceLabel(surface)}' occlude={occludeFootsteps} pos={pos}"); // ВРЕМЕННО безусловно
    }

    private SC_Footsteps.FootstepSurface DetectSurface(Vector3 originPos) // Определение поверхности под ногами монстра
    {
        if (!autoDetectSurface) return defaultSurface; // Ручной режим — поверхность по умолчанию

        Vector3 rayOrigin = originPos + Vector3.up * surfaceRayUpOffset; // Старт луча чуть выше ног

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, surfaceRayLength, surfaceRayMask, QueryTriggerInteraction.Ignore)) // Луч вниз
        {
            for (int i = 0; i < surfaceTags.Length; i++) // Идём по маппингу тегов
            {
                if (!string.IsNullOrEmpty(surfaceTags[i].tag) && hit.collider.CompareTag(surfaceTags[i].tag)) // Если тег совпал
                {
                    return surfaceTags[i].surface; // Возвращаем поверхность из маппинга
                }
            }
        }

        return defaultSurface; // Ничего не нашли — поверхность по умолчанию
    }
}
