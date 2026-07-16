using System.Collections.Generic; // Подключаем списки и словари
using UnityEngine; // Подключаем Unity-классы

// Вешается на игрока (или объект слушателя). Раз в интервал пускает лучи
// ко всем зарегистрированным IOccludable-источникам в радиусе, считает окклюзию
// и раздаёт им сглаженное значение. Слои не нужны — в авто-режиме стенами
// считается вся геометрия, кроме самого источника и игрока.
public class SC_OcclusionListener : MonoBehaviour
{
    private static readonly List<IOccludable> Sources = new List<IOccludable>(); // Все источники, что хотят окклюзию

    public static void Register(IOccludable s) // Источник регистрируется при включении
    {
        if (s != null && !Sources.Contains(s)) Sources.Add(s); // Добавляем, если ещё нет
    }

    public static void Unregister(IOccludable s) // Источник отписывается при выключении
    {
        Sources.Remove(s); // Убираем из списка
    }

    [Header("Listener")] // Блок слушателя
    public Transform listenerPoint; // Откуда пускаем лучи (если пусто — этот объект)

    public Transform playerRoot; // Чьи коллайдеры игнорировать (если пусто — корень слушателя)

    [Header("Range")] // Блок радиуса
    public float maxDistance = 25f; // Дальше этого не окклюдим и не тратим лучи

    [Header("Wall Detection")] // Блок определения стен
    public bool autoDetectAllWalls = true; // БЕЗ слоёв: стенами считается вся геометрия, кроме источника и игрока

    public LayerMask occlusionMask = ~0; // Слои-стены. Используется ТОЛЬКО если Auto Detect выключен

    [Header("Raycast")] // Блок лучей
    [Range(1, 9)] public int rayCount = 3; // Сколько лучей: 1 = бинарно, больше = мягче

    public float raySpread = 0.5f; // Разброс дополнительных лучей вокруг источника (метры)

    public float sourceHeightOffset = 0f; // Приподнять точку источника

    [Range(0f, 1f)] public float maxOcclusion = 1f; // Максимум окклюзии

    public float checkInterval = 0.1f; // Как часто пересчитывать лучи (сек)

    [Header("Smoothing")] // Блок сглаживания
    public float smoothSpeed = 4f; // Скорость сглаживания значения (ед/сек)

    [Header("Debug")] // Блок отладки
    public bool showDebugRays = false; // Рисовать лучи (красный — перекрыт, зелёный — свободен)

    private readonly Dictionary<IOccludable, float> _target = new Dictionary<IOccludable, float>(); // Цель по источнику

    private readonly Dictionary<IOccludable, float> _current = new Dictionary<IOccludable, float>(); // Сглаженное по источнику

    private float _timer; // Таймер до пересчёта

    private void Update() // Каждый кадр
    {
        Transform lisT = listenerPoint != null ? listenerPoint : transform; // Трансформ слушателя
        Vector3 lis = lisT.position; // Позиция слушателя

        _timer -= Time.deltaTime; // Уменьшаем таймер
        bool recompute = _timer <= 0f; // Пора ли пересчитывать лучи
        if (recompute) _timer = checkInterval; // Заводим таймер заново

        for (int i = Sources.Count - 1; i >= 0; i--) // По всем источникам (с конца — можно удалять)
        {
            IOccludable s = Sources[i]; // Текущий источник

            if (s is UnityEngine.Object o && o == null) // Объект уничтожен, но не отписался
            {
                Sources.RemoveAt(i); // Чистим
                continue; // Дальше
            }

            if (recompute) _target[s] = ComputeTarget(s, lis, lisT); // Пересчитываем цель по таймеру

            float tgt = _target.TryGetValue(s, out float tv) ? tv : 0f; // Цель
            float cur = _current.TryGetValue(s, out float cv) ? cv : 0f; // Текущее

            cur = Mathf.MoveTowards(cur, tgt, smoothSpeed * Time.deltaTime); // Плавно к цели
            _current[s] = cur; // Запоминаем

            s.ApplyOcclusion(cur); // Отдаём источнику
        }
    }

