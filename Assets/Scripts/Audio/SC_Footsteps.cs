using UnityEngine; // Подключаем Unity-классы
using FMODUnity; // Подключаем FMOD (EventReference, RuntimeManager, RuntimeUtils)
using FMOD.Studio; // Подключаем EventInstance
using StarterAssets; // Подключаем FirstPersonController, чтобы читать его параметры

[RequireComponent(typeof(CharacterController))] // Скорость читаем из CharacterController
public class SC_Footsteps : MonoBehaviour // Скрипт звука шагов игрока через FMOD
{
    public enum FootstepSurface // Поверхности (совпадают с параметром Surface в FMOD)
    {
        Concrete, // Бетон
        Carpet,   // Ковёр
        Glass,    // Стекло
        Linoleum, // Линолеум (в FMOD лейбл называется "linoleum" с маленькой буквы)
        Wood      // Дерево
    }

    public enum FootstepLocomotion // Тип движения (совпадает с параметром Locomotion в FMOD)
    {
        Walk,   // Ходьба
        Run,    // Бег
        Crouch  // Присед
    }

    [System.Serializable] // Чтобы пара "тег -> поверхность" показывалась в инспекторе
    public struct SurfaceTag // Маппинг тега коллайдера на поверхность
    {
        public string tag;               // Тег объекта пола
        public FootstepSurface surface;  // Какая это поверхность
    }

    [Header("References")] // Блок ссылок
    public FirstPersonController controller; // Контроллер игрока: читаем Grounded, canMove, скорости

    private CharacterController characterController; // Для чтения текущей скорости

    [Header("FMOD")] // Блок FMOD
    public EventReference footstepEvent; // Событие Footsteps из FMOD

    public string surfaceParameter = "Surface"; // Имя параметра поверхности в FMOD

    public string locomotionParameter = "Locomotion"; // Имя параметра движения в FMOD

    [Header("Step Intervals")] // Блок интервалов между шагами (свои, независимые от шума)
    public float walkStepInterval = 0.55f; // Интервал шага при ходьбе

    public float sprintStepInterval = 0.35f; // Интервал шага при беге

    public float crouchStepInterval = 0.75f; // Интервал шага в приседе

    public bool firstStepImmediate = true; // Первый шаг сразу при старте движения

    [Header("Movement Detection")] // Блок определения движения
    public float minMoveSpeed = 0.15f; // Минимальная скорость, чтобы считать движение

    public float sprintSpeedThreshold = 5.0f; // Скорость, после которой считаем бег

    public bool onlyWhenGrounded = true; // Шаги только когда игрок на земле

    [Header("Crouch Detection")] // Блок определения приседа
    public bool detectCrouch = true; // Учитывать ли присед

    public KeyCode crouchKey = KeyCode.LeftControl; // Кнопка приседа (как в PlayerCrouch)

    [Header("Surface Detection")] // Блок определения поверхности
    public bool autoDetectSurface = true; // Определять поверхность лучом вниз по тегу

    public FootstepSurface defaultSurface = FootstepSurface.Concrete; // Поверхность по умолчанию / fallback

    public SurfaceTag[] surfaceTags; // Маппинг тег -> поверхность

    public float surfaceRayLength = 1.5f; // Длина луча вниз

    public float surfaceRayUpOffset = 0.3f; // Насколько поднять старт луча над ногами

    public LayerMask surfaceRayMask = ~0; // По каким слоям искать пол

    [Header("Debug")] // Блок отладки
    public bool showDebugLogs = false; // Показывать логи шагов

    private float stepTimer = 0f; // Таймер до следующего шага

    private void Awake() // Вызывается при создании объекта
    {
        characterController = GetComponent<CharacterController>(); // Берём CharacterController с игрока

        if (controller == null) // Если контроллер игрока не назначен вручную
        {
            controller = GetComponent<FirstPersonController>(); // Пробуем найти на этом же объекте
        }
    }

    private void Update() // Вызывается каждый кадр
    {
        if (controller != null && !controller.canMove) // Если движение заблокировано (QTE и т.п.)
        {
            stepTimer = 0f; // Сбрасываем таймер
            return; // Выходим
        }

        if (onlyWhenGrounded && controller != null && !controller.Grounded) // Если в воздухе
        {
            stepTimer = 0f; // Сбрасываем таймер, чтобы шаг был сразу при приземлении
            return; // Пока в воздухе — шагов нет
        }

        float speed = GetHorizontalSpeed(); // Текущая горизонтальная скорость

        if (speed <= minMoveSpeed) // Если игрок стоит
        {
            stepTimer = firstStepImmediate ? 0f : stepTimer; // Готовим мгновенный первый шаг
            return; // Выходим
        }

        FootstepLocomotion locomotion = GetLocomotion(speed); // Определяем тип движения

        float interval = GetInterval(locomotion); // Выбираем интервал для этого типа

        stepTimer -= Time.deltaTime; // Уменьшаем таймер

        if (stepTimer > 0f) return; // Если шаг ещё не наступил — выходим

        stepTimer = interval; // Заводим таймер на следующий шаг

        PlayFootstep(locomotion); // Проигрываем шаг
    }

