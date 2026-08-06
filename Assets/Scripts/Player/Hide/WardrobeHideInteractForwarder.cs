using UnityEngine; // Подключаем Unity-классы

public class WardrobeHideInteractForwarder : MonoBehaviour, IInteractable // Передатчик взаимодействия зоны шкафа
{
    public string hint = "Взаимодействовать";
    public string Hint => hint;
    public WardrobeHideHandle wardrobe; // Ссылка на шкаф

    public WardrobeHideZoneType zoneType = WardrobeHideZoneType.DoorZone; // Тип зоны

    public int doorIndex = -1; // Индекс двери для дверной зоны

    public void Interact() // Нажатие E
    {
        if (wardrobe == null) wardrobe = GetComponentInParent<WardrobeHideHandle>(); // Ищем шкаф выше

        if (wardrobe == null) return; // Если шкафа нет, выходим

        if (zoneType == WardrobeHideZoneType.InsideHideZone) return; // Внутренняя зона игнорирует E

        if (zoneType == WardrobeHideZoneType.DoorZone) wardrobe.InteractWithDoor(doorIndex); // Дверная зона открывает/закрывает соответствующую дверь
    }

    public void TryHide() // Нажатие Q
    {
        if (wardrobe == null) wardrobe = GetComponentInParent<WardrobeHideHandle>(); // Ищем шкаф выше

        if (wardrobe == null) return; // Если шкафа нет, выходим

        wardrobe.TryHide(); // Передаем попытку спрятаться шкафу
    }
}