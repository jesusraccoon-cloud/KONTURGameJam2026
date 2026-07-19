using UnityEngine; // Подключаем Unity, чтобы использовать MonoBehaviour, GameObject, Transform, Rigidbody и другие классы

public class ThreeStageInteractableObject : MonoBehaviour, IInteractable, IHitInteractable // Объект с тремя стадиями, который работает от E и от удара
{
    [Header("Stages")] // Заголовок в Inspector для настроек стадий

    public GameObject stageWhole; // Визуал первой стадии: целый объект

    public GameObject stageBroken; // Визуал второй стадии: повреждённый объект

    public GameObject stageDestroyed; // Визуал третьей стадии: полностью уничтоженный объект, можно оставить пустым


    [Header("Interaction Settings")] // Заголовок в Inspector для настроек взаимодействия

    public bool canUseE = true; // Можно ли переводить объект на следующую стадию через клавишу E

    public bool canUseHit = true; // Можно ли переводить объект на следующую стадию ударом

    [Min(1)] // Не позволяет указать значение меньше одного в Inspector

    public int ePressesForNextStage = 1; // Сколько раз нужно нажать E для перехода на следующую стадию

    [Min(1)] // Не позволяет указать значение меньше одного в Inspector

    public int hitsForNextStage = 1; // Сколько ударов нужно для перехода на следующую стадию


    [Header("Spawn On Stage 1 -> 2")] // Заголовок в Inspector для предметов после первого разрушения

    public GameObject[] spawnAfterFirstBreak; // Предметы, которые появятся после перехода из целого состояния в повреждённое

    public Transform firstSpawnPoint; // Точка, из которой будут вылетать предметы после первого разрушения


    [Header("Spawn On Stage 2 -> 3")] // Заголовок в Inspector для предметов после финального разрушения

    public GameObject[] spawnAfterDestroy; // Предметы, которые появятся после перехода из повреждённого состояния в уничтоженное

    public Transform secondSpawnPoint; // Точка, из которой будут вылетать предметы после финального разрушения


    [Header("Throw Settings")] // Заголовок в Inspector для настроек выброса предметов

    public float throwForce = 3f; // Основная сила, с которой предметы будут вылетать вперёд от объекта

    public float upwardForce = 1.5f; // Насколько сильно предметы будут подлетать вверх

    public float randomSideForce = 1f; // Насколько сильно предметы будут случайно разлетаться влево и вправо

    public float spawnScatterRadius = 0.2f; // Насколько случайно будет смещаться точка появления каждого предмета

    public float torqueForce = 3f; // Сила случайного вращения предметов


    [Header("Final Settings")] // Заголовок в Inspector для настроек финальной стадии

    public bool disableColliderWhenDestroyed = true; // Нужно ли отключать основной коллайдер после полного уничтожения объекта

    public Collider objectCollider; // Коллайдер основного объекта, который можно отключить после уничтожения


    [Header("Debug")] // Заголовок в Inspector для просмотра текущего состояния

    [SerializeField] // Показываем значение в Inspector, но не даём другим скриптам изменять его напрямую

    private int currentStage = 0; // Текущая стадия: 0 — целый, 1 — повреждённый, 2 — уничтоженный


    private int currentEPresses = 0; // Счётчик нажатий E на текущей стадии

    private int currentHits = 0; // Счётчик ударов на текущей стадии


    public int CurrentStage // Открытое свойство, через которое другие скрипты могут узнать текущую стадию
    {
        get // Позволяем только получить значение
        {
            return currentStage; // Возвращаем текущую стадию объекта
        }
    }


    public bool IsDestroyed // Открытое свойство, которое сообщает, достиг ли объект третьей стадии
    {
        get // Позволяем только получить значение
        {
            return currentStage >= 2; // Возвращаем true, только если объект находится на третьей стадии
        }
    }


    private void Start() // Метод запускается один раз при старте сцены
    {
        currentStage = Mathf.Clamp(currentStage, 0, 2); // Ограничиваем стадию значениями от нуля до двух

        ApplyStageVisuals(); // Сразу выставляем правильный визуал в зависимости от текущей стадии
    }


    public void Interact() // Метод вызывается PlayerInteractor, когда игрок нажимает E по объекту
    {
        if (canUseE == false) return; // Если взаимодействие через E запрещено, прекращаем выполнение

        if (IsDestroyed) return; // Если объект уже уничтожен, больше ничего не делаем

        currentEPresses++; // Увеличиваем счётчик нажатий E на один

        if (currentEPresses >= Mathf.Max(1, ePressesForNextStage)) // Проверяем, достаточно ли нажатий для следующей стадии
        {
            currentEPresses = 0; // Сбрасываем счётчик нажатий E

            currentHits = 0; // Сбрасываем счётчик ударов, чтобы способы взаимодействия не смешивались

            AdvanceStage(); // Переводим объект на следующую стадию
        }
    }


