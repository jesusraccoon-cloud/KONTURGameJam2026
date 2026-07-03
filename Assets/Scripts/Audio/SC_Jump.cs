using UnityEngine; // Подключаем Unity-классы
using FMODUnity; // Подключаем FMOD (EventReference, RuntimeManager, RuntimeUtils)
using FMOD.Studio; // Подключаем EventInstance
using StarterAssets; // Подключаем FirstPersonController и StarterAssetsInputs

[RequireComponent(typeof(CharacterController))] // Игрок с CharacterController
public class SC_Jump : MonoBehaviour // Звук прыжка и приземления через FMOD
{
    [Header("References")] // Блок ссылок
    public FirstPersonController controller; // Контроллер игрока: читаем Grounded, canMove

    public StarterAssetsInputs input; // Ввод игрока: читаем нажатие прыжка

    public SC_Footsteps footsteps; // Источник поверхности для приземления (тот же маппинг тегов, что у шагов)

    [Header("FMOD")] // Блок FMOD
    public EventReference jumpEvent; // Событие прыжка (просто подпрыгивание, без поверхности)

    public EventReference landEvent; // Событие приземления (с поверхностью)

    public bool landUsesSurface = true; // Ставить ли параметр Surface на приземлении

    public string surfaceParameter = "Surface"; // Имя параметра поверхности в FMOD (как в Footsteps)

    public SC_Footsteps.FootstepSurface fallbackSurface = SC_Footsteps.FootstepSurface.Concrete; // Поверхность, если SC_Footsteps не назначен

    [Header("Jump Detection")] // Блок определения прыжка
    public bool requireJumpInput = true; // Играть прыжок только по нажатию (иначе сойти с уступа = «прыжок»)

    [Header("Land Detection")] // Блок определения приземления
    public float minAirTime = 0.12f; // Минимум времени в воздухе, чтобы засчитать приземление (гасит дрожание Grounded)

    [Header("Debug")] // Блок отладки
    public bool showDebugLogs = false; // Показывать логи

    private bool wasGrounded = true; // Был ли игрок на земле в прошлом кадре

    private bool jumpQueued = false; // Было ли намерение прыгнуть, пока стояли на земле

    private float airTime = 0f; // Сколько времени игрок в воздухе

    private void Awake() // Вызывается при создании объекта
    {
        if (controller == null) // Если контроллер не назначен
        {
            controller = GetComponent<FirstPersonController>(); // Ищем на этом же объекте
        }

        if (input == null) // Если ввод не назначен
        {
            input = GetComponent<StarterAssetsInputs>(); // Ищем на этом же объекте
        }

        if (footsteps == null) // Если скрипт шагов не назначен
        {
            footsteps = GetComponent<SC_Footsteps>(); // Ищем на этом же объекте
        }
    }

    private void Start() // Вызывается перед первым кадром
    {
        if (controller != null) // Если контроллер найден
        {
            wasGrounded = controller.Grounded; // Запоминаем стартовое состояние земли
        }
    }

    private void Update() // Вызывается каждый кадр
    {
        if (controller == null) return; // Без контроллера работать не можем

        bool grounded = controller.Grounded; // Текущее состояние земли

        if (grounded && input != null && input.jump && controller.canMove) // Пока стоим и нажат прыжок
        {
            jumpQueued = true; // Запоминаем намерение прыгнуть
        }

        if (wasGrounded && !grounded) // Только что оторвались от земли
        {
            if (!requireJumpInput || jumpQueued) // Если это реальный прыжок (или не требуем нажатие)
            {
                PlayJump(); // Играем прыжок
            }

            jumpQueued = false; // Сбрасываем намерение
            airTime = 0f; // Обнуляем время в воздухе
        }
        else if (!grounded) // Если всё ещё в воздухе
        {
            airTime += Time.deltaTime; // Копим время в воздухе
        }
        else if (!wasGrounded && grounded) // Только что приземлились
        {
            if (airTime >= minAirTime) // Если пробыли в воздухе достаточно долго
            {
                PlayLand(); // Играем приземление
            }

            airTime = 0f; // Обнуляем время в воздухе
            jumpQueued = false; // На всякий случай сбрасываем намерение
        }

        wasGrounded = grounded; // Обновляем состояние земли
    }

    private void PlayJump() // Проигрывание звука прыжка
    {
        if (jumpEvent.IsNull) // Если событие не назначено
        {
            if (showDebugLogs) Debug.LogWarning("SC_Jump: не назначено FMOD-событие Jump"); // Предупреждение
            return; // Выходим
        }

        EventInstance instance = RuntimeManager.CreateInstance(jumpEvent); // Создаём экземпляр
        instance.set3DAttributes(RuntimeUtils.To3DAttributes(transform.position)); // Позиция в пространстве
        instance.start(); // Запускаем звук
        instance.release(); // Освобождаем (one-shot)

        if (showDebugLogs) Debug.Log("Прыжок"); // Лог
    }

    private void PlayLand() // Проигрывание звука приземления
    {
        if (landEvent.IsNull) // Если событие не назначено
        {
            if (showDebugLogs) Debug.LogWarning("SC_Jump: не назначено FMOD-событие Land"); // Предупреждение
            return; // Выходим
        }

        EventInstance instance = RuntimeManager.CreateInstance(landEvent); // Создаём экземпляр
        instance.set3DAttributes(RuntimeUtils.To3DAttributes(transform.position)); // Позиция в пространстве

        if (landUsesSurface) // Если нужна поверхность
        {
            string label = footsteps != null // Есть ли скрипт шагов
                ? footsteps.GetCurrentSurfaceLabel() // Берём поверхность из общего маппинга тегов
                : SC_Footsteps.GetSurfaceLabel(fallbackSurface); // Иначе fallback

            instance.setParameterByNameWithLabel(surfaceParameter, label); // Ставим поверхность по лейблу
        }

        instance.start(); // Запускаем звук
        instance.release(); // Освобождаем (one-shot)

        if (showDebugLogs) Debug.Log("Приземление"); // Лог
    }
}
