using UnityEngine; // Подключаем Unity-классы
using FMODUnity; // Подключаем FMOD (StudioEventEmitter, StudioListener)

[RequireComponent(typeof(StudioEventEmitter))] // Окклюзию вешаем на объект с FMOD-эмиттером
public class SC_SoundOcclusion : MonoBehaviour // Приглушение звука, когда между источником и слушателем есть препятствия
{
    [Header("References")] // Блок ссылок
    public StudioEventEmitter emitter; // FMOD-эмиттер, который окклюдим (обычно зациклённый звук)

    public Transform listener; // Слушатель (камера/игрок). Если пусто — найдём сами

    [Header("FMOD Parameter")] // Блок параметра FMOD
    public string occlusionParameter = "Occlusion"; // Непрерывный параметр 0..1 в событии

    [Header("Wall Detection")] // Блок определения стен
    public bool autoDetectAllWalls = true; // БЕЗ слоёв: стенами считается вся геометрия, кроме источника и игрока

    public Transform playerRoot; // Корень игрока, чьи коллайдеры игнорировать (если пусто — возьмём корень слушателя)

    [Header("Raycast")] // Блок лучей
    public LayerMask occlusionMask = ~0; // Слои-стены. Используется ТОЛЬКО если Auto Detect All Walls выключен

    public float sourceHeightOffset = 0f; // Поднять точку источника (в гайде — половина высоты объекта)

    [Range(1, 9)] public int rayCount = 3; // Сколько лучей: 1 = как в гайде (бинарно), больше = мягче

    public float raySpread = 0.5f; // Разброс дополнительных лучей вокруг источника (метры)

    [Range(0f, 1f)] public float maxOcclusion = 1f; // Максимум окклюзии (насколько сильно глушить)

    public float checkInterval = 0.1f; // Как часто пускать лучи (сек). Меньше = точнее, но дороже

    [Header("Smoothing")] // Блок сглаживания
    public bool smoothInScript = true; // Сглаживать значение в скрипте (иначе — резко, полагаемся на Seek Speed в FMOD)

    public float smoothSpeed = 4f; // Скорость сглаживания (единиц параметра в секунду)

    [Header("Debug")] // Блок отладки
    public bool showDebugRays = false; // Рисовать лучи в Scene (красный — перекрыт, зелёный — свободен)

    private float targetOcclusion = 0f; // Целевая окклюзия (пересчитывается по таймеру)

    private float currentOcclusion = 0f; // Текущая (сглаженная) окклюзия — её пишем в FMOD

    private float checkTimer = 0f; // Таймер до следующего пуска лучей

    private void Awake() // Вызывается при создании объекта
    {
        if (emitter == null) // Если эмиттер не назначен
        {
            emitter = GetComponent<StudioEventEmitter>(); // Берём с этого же объекта
        }
    }

    private void Start() // Вызывается перед первым кадром
    {
        ResolveListener(); // Ищем слушателя, если не назначен

        currentOcclusion = 0f; // Стартуем без окклюзии
        ApplyOcclusion(); // Сразу пишем 0 в параметр
    }

    private void Update() // Вызывается каждый кадр
    {
        if (emitter == null) return; // Без эмиттера нечего окклюдить

        if (listener == null) // Если слушатель ещё не найден (мог появиться позже)
        {
            ResolveListener(); // Пробуем найти
            if (listener == null) return; // Всё ещё нет — выходим
        }

        checkTimer -= Time.deltaTime; // Уменьшаем таймер

        if (checkTimer <= 0f) // Пора пересчитать окклюзию
        {
            checkTimer = checkInterval; // Заводим таймер заново
            targetOcclusion = ComputeOcclusion(); // Пускаем лучи, считаем цель
        }

        if (smoothInScript) // Если сглаживаем в скрипте
        {
            currentOcclusion = Mathf.MoveTowards(currentOcclusion, targetOcclusion, smoothSpeed * Time.deltaTime); // Плавно к цели
        }
        else // Иначе резко
        {
            currentOcclusion = targetOcclusion; // Сразу цель (сглаживание оставляем FMOD Seek Speed)
        }

        ApplyOcclusion(); // Пишем значение в FMOD
    }

