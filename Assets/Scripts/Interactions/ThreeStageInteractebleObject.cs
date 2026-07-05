using UnityEngine; // Подключаем Unity

public class ThreeStageInteractableObject : MonoBehaviour, IInteractable, IHitInteractable // Объект с 3 стадиями, работает от E и от удара
{
    [Header("Stages")] public GameObject stageWhole; // Визуал первой стадии: целое
    public GameObject stageBroken; // Визуал второй стадии: поломанное
    public GameObject stageDestroyed; // Визуал третьей стадии: разбитое, можно оставить пустым

    [Header("Interaction Settings")] public bool canUseE = true; // Можно ли ломать через E
    public bool canUseHit = true; // Можно ли ломать через удар
    public int ePressesForNextStage = 1; // Сколько раз нажать E для перехода стадии
    public int hitsForNextStage = 1; // Сколько ударов нужно для перехода стадии

    [Header("Spawn On Stage 1 -> 2")] public GameObject[] spawnAfterFirstBreak; // Что появляется после перехода целое → поломанное
    public Transform firstSpawnPoint; // Точка появления предметов после первого ломания

    [Header("Spawn On Stage 2 -> 3")] public GameObject[] spawnAfterDestroy; // Что появляется после перехода поломанное → разбитое
    public Transform secondSpawnPoint; // Точка появления предметов после полного уничтожения

    [Header("Final Settings")] public bool disableColliderWhenDestroyed = true; // Отключить коллайдер после финальной стадии
    public Collider objectCollider; // Коллайдер объекта

    private int currentStage = 0; // Текущая стадия: 0 целое, 1 поломанное, 2 разбитое
    private int currentEPresses = 0; // Счетчик нажатий E
    private int currentHits = 0; // Счетчик ударов

    private void Start() // Запускается при старте сцены
    {
        ApplyStageVisuals(); // Сразу выставляем правильный визуал
    }

    public void Interact() // Метод вызывается PlayerInteractor при нажатии E
    {
        if (canUseE == false) return; // Если E запрещена, ничего не делаем
        if (currentStage >= 2) return; // Если объект уже уничтожен, ничего не делаем

        currentEPresses++; // Добавляем одно нажатие E

        if (currentEPresses >= ePressesForNextStage) // Если нажатий достаточно
        {
            currentEPresses = 0; // Сбрасываем счетчик E
            AdvanceStage(); // Переводим объект на следующую стадию
        }
    }

    public void Hit() // Метод вызывается PlayerInteractor при ударе ЛКМ
    {
        if (canUseHit == false) return; // Если удары запрещены, ничего не делаем
        if (currentStage >= 2) return; // Если объект уже уничтожен, ничего не делаем

        currentHits++; // Добавляем один удар

        if (currentHits >= hitsForNextStage) // Если ударов достаточно
        {
            currentHits = 0; // Сбрасываем счетчик ударов
            AdvanceStage(); // Переводим объект на следующую стадию
        }
    }

    private void AdvanceStage() // Переход на следующую стадию
    {
        currentStage++; // Увеличиваем стадию на 1

        if (currentStage == 1) // Если перешли на стадию поломки
        {
            SpawnObjects(spawnAfterFirstBreak, firstSpawnPoint); // Создаем предметы первой поломки
        }

        if (currentStage == 2) // Если перешли на стадию уничтожения
        {
            SpawnObjects(spawnAfterDestroy, secondSpawnPoint); // Создаем предметы финального уничтожения
        }

        ApplyStageVisuals(); // Обновляем визуал объекта
    }

    private void ApplyStageVisuals() // Включает нужную модель стадии
    {
        if (stageWhole != null) stageWhole.SetActive(currentStage == 0); // Целый визуал активен только на стадии 0
        if (stageBroken != null) stageBroken.SetActive(currentStage == 1); // Поломанный визуал активен только на стадии 1
        if (stageDestroyed != null) stageDestroyed.SetActive(currentStage == 2); // Разбитый визуал активен только на стадии 2

        if (currentStage >= 2 && disableColliderWhenDestroyed == true && objectCollider != null) // Если объект уничтожен и коллайдер нужно отключить
        {
            objectCollider.enabled = false; // Отключаем коллайдер, чтобы он не мешал игроку
        }
    }

    private void SpawnObjects(GameObject[] objectsToSpawn, Transform spawnPoint) // Создает предметы на полу
    {
        if (objectsToSpawn == null) return; // Если список пустой, ничего не делаем
        if (spawnPoint == null) return; // Если точка спавна не назначена, ничего не делаем

        for (int i = 0; i < objectsToSpawn.Length; i++) // Проходим по всем предметам
        {
            if (objectsToSpawn[i] == null) continue; // Если предмет не назначен, пропускаем

            Instantiate(objectsToSpawn[i], spawnPoint.position, spawnPoint.rotation); // Создаем предмет в точке спавна
        }
    }
}