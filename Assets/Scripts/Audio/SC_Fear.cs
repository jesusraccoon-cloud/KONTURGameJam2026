using UnityEngine; // Подключаем Unity-классы
using FMODUnity; // Подключаем FMOD (RuntimeManager)

// Управляет ГЛОБАЛЬНЫМ параметром FMOD "Fear" по близости монстра и его состоянию.
// Значение = максимум из «страха по дистанции» и «страха по состоянию», сглажено.
// Всё настраивается в инспекторе.
public class SC_Fear : MonoBehaviour
{
    [Header("References")] // Блок ссылок
    public MonsterAI monster; // Монстр, за которым следим

    public Transform player; // Игрок (если пусто — возьмём monster.player)

    [Header("FMOD Global Parameter")] // Блок параметра FMOD
    public string fearParameter = "Fear"; // Имя глобального параметра

    [Header("Distance Fear")] // Страх по дистанции
    public bool useDistanceFear = true; // Учитывать дистанцию до монстра

    public float nearDistance = 3f; // Ближе этого — максимум страха

    public float farDistance = 20f; // Дальше этого — ноль

    [Header("State Fear")] // Страх по состоянию монстра
    public bool useStateFear = true; // Учитывать состояние (погоня/атака и т.п.)

    [Range(0f, 1f)] public float chaseFear = 0.7f; // Страх во время погони

    [Range(0f, 1f)] public float attackFear = 1f; // Страх во время атаки

    [Range(0f, 1f)] public float investigateFear = 0.3f; // Страх, когда монстр идёт на шум / осматривается

    [Header("Range")] // Общий диапазон
    [Range(0f, 1f)] public float maxFear = 1f; // Максимум параметра (потолок)

    [Header("Smoothing")] // Сглаживание
    public float riseSpeed = 3f; // Скорость нарастания страха (быстро)

    public float fallSpeed = 1f; // Скорость спада страха (медленно — напряжение держится дольше)

    [Header("Debug")] // Отладка
    public bool showDebugLogs = false; // Показывать логи

    private float currentFear = 0f; // Текущее сглаженное значение

    private float lastLogged = -1f; // Последнее залогированное значение

    private void Update() // Каждый кадр
    {
        float target = ComputeTargetFear(); // Целевой страх

        float speed = target > currentFear ? riseSpeed : fallSpeed; // Растёт быстро, спадает медленно

        currentFear = Mathf.MoveTowards(currentFear, target, speed * Time.deltaTime); // Плавно к цели

        RuntimeManager.StudioSystem.setParameterByName(fearParameter, currentFear); // Пишем в глобальный параметр FMOD

        if (showDebugLogs && Mathf.Abs(currentFear - lastLogged) >= 0.05f) // Логируем при заметном изменении
        {
            lastLogged = currentFear; // Запоминаем
            Debug.Log($"Fear = {currentFear:0.00} (target {target:0.00}, state {(monster != null ? monster.currentState.ToString() : "no monster")})"); // Лог
        }
    }

    private float ComputeTargetFear() // Считает целевой страх
    {
        if (monster == null) return 0f; // Нет монстра — нет страха

        if (monster.currentState == MonsterState.Disabled) return 0f; // Монстр выключен — нет страха

        float distanceFear = 0f; // Страх по дистанции

        if (useDistanceFear) // Если учитываем дистанцию
        {
            Transform p = player != null ? player : monster.player; // Игрок

            if (p != null) // Если игрок известен
            {
                float d = Vector3.Distance(monster.transform.position, p.position); // Дистанция монстр-игрок

                distanceFear = Mathf.InverseLerp(farDistance, nearDistance, d) * maxFear; // Ближе → больше страха
            }
        }

        float stateFear = 0f; // Страх по состоянию

        if (useStateFear) // Если учитываем состояние
        {
            switch (monster.currentState) // Смотрим состояние
            {
                case MonsterState.Chase: stateFear = chaseFear; break; // Погоня
                case MonsterState.Attack: stateFear = attackFear; break; // Атака
                case MonsterState.InvestigateNoise: stateFear = investigateFear; break; // Идёт на шум
                case MonsterState.LookAroundNoise: stateFear = investigateFear; break; // Осматривается
            }
        }

        return Mathf.Min(maxFear, Mathf.Max(distanceFear, stateFear)); // Берём сильнейший драйвер, но не выше потолка
    }
}
