using UnityEngine;

public class DyingLight : MonoBehaviour
{
    [SerializeField] private Light targetLight;

    [Header("Lifetime")]
    [SerializeField] private float fullLifeSeconds = 120f; // сколько "живет" до финала
    [SerializeField] private bool loop = false;            // для теста можно true

    [Header("When dead")]
    [SerializeField] private bool stayOffWhenDead = true;  // после смерти всегда 0
    [SerializeField] private float deadIntensity = 0f;

    [Header("Base Light")]
    [SerializeField] private float startIntensity = 3f;
    [SerializeField] private float endIntensity = 0.6f;

    [Header("Soft Flicker")]
    [SerializeField] private float startSoftAmp = 0.25f;
    [SerializeField] private float endSoftAmp = 0.9f;
    [SerializeField] private float startSoftSpeed = 6f;
    [SerializeField] private float endSoftSpeed = 12f;

    [Header("Blackouts")]
    [SerializeField] private float startBlackoutChancePerSecond = 0.02f;
    [SerializeField] private float endBlackoutChancePerSecond = 0.18f;
    [SerializeField] private Vector2 blackoutDuration = new Vector2(0.08f, 0.35f);

    [Header("Burst Flashes (series)")]
    [SerializeField] private float startBurstChancePerSecond = 0.05f;
    [SerializeField] private float endBurstChancePerSecond = 0.18f;
    [SerializeField] private int minBurstCount = 1;
    [SerializeField] private int maxBurstCount = 3;
    [SerializeField] private float burstFlashIntensity = 6f;
    [SerializeField] private float burstFlashDuration = 0.04f;
    [SerializeField] private float burstInterval = 0.06f;

    [Header("Last gasp (final spasm + fade-out)")]
    [Tooltip("Когда t >= lastGaspStart, лампа начинает истерить")]
    [SerializeField, Range(0f, 1f)] private float lastGaspStart = 0.85f;
    [Tooltip("С этого момента начинается плавное затухание в ноль")]
    [SerializeField, Range(0f, 1f)] private float fadeOutStart = 0.92f;
    [Tooltip("Насколько сильнее вспышки/шансы в last gasp")]
    [SerializeField] private float lastGaspMultiplier = 2.2f;
    [Tooltip("Добавка к яркости вспышки в last gasp")]
    [SerializeField] private float lastGaspFlashBoost = 2.0f;

    private float lifeTimer = 0f;

    // blackout state
    private float blackoutTimer = 0f;

    // burst state
    private int burstRemaining = 0;
    private float burstTimer = 0f;
    private bool isFlashFrame = false;

    // death latch
    private bool dead = false;

    void Awake()
    {
        if (targetLight == null)
            targetLight = GetComponent<Light>();
    }

    void Update()
    {
        if (targetLight == null) return;

        if (dead)
        {
            targetLight.intensity = deadIntensity;
            return;
        }

        // --- прогресс жизни 0..1 ---
        lifeTimer += Time.deltaTime;
        float t = (fullLifeSeconds <= 0.01f) ? 1f : Mathf.Clamp01(lifeTimer / fullLifeSeconds);

        if (loop && t >= 1f)
        {
            lifeTimer = 0f;
            t = 0f;
        }

        // --- если не loop и дошли до конца: можно "умереть" навсегда ---
        if (!loop && t >= 1f && stayOffWhenDead)
        {
            dead = true;
            targetLight.intensity = deadIntensity;
            return;
        }

        // --- интерполяция параметров ---
        float baseIntensity = Mathf.Lerp(startIntensity, endIntensity, t);
        float softAmp = Mathf.Lerp(startSoftAmp, endSoftAmp, t);
        float softSpeed = Mathf.Lerp(startSoftSpeed, endSoftSpeed, t);

        float blackoutChance = Mathf.Lerp(startBlackoutChancePerSecond, endBlackoutChancePerSecond, t);
        float burstChance = Mathf.Lerp(startBurstChancePerSecond, endBurstChancePerSecond, t);

        // --- last gasp: усиливаем хаос ближе к смерти ---
        float gasp01 = 0f;
        if (t >= lastGaspStart)
            gasp01 = Mathf.InverseLerp(lastGaspStart, 1f, t); // 0..1

        float chaosMul = Mathf.Lerp(1f, lastGaspMultiplier, gasp01);
        blackoutChance *= chaosMul;
        burstChance *= chaosMul;
        softAmp *= Mathf.Lerp(1f, 1.35f, gasp01);
        softSpeed *= Mathf.Lerp(1f, 1.25f, gasp01);

        float flashIntensityNow = burstFlashIntensity + Mathf.Lerp(0f, lastGaspFlashBoost, gasp01);

        // --- fade-out: после точки fadeOutStart яркость гасится в ноль ---
        float fade01 = 0f;
        if (t >= fadeOutStart)
            fade01 = Mathf.InverseLerp(fadeOutStart, 1f, t); // 0..1

        // 1 -> 0
        float fadeMul = 1f - fade01;

        // --- blackout ---
        if (blackoutTimer > 0f)
        {
            blackoutTimer -= Time.deltaTime;
            targetLight.intensity = 0f;
            return;
        }

        // --- мягкое "дыхание" ---
        float soft = (Mathf.PerlinNoise(Time.time * softSpeed, 0f) - 0.5f) * 2f; // -1..1
        float current = baseIntensity + soft * softAmp;
        current = Mathf.Max(0f, current);

        // применяем fadeMul к обычному свету
        current *= fadeMul;

        // --- серия вспышек ---
        if (burstRemaining > 0)
        {
            burstTimer -= Time.deltaTime;

            if (burstTimer <= 0f)
            {
                isFlashFrame = !isFlashFrame;

                if (isFlashFrame)
                {
                    // вспышка тоже затухает ближе к финалу
                    targetLight.intensity = flashIntensityNow * fadeMul;
                }
                else
                {
                    targetLight.intensity = current;
                    burstRemaining--;
                }

                burstTimer = isFlashFrame ? burstFlashDuration : burstInterval;
            }

            return;
        }

        // --- случайный blackout (в конце чаще) ---
        if (Random.value < blackoutChance * Time.deltaTime)
        {
            // в last gasp blackout может быть дольше
            float durMul = Mathf.Lerp(1f, 1.6f, gasp01);
            blackoutTimer = Random.Range(blackoutDuration.x, blackoutDuration.y) * durMul;
            targetLight.intensity = 0f;
            return;
        }

        // --- случайный запуск серии вспышек (в конце чаще и сериями) ---
        if (Random.value < burstChance * Time.deltaTime)
        {
            int extra = Mathf.RoundToInt(Mathf.Lerp(0f, 3f, gasp01));
            burstRemaining = Random.Range(minBurstCount, maxBurstCount + 1) + extra;
            burstTimer = 0f;
        }

        targetLight.intensity = current;

        // --- финальный "щёлк" в ноль, если дошли до 1 и не loop ---
        if (!loop && t >= 1f && stayOffWhenDead)
        {
            dead = true;
            targetLight.intensity = deadIntensity;
        }
    }
}