    private float ComputeTarget(IOccludable s, Vector3 lis, Transform lisT) // Целевая окклюзия источника
    {
        if (!s.WantsOcclusion) return 0f; // Молчит — окклюзии нет

        Vector3 sp = s.OcclusionPoint + Vector3.up * sourceHeightOffset; // Точка источника

        if ((sp - lis).sqrMagnitude > maxDistance * maxDistance) return 0f; // Вне радиуса — не окклюдим

        Component sc = s as Component; // Компонент источника (чтобы игнорировать его коллайдеры)
        Transform sourceT = sc != null ? sc.transform : null; // Трансформ источника

        return ComputeOcclusion(sp, lis, lisT, sourceT); // Считаем лучами
    }

    private float ComputeOcclusion(Vector3 sp, Vector3 lis, Transform lisT, Transform sourceT) // Окклюзия лучами
    {
        int rays = Mathf.Max(1, rayCount); // Минимум один луч

        Vector3 dir = lis - sp; // Направление на слушателя
        float dist = dir.magnitude; // Дистанция
        if (dist < 0.01f) return 0f; // Вплотную — окклюзии нет
        dir /= dist; // Нормализуем

        Vector3 right = Vector3.Cross(dir, Vector3.up).normalized; // Ось «вправо»
        if (right.sqrMagnitude < 0.001f) right = Vector3.right; // Защита при взгляде строго вверх/вниз
        Vector3 up = Vector3.Cross(right, dir).normalized; // Ось «вверх»

        int blocked = 0; // Перекрытые лучи

        for (int i = 0; i < rays; i++) // По всем лучам
        {
            Vector3 offset = Vector3.zero; // Смещение источника

            if (rays > 1 && i > 0) // Первый луч центральный, остальные по кругу
            {
                float angle = (i - 1) * (360f / (rays - 1)) * Mathf.Deg2Rad; // Угол
                offset = (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * raySpread; // Точка вокруг источника
            }

            if (IsBlocked(sp + offset, lis, lisT, sourceT)) blocked++; // Считаем перекрытые
        }

        return ((float)blocked / rays) * maxOcclusion; // Доля перекрытых * максимум
    }

    private bool IsBlocked(Vector3 from, Vector3 to, Transform lisT, Transform sourceT) // Перекрыт ли отрезок
    {
        Vector3 seg = to - from; // Вектор
        float len = seg.magnitude; // Длина
        if (len < 0.01f) return false; // Коротко — нет

        if (!autoDetectAllWalls) // Режим по слоям
        {
            bool hit = Physics.Linecast(from, to, occlusionMask, QueryTriggerInteraction.Ignore); // Луч по слоям
            if (showDebugRays) Debug.DrawLine(from, to, hit ? Color.red : Color.green); // Отладка
            return hit; // Результат
        }

        // Авто-режим: вся геометрия, кроме источника и игрока
        Vector3 dir = seg / len; // Направление
        Transform ignoreRoot = playerRoot != null ? playerRoot : (lisT != null ? lisT.root : null); // Кого считаем игроком

        RaycastHit[] hits = Physics.RaycastAll(from, dir, len, ~0, QueryTriggerInteraction.Ignore); // Все препятствия

        for (int i = 0; i < hits.Length; i++) // Перебираем
        {
            Transform t = hits[i].collider.transform; // Во что попали

            if (sourceT != null && t.IsChildOf(sourceT)) continue; // Пропускаем сам источник
            if (ignoreRoot != null && t.IsChildOf(ignoreRoot)) continue; // Пропускаем игрока

            if (showDebugRays) Debug.DrawLine(from, to, Color.red); // Отладка: перекрыто
            return true; // Нашли настоящую стену
        }

        if (showDebugRays) Debug.DrawLine(from, to, Color.green); // Отладка: свободно
        return false; // Ничего не мешает
    }
}
