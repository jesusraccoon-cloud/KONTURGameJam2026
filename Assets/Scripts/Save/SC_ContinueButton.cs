using UnityEngine; // Unity-классы
using UnityEngine.UI; // Button

// Вешается на кнопку «Продолжить / Загрузить» в меню.
// Делает кнопку неактивной (и, опционально, прячет её), если сохранения на диске нет.
// Нажатие кнопки настраивается отдельно: OnClick -> SC_SaveManager.ContinueFromDisk().
[RequireComponent(typeof(Button))]
public class SC_ContinueButton : MonoBehaviour
{
    [Tooltip("Кнопка Continue (если пусто — берётся с этого объекта).")]
    [SerializeField] private Button button; // Сама кнопка

    [Tooltip("Необязательно: объект, который нужно СКРЫТЬ, когда сейва нет (например саму кнопку или её строку).")]
    [SerializeField] private GameObject hideWhenNoSave; // Что прятать при отсутствии сейва

    private void Reset() // При добавлении компонента в редакторе
    {
        button = GetComponent<Button>(); // Подставляем кнопку с этого объекта
    }

    private void OnEnable() // Каждый раз, когда меню показывается
    {
        Refresh(); // Обновляем состояние кнопки
    }

    // Можно вызвать вручную (например после удаления сейва), чтобы обновить кнопку.
    public void Refresh()
    {
        if (button == null) button = GetComponent<Button>(); // Гарантируем ссылку

        bool hasSave = SC_SaveSystem.HasDiskSave; // Есть ли файл сейва на диске

        if (button != null) button.interactable = hasSave; // Активна только при наличии сейва

        if (hideWhenNoSave != null) hideWhenNoSave.SetActive(hasSave); // Прячем объект, если сейва нет
    }
}
