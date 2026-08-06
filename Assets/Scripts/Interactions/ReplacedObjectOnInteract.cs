using UnityEngine; // Подключаем Unity-классы, чтобы работать с объектами, коллайдерами и MonoBehaviour

public class ReplaceObjectOnInteract : MonoBehaviour, IInteractable // Создаем интерактивный скрипт замены объекта через E
{
    public string hint = "Взаимодействовать";
    public string Hint => hint;
    [Header("Objects To Disable")] // Заголовок в Inspector для объектов, которые нужно выключить
    public GameObject[] objectsToDisable; // Сюда кладем оригинальный объект, например картину на стене

    [Header("Objects To Enable")] // Заголовок в Inspector для объектов, которые нужно включить
    public GameObject[] objectsToEnable; // Сюда кладем замену, например картину уже лежащую внизу

    [Header("Start State")] // Заголовок в Inspector для стартового состояния
    public bool disableObjectsToEnableOnStart = true; // Нужно ли автоматически выключать замену при старте сцены

    public bool disableThisObjectIfDisableListEmpty = true; // Если список выключаемых объектов пустой, выключить объект со скриптом

    [Header("Interaction")] // Заголовок в Inspector для настроек взаимодействия
    public bool canUseOnlyOnce = true; // Можно ли использовать интеракцию только один раз

    public bool wasUsed = false; // Запоминаем, была ли интеракция уже использована

    public Collider interactCollider; // Коллайдер, по которому игрок нажимает E

    public bool disableInteractColliderAfterUse = true; // Нужно ли отключить коллайдер после использования

    [Header("Save")] // Заголовок в Inspector для сохранения
    [SerializeField] private SC_Saveable saveable; // Компонент сохранения (авто-поиск). Через его ID запоминается, что замена уже произошла.

    [Header("Debug")] // Заголовок в Inspector для отладки
    public bool showDebugLogs = true; // Показывать ли сообщения в Console

    private void Reset() // Вызывается в редакторе Unity, когда скрипт добавляют на объект
    {
        interactCollider = GetComponent<Collider>(); // Автоматически подставляем коллайдер с этого объекта
    }

    private void Start() // Запускается при старте сцены
    {
        if (saveable == null) saveable = GetComponent<SC_Saveable>(); // Ищем SC_Saveable на объекте
        if (saveable == null) saveable = GetComponentInChildren<SC_Saveable>(true); // Или на дочернем

        if (disableObjectsToEnableOnStart == true) // Проверяем, нужно ли выключать замену при старте
        {
            SetObjectsActive(objectsToEnable, false); // Выключаем все объекты, которые должны появиться после интеракции
        }

        if (saveable != null && SC_SaveSystem.TryGetState(saveable.id, out int st) && st == 1) // Замена уже была сделана до загрузки?
        {
            wasUsed = true; // Помечаем использованным
            ApplyReplacement(); // Сразу приводим объект к заменённому виду
        }
    }

    public void Interact() // Метод вызывается PlayerInteractor при нажатии E
    {
        if (canUseOnlyOnce == true && wasUsed == true) return; // Если объект уже использовали и повтор запрещен, выходим

        wasUsed = true; // Запоминаем, что интеракция уже произошла

        if (saveable != null) SC_SaveSystem.SetState(saveable.id, 1); // Сохраняем факт замены

        ApplyReplacement(); // Выполняем замену

        if (showDebugLogs == true) Debug.Log("ReplaceObjectOnInteract: объект заменен", this); // Пишем в Console, что замена сработала
    }

    private void ApplyReplacement() // Применить замену (включить замену, выключить оригинал/коллайдер)
    {
        if (disableInteractColliderAfterUse == true && interactCollider != null) // Проверяем, нужно ли отключить коллайдер
        {
            interactCollider.enabled = false; // Отключаем коллайдер, чтобы по объекту нельзя было нажать повторно
        }

        SetObjectsActive(objectsToEnable, true); // Включаем замену объекта, например картину внизу

        if (objectsToDisable != null && objectsToDisable.Length > 0) // Проверяем, есть ли вручную назначенные объекты для выключения
        {
            SetObjectsActive(objectsToDisable, false); // Выключаем оригинальные объекты
        }
        else if (disableThisObjectIfDisableListEmpty == true) // Если список пустой, проверяем, можно ли выключить объект со скриптом
        {
            gameObject.SetActive(false); // Выключаем объект, на котором висит этот скрипт
        }
    }

    private void SetObjectsActive(GameObject[] objects, bool activeState) // Метод включает или выключает список объектов
    {
        if (objects == null) return; // Если список пустой, ничего не делаем

        for (int i = 0; i < objects.Length; i++) // Проходим по всем объектам в списке
        {
            if (objects[i] == null) continue; // Если ячейка пустая, пропускаем ее

            objects[i].SetActive(activeState); // Включаем или выключаем объект
        }
    }
}