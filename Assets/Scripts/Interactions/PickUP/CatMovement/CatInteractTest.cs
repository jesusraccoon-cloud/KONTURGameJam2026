using UnityEngine; // Подключаем Unity-классы

public class CatInteractTest : MonoBehaviour, IInteractable // Скрипт интеракции с котом через PlayerInteractor
{
    public CatMoveToPoint catMoveScript; // Ссылка на скрипт движения кота

    private void Reset() // Вызывается, когда скрипт добавляют на объект
    {
        catMoveScript = GetComponent<CatMoveToPoint>(); // Пытаемся найти CatMoveToPoint на этом же объекте
    }

    private void Awake() // Вызывается при создании объекта
    {
        if (catMoveScript == null) // Если ссылка не назначена вручную
        {
            catMoveScript = GetComponent<CatMoveToPoint>(); // Пытаемся найти CatMoveToPoint на этом же объекте
        }

        if (catMoveScript == null) // Если на этом объекте не нашли
        {
            catMoveScript = GetComponentInParent<CatMoveToPoint>(); // Пытаемся найти CatMoveToPoint у родителя
        }

        if (catMoveScript == null) // Если у родителя тоже не нашли
        {
            catMoveScript = GetComponentInChildren<CatMoveToPoint>(); // Пытаемся найти CatMoveToPoint у дочерних объектов
        }
    }

    public void Interact() // Метод вызывается PlayerInteractor при коротком нажатии E
    {
        if (catMoveScript == null) // Если скрипт движения кота не назначен
        {
            Debug.Log("CatMoveScript не назначен"); // Пишем сообщение в Console
            return; // Выходим из метода
        }

        Debug.Log("Игрок взаимодействовал с котом"); // Пишем сообщение об успешной интеракции

        catMoveScript.StartMoving(); // Запускаем сценарий движения кота
    }
}