    private float GetHorizontalSpeed() // Горизонтальная скорость из CharacterController
    {
        if (characterController == null) return 0f; // Если контроллера нет — 0

        Vector3 v = characterController.velocity; // Берём скорость
        v.y = 0f; // Убираем вертикаль (падение/прыжок не считаем движением)
        return v.magnitude; // Возвращаем длину
    }

    private FootstepLocomotion GetLocomotion(float speed) // Определение типа движения
    {
        if (detectCrouch && Input.GetKey(crouchKey)) // Если сидим на корточках
        {
            return FootstepLocomotion.Crouch; // Присед
        }

        if (speed >= sprintSpeedThreshold) // Если скорость выше порога бега
        {
            return FootstepLocomotion.Run; // Бег
        }

        return FootstepLocomotion.Walk; // Иначе ходьба
    }

    private float GetInterval(FootstepLocomotion locomotion) // Интервал для типа движения
    {
        switch (locomotion) // Смотрим тип
        {
            case FootstepLocomotion.Run: return sprintStepInterval; // Бег
            case FootstepLocomotion.Crouch: return crouchStepInterval; // Присед
            default: return walkStepInterval; // Ходьба
        }
    }

    public string GetCurrentSurfaceLabel() // Текущая поверхность сразу в виде лейбла FMOD (для других скриптов, например SC_Jump)
    {
        return GetSurfaceLabel(DetectSurface()); // Определяем поверхность и переводим в лейбл
    }

    public FootstepSurface DetectSurface() // Определение поверхности под ногами
    {
        if (!autoDetectSurface) return defaultSurface; // Ручной режим — сразу поверхность по умолчанию

        Vector3 origin = transform.position + Vector3.up * surfaceRayUpOffset; // Старт луча чуть выше ног

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, surfaceRayLength, surfaceRayMask, QueryTriggerInteraction.Ignore)) // Луч вниз
        {
            for (int i = 0; i < surfaceTags.Length; i++) // Идём по маппингу тегов
            {
                if (!string.IsNullOrEmpty(surfaceTags[i].tag) && hit.collider.CompareTag(surfaceTags[i].tag)) // Если тег совпал
                {
                    if (showDebugLogs) Debug.Log($"[Surface] попал в '{hit.collider.name}' tag='{hit.collider.tag}' → {surfaceTags[i].surface}"); // Диагностика
                    return surfaceTags[i].surface; // Возвращаем поверхность из маппинга
                }
            }

            if (showDebugLogs) Debug.LogWarning($"[Surface] попал в '{hit.collider.name}' tag='{hit.collider.tag}', но такого тега нет в surfaceTags → default {defaultSurface}"); // Диагностика
        }
        else if (showDebugLogs) Debug.LogWarning($"[Surface] луч из {origin} вниз на {surfaceRayLength}м ни во что не попал (mask/длина/высота?) → default {defaultSurface}"); // Диагностика

        return defaultSurface; // Ничего не нашли — поверхность по умолчанию
    }

    private void PlayFootstep(FootstepLocomotion locomotion) // Проигрывание одного шага в FMOD
    {
        if (footstepEvent.IsNull) // Если событие не назначено
        {
            if (showDebugLogs) Debug.LogWarning("SC_Footsteps: не назначено FMOD-событие Footsteps"); // Предупреждение
            return; // Выходим
        }

        FootstepSurface surface = DetectSurface(); // Определяем поверхность

        EventInstance instance = RuntimeManager.CreateInstance(footstepEvent); // Создаём экземпляр события

        instance.set3DAttributes(RuntimeUtils.To3DAttributes(transform.position)); // Позиция шага в пространстве

        string surfaceLabel = GetSurfaceLabel(surface); // Лейбл поверхности для FMOD

        FMOD.RESULT rSurf = instance.setParameterByNameWithLabel(surfaceParameter, surfaceLabel); // Ставим поверхность и ловим результат

        instance.setParameterByNameWithLabel(locomotionParameter, GetLocomotionLabel(locomotion)); // Ставим тип движения по лейблу

        instance.start(); // Запускаем звук
        instance.release(); // Освобождаем экземпляр после завершения (one-shot)

        if (showDebugLogs) Debug.Log($"Шаг: surface={surface} label='{surfaceLabel}' param='{surfaceParameter}' FMOD={rSurf} / {locomotion}"); // Лог шага + результат FMOD
    }

    public static string GetSurfaceLabel(FootstepSurface surface) // Enum поверхности -> лейбл FMOD (статик, чтобы вызывать без экземпляра)
    {
        switch (surface) // Смотрим поверхность
        {
            case FootstepSurface.Concrete: return "Concrete"; // Бетон
            case FootstepSurface.Carpet: return "Carpet"; // Ковёр
            case FootstepSurface.Glass: return "Glass"; // Стекло
            case FootstepSurface.Linoleum: return "linoleum"; // Линолеум (в FMOD с маленькой буквы!)
            case FootstepSurface.Wood: return "Wood"; // Дерево
            default: return "Concrete"; // На всякий случай
        }
    }

    private string GetLocomotionLabel(FootstepLocomotion locomotion) // Enum движения -> лейбл FMOD
    {
        switch (locomotion) // Смотрим тип
        {
            case FootstepLocomotion.Run: return "Run"; // Бег
            case FootstepLocomotion.Crouch: return "Crouch"; // Присед
            default: return "Walk"; // Ходьба
        }
    }
}
