using UnityEngine; // Подключаем Unity
using TMPro; // Подключаем TextMeshPro
using StarterAssets; // Подключаем Starter Assets

public class FinalQTE : MonoBehaviour // Скрипт QTE, где монстр хватает игрока за ногу
{
    [Header("Player")] // Блок игрока
    public Transform player; // Трансформ игрока
    public CharacterController characterController; // CharacterController игрока
    public FirstPersonController playerController; // Контроллер игрока

    [Header("Monster")] // Блок монстра
    public GameObject monsterObject; // Объект монстра
    public Transform monsterGrabPoint; // Точка, куда поставить монстра во время QTE

    [Header("QTE Settings")] // Настройки QTE
    public KeyCode qteKey = KeyCode.E; // Кнопка QTE
    public int requiredPresses = 10; // Сколько раз нажать E
    public float qteTime = 4f; // Сколько секунд даётся на QTE

    [Header("UI")] // Блок интерфейса
    public GameObject qtePanel; // Панель QTE
    public TextMeshProUGUI qteText; // Текст QTE

    [Header("Camera Shake")] // Блок тряски камеры
    public Transform cameraShakeTarget; // Обычно CinemachineCameraTarget
    public float shakePower = 0.04f; // Сила тряски
    public float shakeSpeed = 35f; // Скорость тряски

    private int currentPresses = 0; // Текущее количество нажатий
    private float timer = 0f; // Таймер QTE
    private bool qteActive = false; // Активно ли QTE сейчас
    private Vector3 cameraStartLocalPosition; // Стартовая локальная позиция камеры

    public void StartQTE() // Запуск QTE
    {
        if (qteActive) return; // Если QTE уже идёт — выходим

        qteActive = true; // Включаем QTE
        currentPresses = 0; // Сбрасываем нажатия
        timer = qteTime; // Ставим таймер

        if (playerController != null) // Если контроллер игрока назначен
        {
            playerController.canMove = false; // Запрещаем ходить
            playerController.canLook = true; // Разрешаем крутить камерой
        }

        if (cameraShakeTarget != null) // Если цель тряски назначена
        {
            cameraStartLocalPosition = cameraShakeTarget.localPosition; // Запоминаем стартовую позицию
        }

        if (monsterObject != null) monsterObject.SetActive(true); // Включаем монстра

        if (monsterObject != null && monsterGrabPoint != null) // Если монстр и точка назначены
        {
            monsterObject.transform.position = monsterGrabPoint.position; // Ставим монстра в точку хватания
            monsterObject.transform.rotation = monsterGrabPoint.rotation; // Поворачиваем монстра как точку
        }

        if (qtePanel != null) qtePanel.SetActive(true); // Включаем UI QTE

        UpdateText(); // Обновляем текст
    }

    private void Update() // Каждый кадр
    {
        if (!qteActive) return; // Если QTE не активно — выходим

        timer -= Time.deltaTime; // Уменьшаем таймер

        ShakeCamera(); // Трясём камеру во время QTE

        if (Input.GetKeyDown(qteKey)) // Если нажали кнопку QTE
        {
            currentPresses++; // Добавляем одно нажатие
            UpdateText(); // Обновляем текст

            if (currentPresses >= requiredPresses) // Если нажатий достаточно
            {
                CompleteQTE(); // Завершаем QTE успешно
            }
        }

        if (timer <= 0f) // Если время вышло
        {
            FailQTE(); // Проваливаем QTE
        }
    }

    private void ShakeCamera() // Метод тряски камеры
    {
        if (cameraShakeTarget == null) return; // Если цель не назначена — выходим

        float x = Mathf.Sin(Time.time * shakeSpeed) * shakePower; // Считаем дрожание по X
        float y = Mathf.Cos(Time.time * shakeSpeed * 1.3f) * shakePower; // Считаем дрожание по Y

        cameraShakeTarget.localPosition = cameraStartLocalPosition + new Vector3(x, y, 0f); // Двигаем цель камеры
    }

    private void StopCameraShake() // Метод остановки тряски
    {
        if (cameraShakeTarget == null) return; // Если цель не назначена — выходим

        cameraShakeTarget.localPosition = cameraStartLocalPosition; // Возвращаем камеру в стартовую позицию
    }

    private void UpdateText() // Обновление текста QTE
    {
        if (qteText == null) return; // Если текста нет — выходим

        qteText.text = "ЖМИ E\n" + currentPresses + "/" + requiredPresses; // Показываем прогресс
    }

    private void CompleteQTE() // Успешное завершение QTE
    {
        qteActive = false; // Выключаем QTE

        StopCameraShake(); // Останавливаем тряску камеры

        if (qtePanel != null) qtePanel.SetActive(false); // Прячем UI QTE

        if (playerController != null) // Если контроллер игрока назначен
        {
            playerController.canMove = true; // Возвращаем ходьбу
            playerController.canLook = true; // Оставляем обзор включённым
        }

        Debug.Log("QTE пройдено: игрок вырвался"); // Сообщение в Console
    }

    private void FailQTE() // Провал QTE
    {
        qteActive = false; // Выключаем QTE

        StopCameraShake(); // Останавливаем тряску камеры

        if (qtePanel != null) qtePanel.SetActive(false); // Прячем UI QTE

        if (playerController != null) // Если контроллер игрока назначен
        {
            playerController.canMove = false; // После провала не даём ходить
            playerController.canLook = true; // Но можно смотреть
        }

        Debug.Log("QTE провалено: игрок пойман"); // Сообщение в Console
    }
}