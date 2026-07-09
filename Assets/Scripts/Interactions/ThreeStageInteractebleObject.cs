using UnityEngine; // Подключаем Unity, чтобы использовать MonoBehaviour, GameObject, Transform, Rigidbody и другие классы

public class ThreeStageInteractableObject : MonoBehaviour, IInteractable, IHitInteractable // Объект с 3 стадиями, работает от E и от удара
{
    [Header("Stages")] // Заголовок в Inspector для настроек стадий
    public GameObject stageWhole; // Визуал первой стадии: целый объект
    public GameObject stageBroken; // Визуал второй стадии: поломанный объект
    public GameObject stageDestroyed; // Визуал третьей стадии: уничтоженный объект, можно оставить пустым

    [Header("Interaction Settings")] // Заголовок в Inspector для настроек взаимодействия
    public bool canUseE = true; // Можно ли переводить объект на следующую стадию через клавишу E
    public bool canUseHit = true; // Можно ли переводить объект на следующую стадию ударом
    public int ePressesForNextStage = 1; // Сколько раз нужно нажать E для перехода на следующую стадию
    public int hitsForNextStage = 1; // Сколько ударов нужно для перехода на следующую стадию

    [Header("Spawn On Stage 1 -> 2")] // Заголовок в Inspector для предметов после первого разрушения
    public GameObject[] spawnAfterFirstBreak; // Предметы, которые появятся после перехода из целого состояния в поломанное
    public Transform firstSpawnPoint; // Точка, из которой будут вылетать предметы после первого разрушения

    [Header("Spawn On Stage 2 -> 3")] // Заголовок в Inspector для предметов после финального разрушения
    public GameObject[] spawnAfterDestroy; // Предметы, которые появятся после перехода из поломанного состояния в уничтоженное
    public Transform secondSpawnPoint; // Точка, из которой будут вылетать предметы после финального разрушения

    [Header("Throw Settings")] // Заголовок в Inspector для настроек выброса предметов
    public float throwForce = 3f; // Основная сила, с которой предметы будут вылетать вперед от объекта
    public float upwardForce = 1.5f; // Насколько сильно предметы будут подлетать вверх
    public float randomSideForce = 1f; // Насколько сильно предметы будут случайно разлетаться влево и вправо
    public float spawnScatterRadius = 0.2f; // Насколько случайно будет смещаться точка появления каждого предмета
    public float torqueForce = 3f; // Сила случайного вращения, чтобы осколки выглядели живее при падении

    [Header("Final Settings")] // Заголовок в Inspector для настроек финальной стадии
    public bool disableColliderWhenDestroyed = true; // Нужно ли отключать коллайдер после полного уничтожения объекта
    public Collider objectCollider; // Коллайдер основного объекта, который можно отключить после уничтожения

    private int currentStage = 0; // Текущая стадия объекта: 0 — целый, 1 — поломанный, 2 — уничтоженный
    private int currentEPresses = 0; // Счетчик нажатий E на текущей стадии
    private int currentHits = 0; // Счетчик ударов на текущей стадии

    private void Start() // Метод запускается один раз при старте сцены
    {
        ApplyStageVisuals(); // Сразу выставляем правильный визуал в зависимости от текущей стадии
    }

    public void Interact() // Метод вызывается другим скриптом, когда игрок нажимает E по объекту
    {
        if (canUseE == false) return; // Если взаимодействие через E запрещено, выходим из метода
        if (currentStage >= 2) return; // Если объект уже уничтожен, больше ничего не делаем

        currentEPresses++; // Увеличиваем счетчик нажатий E на 1

        if (currentEPresses >= ePressesForNextStage) // Проверяем, достаточно ли нажатий для перехода на следующую стадию
        {
            currentEPresses = 0; // Сбрасываем счетчик нажатий E
            AdvanceStage(); // Переводим объект на следующую стадию
        }
    }

    public void Hit() // Метод вызывается другим скриптом, когда игрок ударяет объект
    {
        if (canUseHit == false) return; // Если взаимодействие ударом запрещено, выходим из метода
        if (currentStage >= 2) return; // Если объект уже уничтожен, больше ничего не делаем

        currentHits++; // Увеличиваем счетчик ударов на 1

        if (currentHits >= hitsForNextStage) // Проверяем, достаточно ли ударов для перехода на следующую стадию
        {
            currentHits = 0; // Сбрасываем счетчик ударов
            AdvanceStage(); // Переводим объект на следующую стадию
        }
    }

