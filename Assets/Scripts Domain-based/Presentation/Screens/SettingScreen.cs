using Core.Presentation;
using Infrastructure;
using UnityEngine;
using UnityEngine.UI;

namespace Presentation.Screens
{
    /// <summary>
    /// Отвечает за отображение пользовательского интерфейса экрана настроек игры
    /// </summary>
    /// <remarks>
    /// Является самодостаточным. Можно использовать другими классами.
    /// Зависит от <see cref="SettingService"/>
    /// </remarks>
    public class SettingScreen : ScreenBase
    {
        [Header("Bindings")]
        [SerializeField] private Slider sliderMaster;
        [SerializeField] private Slider sliderMusic;
        [SerializeField] private Slider sliderSFX;
        [SerializeField] private Slider sliderVoice;

        protected override void Awake()
        {
            base.Awake();

            sliderMaster?.onValueChanged.AddListener(OnMasterChanged);
            sliderMusic?.onValueChanged.AddListener(OnMusicChanged);
            sliderSFX?.onValueChanged.AddListener(OnSFXChanged);
            sliderVoice?.onValueChanged.AddListener(OnVoiceChanged);
        }

        private void OnEnable()
        {
            sliderMaster.value = SettingService.Instance.MasterVolume.Value;
            sliderMusic.value = SettingService.Instance.MusicVolume.Value;
            sliderSFX.value = SettingService.Instance.SFXVolume.Value;
            sliderVoice.value = SettingService.Instance.VoiceVolume.Value;
        }

        private void OnMasterChanged(float volume) => SettingService.Instance.MasterVolume.Value = volume;
        private void OnMusicChanged(float volume) => SettingService.Instance.MusicVolume.Value = volume;
        private void OnSFXChanged(float volume) => SettingService.Instance.SFXVolume.Value = volume;
        private void OnVoiceChanged(float volume) => SettingService.Instance.VoiceVolume.Value = volume;
    }
}
