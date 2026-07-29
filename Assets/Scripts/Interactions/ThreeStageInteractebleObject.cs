using UnityEngine; // Подключаем Unity, чтобы использовать MonoBehaviour, GameObject, Collider и другие классы.

public class ThreeStageInteractableObject : MonoBehaviour, IInteractable, IHitInteractable // Объект с тремя стадиями, который работает от E и от удара.
{
    [Header("Stages")] // Заголовок в Inspector для настроек стадий.

    public GameObject stageWhole; // Визуал первой стадии: целый объект.

    public GameObject stageBroken; // Визуал второй стадии: повреждённый объект.

    public GameObject stageDestroyed; // Визуал третьей стадии: полностью уничтоженный объект, можно оставить пустым.


    [Header("Interaction Settings")] // Заголовок в Inspector для настроек взаимодействия.

    public bool canUseE = true; // Можно ли взаимодействовать с объектом через клавишу E.

    public bool canUseHit = true; // Можно ли взаимодействовать с объектом ударом.

    [Min(1)] // Не позволяет указать значение меньше одного в Inspector.

    public int ePressesForNextStage = 1; // Сколько нажатий E нужно для перехода на следующую стадию.

    [Min(1)] // Не позволяет указать значение меньше одного в Inspector.

    public int hitsForNextStage = 1; // Сколько ударов нужно для перехода на следующую стадию.


    [Header("Objects Activated One By One")] // Заголовок для заранее размещённых предметов.

    public GameObject[] objectsToActivate; // Предметы, которые будут включаться по одному в указанном порядке.

    public bool disableObjectsOnStart = true; // Нужно ли автоматически выключить все назначенные предметы при запуске сцены.


    [Header("Final Settings")] // Заголовок в Inspector для настроек финальной стадии.

    public bool disableColliderWhenDestroyed = true; // Нужно ли отключать основной коллайдер после полного уничтожения объекта.

    public Collider objectCollider; // Коллайдер основного объекта, который можно отключить после уничтожения.


    [Header("Debug")] // Заголовок в Inspector для просмотра текущего состояния.

    [SerializeField] // Показываем значение в Inspector, но не даём другим скриптам менять его напрямую.

    private int currentStage = 0; // Текущая стадия: 0 — целый, 1 — повреждённый, 2 — уничтоженный.

    [SerializeField] // Показываем индекс следующего предмета в Inspector для отладки.

    private int nextObjectIndex = 0; // Индекс предмета, который будет включён при следующем взаимодействии.


    private int currentEPresses = 0; // Счётчик нажатий E на текущей стадии.

    private int currentHits = 0; // Счётчик ударов на текущей стадии.


    public int CurrentStage // Открытое свойство, через которое другие скрипты могут узнать текущую стадию.
    {
        get // Позволяем только получить значение.
        {
            return currentStage; // Возвращаем текущую стадию объекта.
        }
    }


    public bool IsDestroyed // Открытое свойство, которое сообщает, достиг ли объект третьей стадии.
    {
        get // Позволяем только получить значение.
        {
            return currentStage >= 2; // Возвращаем true, только если объект находится на третьей стадии.
        }
    }


    private void Start() // Метод запускается один раз при старте сцены.
    {
        currentStage = Mathf.Clamp(currentStage, 0, 2); // Ограничиваем стадию значениями от нуля до двух.

        nextObjectIndex = 0; // Начинаем включение предметов с первого элемента массива.

        if (disableObjectsOnStart == true) // Проверяем, нужно ли выключить предметы автоматически.
        {
            DisableAllActivationObjects(); // Выключаем все заранее размещённые предметы.
        }

        ApplyStageVisuals(); // Сразу выставляем правильный визуал в зависимости от текущей стадии.
    }


    public void Interact() // Метод вызывается PlayerInteractor, когда игрок нажимает E по объекту.
    {
        if (canUseE == false) return; // Если взаимодействие через E запрещено, прекращаем выполнение.

        if (IsDestroyed) return; // Если объект уже уничтожен, больше ничего не делаем.

        ActivateNextObject(); // Включаем один следующий предмет после каждого успешного нажатия E.

        currentEPresses++; // Увеличиваем счётчик нажатий E на один.

        if (currentEPresses >= Mathf.Max(1, ePressesForNextStage)) // Проверяем, достаточно ли нажатий для следующей стадии.
        {
            currentEPresses = 0; // Сбрасываем счётчик нажатий E.

            currentHits = 0; // Сбрасываем счётчик ударов, чтобы способы взаимодействия не смешивались.

            AdvanceStage(); // Переводим объект на следующую стадию.
        }
    }