    private void ApplyOcclusion() // Записать текущую окклюзию в параметр эмиттера
    {
        emitter.SetParameter(occlusionParameter, currentOcclusion); // FMOD сам разрулит по инстансу
    }

    private float ComputeOcclusion() // Посчитать окклюзию лучами (0 = свободно, 1 = полностью перекрыто)
    {
        Vector3 source = transform.position + Vector3.up * sourceHeightOffset; // Точка источника (чуть приподнята)
        Vector3 target = listener.position; // Точка слушателя

        int rays = Mathf.Max(1, rayCount); // Минимум один луч

        Vector3 dir = target - source; // Направление на слушателя
        float dist = dir.magnitude; // Дистанция

        if (dist < 0.01f) return 0f; // Слушатель вплотную — окклюзии нет

        dir /= dist; // Нормализуем направление

        Vector3 right = Vector3.Cross(dir, Vector3.up).normalized; // Ось «вправо» относительно луча
        if (right.sqrMagnitude < 0.001f) right = Vector3.right; // Защита, если смотрим строго вверх/вниз
        Vector3 up = Vector3.Cross(right, dir).normalized; // Ось «вверх» относительно луча

        int blocked = 0; // Сколько лучей перекрыто

        for (int i = 0; i < rays; i++) // По всем лучам
        {
            Vector3 offset = Vector3.zero; // Смещение источника для этого луча

            if (rays > 1 && i > 0) // Первый луч — центральный, остальные — по кругу
            {
                float angle = (i - 1) * (360f / (rays - 1)) * Mathf.Deg2Rad; // Угол на окружности
                offset = (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * raySpread; // Точка на окружности вокруг источника
            }

            Vector3 from = source + offset; // Старт луча

            if (IsBlocked(from, target)) blocked++; // Считаем перекрытые лучи
        }

        return ((float)blocked / rays) * maxOcclusion; // Доля перекрытых лучей * максимум
    }

    private bool IsBlocked(Vector3 from, Vector3 to) // Перекрыт ли отрезок препятствием
    {
        Vector3 seg = to - from; // Вектор отрезка
        float len = seg.magnitude; // Длина

        if (len < 0.01f) return false; // Слишком коротко — не перекрыто

        bool blocked; // Результат

        if (!autoDetectAllWalls) // Режим по слоям (как в гайде)
        {
            blocked = Physics.Linecast(from, to, occlusionMask, QueryTriggerInteraction.Ignore); // Луч только по слоям-стенам
        }
        else // Авто-режим: вся геометрия, кроме источника и игрока
        {
            blocked = false; // Пока не перекрыто

            Vector3 dir = seg / len; // Направление

            Transform ignoreRoot = playerRoot != null ? playerRoot : (listener != null ? listener.root : null); // Кого считаем игроком

            RaycastHit[] hits = Physics.RaycastAll(from, dir, len, ~0, QueryTriggerInteraction.Ignore); // Все препятствия на пути

            for (int i = 0; i < hits.Length; i++) // Перебираем попадания
            {
                Transform t = hits[i].collider.transform; // Во что попали

                if (t.IsChildOf(transform)) continue; // Пропускаем сам источник и его детей

                if (ignoreRoot != null && t.IsChildOf(ignoreRoot)) continue; // Пропускаем игрока

                blocked = true; // Нашли настоящую стену
                break; // Дальше не ищем
            }
        }

        if (showDebugRays) Debug.DrawLine(from, to, blocked ? Color.red : Color.green); // Рисуем для отладки

        return blocked; // Отдаём результат
    }

    private void ResolveListener() // Найти слушателя, если не назначен вручную
    {
        if (listener != null) return; // Уже есть — выходим

        StudioListener sl = FindObjectOfType<StudioListener>(); // Ищем FMOD-слушателя в сцене

        if (sl != null) // Нашли
        {
            listener = sl.transform; // Берём его трансформ
        }
        else if (Camera.main != null) // Иначе главную камеру
        {
            listener = Camera.main.transform; // Берём камеру
        }
    }
}
