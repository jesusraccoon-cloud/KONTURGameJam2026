using UnityEngine; // Подключаем Unity-классы.

public class UniversalDoorInteractForwarder : MonoBehaviour, IInteractable
{
    public string hint = "Взаимодействовать";

    public string Hint => hint;

    public UniversalDoor door;
    public void Interact()
    {
        if (door == null) door = GetComponentInParent<UniversalDoor>();
        if (door == null) return;
        door.Interact();
    }
}
