using UnityEngine; // Unity-классы
using UnityEngine.UI; // Slider
using FMODUnity; // RuntimeManager
using FMOD.Studio; // VCA

// Вешается на UI-Slider в настройках. Крутит громкость FMOD-VCA (например vca:/Music),
// сохраняет значение в PlayerPrefs и восстанавливает при открытии настроек.
// Диапазон слайдера: Min 0, Max 1.
[RequireComponent(typeof(Slider))]
public class SC_VCAVolumeSlider : MonoBehaviour
{
    [Header("VCA")]
    [Tooltip("Путь к VCA в FMOD. Например: vca:/Master, vca:/Music, vca:/SFX")]
    public string vcaPath = "vca:/Master"; // Какой VCA крутим

    [Range(0f, 1f)]
    public float defaultVolume = 1f; // Значение по умолчанию (если ещё не сохраняли)

    [SerializeField] private Slider slider; // Слайдер (авто с этого объекта)

    // Ключ сохранения — единый с SC_AudioSettingsApplier, чтобы громкость применялась и на старте.
    public static string KeyFor(string path) => "vca_vol_" + path;

    private void Reset() // При добавлении компонента
    {
        slider = GetComponent<Slider>(); // Подставляем слайдер
        if (slider != null) { slider.minValue = 0f; slider.maxValue = 1f; } // Диапазон 0..1
    }

    private void Awake()
    {
        if (slider == null) slider = GetComponent<Slider>(); // Гарантируем ссылку
    }

    private void OnEnable() // Когда настройки показали
    {
        float v = PlayerPrefs.GetFloat(KeyFor(vcaPath), defaultVolume); // Загружаем сохранённое

        ApplyToVCA(v); // Применяем к VCA

        if (slider != null)
        {
            slider.SetValueWithoutNotify(v); // Ставим ползунок без вызова колбэка
            slider.onValueChanged.AddListener(OnSliderChanged); // Подписываемся на изменение
        }
    }

    private void OnDisable() // Когда настройки скрыли
    {
        if (slider != null) slider.onValueChanged.RemoveListener(OnSliderChanged); // Отписываемся
    }

    private void OnSliderChanged(float v) // Игрок двигает ползунок
    {
        ApplyToVCA(v); // Меняем громкость VCA
        PlayerPrefs.SetFloat(KeyFor(vcaPath), v); // Сохраняем
    }

    private void ApplyToVCA(float v) // Применить громкость к VCA
    {
        if (string.IsNullOrEmpty(vcaPath)) return; // Путь не задан

        try
        {
            VCA vca = RuntimeManager.GetVCA(vcaPath); // Берём VCA
            if (vca.isValid()) vca.setVolume(Mathf.Clamp01(v)); // Ставим громкость (0..1, линейно)
        }
        catch { Debug.LogWarning($"SC_VCAVolumeSlider: VCA '{vcaPath}' не найден. Проверь путь.", this); } // Не нашли
    }
}
