using System.Collections; // Корутина ожидания загрузки банков
using UnityEngine; // Unity-классы
using FMODUnity; // RuntimeManager
using FMOD.Studio; // VCA

// Применяет сохранённые в PlayerPrefs громкости VCA при запуске игры — чтобы уровни были
// правильные с самого первого звука, даже если настройки ещё не открывали.
// Вешай на ВСЕГДА-АКТИВНЫЙ объект в стартовой сцене (меню). Ключи общие с SC_VCAVolumeSlider.
public class SC_AudioSettingsApplier : MonoBehaviour
{
    [Tooltip("Пути VCA, которые нужно восстановить при старте. Например: vca:/Master, vca:/Music, vca:/SFX")]
    public string[] vcaPaths = new[] { "vca:/Master", "vca:/Music", "vca:/SFX" }; // Список VCA

    [Range(0f, 1f)]
    public float defaultVolume = 1f; // Значение, если ещё ничего не сохранено

    private IEnumerator Start()
    {
        // Ждём, пока FMOD загрузит банки (иначе VCA ещё не существуют)
        while (!RuntimeManager.HaveAllBanksLoaded) yield return null;

        Apply(); // Применяем сохранённые уровни
    }

    // Можно дёрнуть вручную (например после сброса настроек).
    public void Apply()
    {
        if (vcaPaths == null) return; // Список пуст

        foreach (string path in vcaPaths) // По каждому VCA
        {
            if (string.IsNullOrEmpty(path)) continue; // Пустой путь пропускаем

            float v = PlayerPrefs.GetFloat(SC_VCAVolumeSlider.KeyFor(path), defaultVolume); // Сохранённая громкость

            try
            {
                VCA vca = RuntimeManager.GetVCA(path); // Берём VCA
                if (vca.isValid()) vca.setVolume(Mathf.Clamp01(v)); // Ставим громкость
            }
            catch { Debug.LogWarning($"SC_AudioSettingsApplier: VCA '{path}' не найден.", this); } // Не нашли
        }
    }
}
