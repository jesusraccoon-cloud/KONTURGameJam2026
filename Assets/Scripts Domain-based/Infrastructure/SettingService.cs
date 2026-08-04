using System.Collections;
using Core;
using FMOD.Studio;
using FMODUnity;
using R3;
using UnityEngine;

namespace Infrastructure
{
    /// <summary>
    /// Сервис для настройки игры с возможностью чтения настроек
    /// </summary>
    public class SettingService : ServiceBase<SettingService>
    {
        [Header("Audio Settings")]
        [SerializeField] private ReactiveProperty<float> volumeMaster = new(1f);
        [SerializeField] private ReactiveProperty<float> volumeMusic = new(1f);
        [SerializeField] private ReactiveProperty<float> volumeSFX = new(1f);
        [SerializeField] private ReactiveProperty<float> volumeUI = new(1f);
        [SerializeField] private ReactiveProperty<float> volumeVoice = new(1f);

        private VCA vcaMaster;
        private VCA vcaMusic;
        private VCA vcaSFX;
        private VCA vcaUI;
        private VCA vcaVoice;

        public ReactiveProperty<float> MasterVolume => volumeMaster;
        public ReactiveProperty<float> MusicVolume => volumeMusic;
        public ReactiveProperty<float> SFXVolume => volumeSFX;
        public ReactiveProperty<float> UIVolume => volumeUI;
        public ReactiveProperty<float> VoiceVolume => volumeVoice;


        #region [Методы] Инициализация и запуск

        public override void Startup()
        {
            MasterVolume
                .Subscribe(x => VolumeUpdatedHandler(vcaMaster, "MasterVolume", x))
                .AddTo(this);

            MusicVolume
                .Subscribe(x => VolumeUpdatedHandler(vcaMusic, "MusicVolume", x))
                .AddTo(this);

            SFXVolume
                .Subscribe(x => VolumeUpdatedHandler(vcaSFX, "SFXVolume", x))
                .AddTo(this);

            UIVolume
                .Subscribe(x => VolumeUpdatedHandler(vcaUI, "UIVolume", x))
                .AddTo(this);

            VoiceVolume
                .Subscribe(x => VolumeUpdatedHandler(vcaVoice, "VoiceVolume", x))
                .AddTo(this);

            this.status.Value = LifecycleState.Started;
        }

        #endregion


        private IEnumerator Start()
        {
            // Ждём загрузки банков FMOD — иначе VCA ещё не существуют и setVolume не применится
            while (!RuntimeManager.HaveAllBanksLoaded) yield return null;

            vcaMaster = RuntimeManager.GetVCA("vca:/Master");
            vcaMusic = RuntimeManager.GetVCA("vca:/Music");
            vcaSFX = RuntimeManager.GetVCA("vca:/SFX");
            vcaUI = RuntimeManager.GetVCA("vca:/UI");
            vcaVoice = RuntimeManager.GetVCA("vca:/Voice");

<<<<<<< HEAD
=======
            // OnNext прогоняет сохранённые значения через подписку -> применяет к VCA (теперь валидным) и на старте
>>>>>>> c7e953588fc564e6b31476eb5fbcf2077dad70de
            volumeMaster.OnNext(PlayerPrefs.GetFloat("MasterVolume", volumeMaster.Value));
            volumeMusic.OnNext(PlayerPrefs.GetFloat("MusicVolume", volumeMusic.Value));
            volumeSFX.OnNext(PlayerPrefs.GetFloat("SFXVolume", volumeSFX.Value));
            volumeUI.OnNext(PlayerPrefs.GetFloat("UIVolume", volumeUI.Value));
            volumeVoice.OnNext(PlayerPrefs.GetFloat("VoiceVolume", volumeVoice.Value));

            print("Setting Service is Started");
        }

        private void VolumeUpdatedHandler(VCA vca, string prefsParameter, float sliderValue)
        {
            if (vca.isValid())
            {
                float dB = SliderToDB(sliderValue);
                float amplitude = Mathf.Pow(10f, dB / 20f);
                vca.setVolume(amplitude);
            }

            if (Status.CurrentValue == LifecycleState.Started)
                PlayerPrefs.SetFloat(prefsParameter, sliderValue);
        }

        private float SliderToDB(float volume) =>
            Mathf.Approximately(volume, 0f) ? -80f : 20f * Mathf.Log10(volume);

        private void SetMasterBusVolume(float sliderValue)
        {
            Bus masterBus = RuntimeManager.GetBus("bus:/");
            if (masterBus.isValid())
            {
                float dB = SliderToDB(sliderValue);
                masterBus.setVolume(dB);
            }
        }
    }
}