    public void Hit() // Метод вызывается PlayerInteractor, когда игрок ударяет объект
    {
        if (canUseHit == false) return; // Если взаимодействие ударом запрещено, прекращаем выполнение

        if (IsDestroyed) return; // Если объект уже уничтожен, больше ничего не делаем

        currentHits++; // Увеличиваем счётчик ударов на один

        if (currentHits >= Mathf.Max(1, hitsForNextStage)) // Проверяем, достаточно ли ударов для следующей стадии
        {
            currentHits = 0; // Сбрасываем счётчик ударов

            currentEPresses = 0; // Сбрасываем счётчик нажатий E, чтобы способы взаимодействия не смешивались

            AdvanceStage(); // Переводим объект на следующую стадию
        }
    }


    private void AdvanceStage() // Метод переводит объект на следующую стадию
    {
        if (IsDestroyed) return; // Дополнительная защита от перехода дальше третьей стадии

        currentStage++; // Увеличиваем номер текущей стадии на один

        currentStage = Mathf.Clamp(currentStage, 0, 2); // Ограничиваем текущую стадию значением два

        if (currentStage == 1) // Проверяем, перешёл ли объект на повреждённую стадию
        {
            SpawnObjects(spawnAfterFirstBreak, firstSpawnPoint); // Создаём и выбрасываем предметы первой поломки
        }

        if (currentStage == 2) // Проверяем, перешёл ли объект на полностью уничтоженную стадию
        {
            SpawnObjects(spawnAfterDestroy, secondSpawnPoint); // Создаём и выбрасываем предметы финального разрушения
        }

        ApplyStageVisuals(); // Обновляем видимые модели объекта после смены стадии
    }


    private void ApplyStageVisuals() // Метод включает нужный визуал стадии и выключает остальные
    {
        if (stageWhole != null) // Проверяем, назначена ли модель целой стадии
        {
            stageWhole.SetActive(currentStage == 0); // Включаем целый визуал только на стадии ноль
        }

        if (stageBroken != null) // Проверяем, назначена ли модель повреждённой стадии
        {
            stageBroken.SetActive(currentStage == 1); // Включаем повреждённый визуал только на стадии один
        }

        if (stageDestroyed != null) // Проверяем, назначена ли модель уничтоженной стадии
        {
            stageDestroyed.SetActive(currentStage == 2); // Включаем уничтоженный визуал только на стадии два
        }

        if (objectCollider != null) // Проверяем, назначен ли основной коллайдер
        {
            if (IsDestroyed && disableColliderWhenDestroyed) // Проверяем, уничтожен ли объект и нужно ли отключать коллайдер
            {
                objectCollider.enabled = false; // Отключаем коллайдер после полного уничтожения
            }
            else // Выполняем, если объект ещё не уничтожен или отключение коллайдера запрещено
            {
                objectCollider.enabled = true; // Оставляем коллайдер включённым
            }
        }
    }


    private void SpawnObjects(GameObject[] objectsToSpawn, Transform spawnPoint) // Метод создаёт предметы и выбрасывает их рядом с объектом
    {
        if (objectsToSpawn == null) return; // Если массив предметов отсутствует, прекращаем выполнение

        if (objectsToSpawn.Length == 0) return; // Если массив пустой, прекращаем выполнение

        if (spawnPoint == null) return; // Если точка появления не назначена, прекращаем выполнение

        for (int i = 0; i < objectsToSpawn.Length; i++) // Проходим по всем предметам, которые нужно создать
        {
            if (objectsToSpawn[i] == null) continue; // Если конкретный prefab не назначен, пропускаем его

            Vector3 randomOffset = new Vector3( // Создаём случайное смещение точки появления
                Random.Range(-spawnScatterRadius, spawnScatterRadius), // Добавляем случайное смещение по оси X
                Random.Range(0f, spawnScatterRadius), // Добавляем небольшое случайное смещение вверх
                Random.Range(-spawnScatterRadius, spawnScatterRadius) // Добавляем случайное смещение по оси Z
            );

            GameObject spawnedObject = Instantiate( // Создаём новый предмет в сцене
                objectsToSpawn[i], // Используем prefab из массива
                spawnPoint.position + randomOffset, // Устанавливаем позицию с небольшим случайным смещением
                Random.rotation // Устанавливаем случайный поворот
            );

            Rigidbody spawnedRigidbody = spawnedObject.GetComponent<Rigidbody>(); // Ищем Rigidbody на корневом объекте созданного предмета

            if (spawnedRigidbody == null) // Проверяем, не находится ли Rigidbody на дочернем объекте
            {
                spawnedRigidbody = spawnedObject.GetComponentInChildren<Rigidbody>(); // Ищем Rigidbody среди дочерних объектов
            }

            if (spawnedRigidbody != null) // Проверяем, найден ли Rigidbody
            {
                Vector3 throwDirection = transform.forward; // Берём направление вперёд от основного объекта

                throwDirection += transform.right * Random.Range(-randomSideForce, randomSideForce); // Добавляем случайное отклонение влево или вправо

                throwDirection += Vector3.up * upwardForce; // Добавляем направление вверх

                throwDirection.Normalize(); // Нормализуем направление, чтобы сила оставалась стабильной

                spawnedRigidbody.AddForce(throwDirection * throwForce, ForceMode.Impulse); // Придаём предмету резкий импульс

                spawnedRigidbody.AddTorque(Random.insideUnitSphere * torqueForce, ForceMode.Impulse); // Добавляем случайное вращение
            }
        }
    }
}