    private void AdvanceStage() // Метод переводит объект на следующую стадию
    {
        currentStage++; // Увеличиваем номер текущей стадии на 1

        if (currentStage == 1) // Проверяем, перешел ли объект на стадию поломки
        {
            SpawnObjects(spawnAfterFirstBreak, firstSpawnPoint); // Создаем и выбрасываем предметы первой поломки
        }

        if (currentStage == 2) // Проверяем, перешел ли объект на стадию полного уничтожения
        {
            SpawnObjects(spawnAfterDestroy, secondSpawnPoint); // Создаем и выбрасываем предметы финального уничтожения
        }

        ApplyStageVisuals(); // Обновляем видимые модели объекта после смены стадии
    }

    private void ApplyStageVisuals() // Метод включает нужный визуал стадии и выключает остальные
    {
        if (stageWhole != null) stageWhole.SetActive(currentStage == 0); // Включаем целый визуал только на стадии 0
        if (stageBroken != null) stageBroken.SetActive(currentStage == 1); // Включаем поломанный визуал только на стадии 1
        if (stageDestroyed != null) stageDestroyed.SetActive(currentStage == 2); // Включаем уничтоженный визуал только на стадии 2

        if (currentStage >= 2 && disableColliderWhenDestroyed == true && objectCollider != null) // Проверяем, нужно ли отключить коллайдер после уничтожения
        {
            objectCollider.enabled = false; // Отключаем коллайдер, чтобы уничтоженный объект не мешал игроку
        }
    }

    private void SpawnObjects(GameObject[] objectsToSpawn, Transform spawnPoint) // Метод создает предметы и выбрасывает их недалеко от объекта
    {
        if (objectsToSpawn == null) return; // Если массив предметов не назначен, выходим из метода
        if (spawnPoint == null) return; // Если точка появления не назначена, выходим из метода

        for (int i = 0; i < objectsToSpawn.Length; i++) // Проходим по всем предметам, которые нужно создать
        {
            if (objectsToSpawn[i] == null) continue; // Если конкретный предмет не назначен, пропускаем его

            Vector3 randomOffset = new Vector3( // Создаем случайное смещение, чтобы предметы не появлялись строго в одной точке
                Random.Range(-spawnScatterRadius, spawnScatterRadius), // Случайное смещение по оси X
                Random.Range(0f, spawnScatterRadius), // Случайное смещение немного вверх по оси Y
                Random.Range(-spawnScatterRadius, spawnScatterRadius) // Случайное смещение по оси Z
            );

            GameObject spawnedObject = Instantiate( // Создаем предмет в сцене
                objectsToSpawn[i], // Берем prefab из массива предметов
                spawnPoint.position + randomOffset, // Ставим его в точку спавна с небольшим случайным смещением
                Random.rotation // Даем предмету случайный поворот при появлении
            );

            Rigidbody spawnedRigidbody = spawnedObject.GetComponent<Rigidbody>(); // Пытаемся найти Rigidbody на созданном предмете

            if (spawnedRigidbody != null) // Проверяем, есть ли Rigidbody у созданного предмета
            {
                Vector3 throwDirection = transform.forward; // Берем направление вперед от основного объекта

                throwDirection += transform.right * Random.Range(-randomSideForce, randomSideForce); // Добавляем случайное отклонение влево или вправо
                throwDirection += Vector3.up * upwardForce; // Добавляем направление вверх, чтобы предметы немного подлетали

                throwDirection.Normalize(); // Нормализуем направление, чтобы сила не становилась слишком большой из-за сложения векторов

                spawnedRigidbody.AddForce(throwDirection * throwForce, ForceMode.Impulse); // Придаем предмету резкий импульс в рассчитанном направлении
                spawnedRigidbody.AddTorque(Random.insideUnitSphere * torqueForce, ForceMode.Impulse); // Добавляем случайное вращение, чтобы предмет крутился в воздухе
            }
        }
    }
}