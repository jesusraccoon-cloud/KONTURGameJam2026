using UnityEngine; // Подключаем Unity-классы
using FMODUnity; // Подключаем FMOD (StudioEventEmitter)

// Делает обычный StudioEventEmitter окклюдируемым через центральный SC_OcclusionListener.
// Вешается рядом с эмиттером (радио, эмбиенс и т.п.).
[RequireComponent(typeof(StudioEventEmitter))]
public class SC_OccludableEmitter : MonoBehaviour, IOccludable
{
    [Header("References")] // Блок ссылок
    public StudioEventEmitter emitter; // Эмиттер, который окклюдим

    [Header("FMOD Parameter")] // Блок параметра
    public string occlusionParameter = "Occlusion"; // Непрерывный параметр 0..1 в событии

    public Vector3 OcclusionPoint => transform.position; // Звук исходит из позиции эмиттера

    public bool WantsOcclusion => emitter != null && emitter.IsPlaying(); // Окклюдим, только пока играет

    public void ApplyOcclusion(float occlusion01) // Применить окклюзию
    {
        if (emitter != null) emitter.SetParameter(occlusionParameter, occlusion01); // Пишем в параметр эмиттера
    }

    private void Awake() // При создании
    {
        if (emitter == null) emitter = GetComponent<StudioEventEmitter>(); // Берём с этого же объекта
    }

    private void OnEnable() // При включении
    {
        SC_OcclusionListener.Register(this); // Регистрируемся у слушателя
    }

    private void OnDisable() // При выключении
    {
        SC_OcclusionListener.Unregister(this); // Отписываемся
    }
}