    public void Hit() // Метод вызывается PlayerInteractor, когда игрок ударяет объект.
    {
        if (canUseHit == false) return; // Если взаимодействие ударом запрещено, прекращаем выполнение.

        if (IsDestroyed) return; // Если объект уже уничтожен, больше ничего не делаем.

        ActivateNextObject(); // Включаем один следующий предмет после каждого успешного удара.

        currentHits++; // Увеличиваем счётчик ударов на один.

        if (currentHits >= Mathf.Max(1, hitsForNextStage)) // Проверяем, достаточно ли ударов для следующей стадии.
        {
            currentHits = 0; // Сбрасываем счётчик ударов.

            currentEPresses = 0; // Сбрасываем счётчик нажатий E, чтобы способы взаимодействия не смешивались.

            AdvanceStage(); // Переводим объект на следующую стадию.
        }
    }


    private void ActivateNextObject() // Метод включает ровно один следующий предмет из массива.
    {
        if (objectsToActivate == null) return; // Если массив не назначен, прекращаем выполнение.

        if (objectsToActivate.Length == 0) return; // Если массив пустой, прекращаем выполнение.

        while (nextObjectIndex < objectsToActivate.Length) // Ищем следующий корректно назначенный предмет.
        {
            GameObject objectToActivate = objectsToActivate[nextObjectIndex]; // Получаем текущий предмет из массива.

            nextObjectIndex++; // Сразу переходим к следующему индексу для будущего взаимодействия.

            if (objectToActivate == null) // Проверяем, не оставлено ли пустое поле в массиве.
            {
                continue; // Пропускаем пустое поле и ищем следующий предмет.
            }

            objectToActivate.SetActive(true); // Включаем заранее размещённый предмет в сцене.

            return; // После включения одного предмета прекращаем выполнение метода.
        }
    }


    private void DisableAllActivationObjects() // Метод выключает все назначенные предметы при запуске сцены.
    {
        if (objectsToActivate == null) return; // Если массив не назначен, прекращаем выполнение.

        for (int i = 0; i < objectsToActivate.Length; i++) // Проходим по всем предметам массива.
        {
            if (objectsToActivate[i] == null) continue; // Пропускаем пустые элементы массива.

            objectsToActivate[i].SetActive(false); // Выключаем предмет до первого нужного взаимодействия.
        }
    }


    private void AdvanceStage() // Метод переводит объект на следующую стадию.
    {
        if (IsDestroyed) return; // Дополнительная защита от перехода дальше третьей стадии.

        currentStage++; // Увеличиваем номер текущей стадии на один.

        currentStage = Mathf.Clamp(currentStage, 0, 2); // Ограничиваем текущую стадию значением два.

        ApplyStageVisuals(); // Обновляем видимые модели объекта после смены стадии.
    }


    private void ApplyStageVisuals() // Метод включает нужный визуал стадии и выключает остальные.
    {
        if (stageWhole != null) // Проверяем, назначена ли модель целой стадии.
        {
            stageWhole.SetActive(currentStage == 0); // Включаем целый визуал только на стадии ноль.
        }

        if (stageBroken != null) // Проверяем, назначена ли модель повреждённой стадии.
        {
            stageBroken.SetActive(currentStage == 1); // Включаем повреждённый визуал только на стадии один.
        }

        if (stageDestroyed != null) // Проверяем, назначена ли модель уничтоженной стадии.
        {
            stageDestroyed.SetActive(currentStage == 2); // Включаем уничтоженный визуал только на стадии два.
        }

        if (objectCollider != null) // Проверяем, назначен ли основной коллайдер.
        {
            if (IsDestroyed && disableColliderWhenDestroyed) // Проверяем, уничтожен ли объект и нужно ли отключать коллайдер.
            {
                objectCollider.enabled = false; // Отключаем коллайдер после полного уничтожения.
            }
            else // Выполняем, если объект ещё не уничтожен или отключение коллайдера запрещено.
            {
                objectCollider.enabled = true; // Оставляем коллайдер включённым.
            }
        }
    }
}