using UnityEngine; // Подключаем Unity-классы

public class HammerBathroomLock : MonoBehaviour, IHitInteractable // Замок ванной, который выбивается ударом
{
    [Header("Door")] // Блок двери
    public UniversalDoor bathroomDoor; // Дверь ванной, которую надо разблокировать

    public void Hit() // Метод вызывается PlayerInteractor при ЛКМ
    {
        if (bathroomDoor != null) // Если дверь ванной назначена
        {
            bathroomDoor.isLocked = false; // Разблокируем дверь ванной
            bathroomDoor.canMonsterOpen = false; // Монстру всё равно запрещаем её открывать
        }

        gameObject.SetActive(false); // Выключаем коллайдер замка после удара

        Debug.Log("Замок ванной выбит"); // Сообщение в Console
    }
}