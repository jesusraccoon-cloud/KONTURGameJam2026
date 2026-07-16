using UnityEngine; // Подключаем Unity-классы

// Любой звук, который хочет окклюзию, реализует этот интерфейс.
// Центральный SC_OcclusionListener на игроке сам находит такие источники,
// пускает к ним лучи и раздаёт значение окклюзии — независимо от того,
// как именно звук проигрывается (StudioEventEmitter, свой EventInstance и т.д.).
public interface IOccludable
{
    Vector3 OcclusionPoint { get; } // Откуда исходит звук (точка, к которой пускаем лучи)

    bool WantsOcclusion { get; } // Играет ли звук сейчас и есть ли смысл его окклюдить

    void ApplyOcclusion(float occlusion01); // Применить окклюзию 0..1 (записать в FMOD-параметр)
}
