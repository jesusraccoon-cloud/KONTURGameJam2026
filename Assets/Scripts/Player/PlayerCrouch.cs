using UnityEngine;

public class PlayerCrouch : MonoBehaviour
{
    [Header("References")]
    public CharacterController controller; // CharacterController игрока
    public Transform cameraRoot; // PlayerCameraRoot

    [Header("Crouch Settings")]
    public KeyCode crouchKey = KeyCode.LeftControl; // Кнопка приседа
    public float crouchHeight = 1.0f; // Высота капсулы в приседе
    public float crouchCameraOffset = 0.45f; // Насколько опускать камеру вниз
    public float crouchSpeed = 10f; // Скорость перехода

    private float standingHeight; // Высота стоя
    private Vector3 standingCenter; // Центр CharacterController стоя
    private Vector3 crouchingCenter; // Центр CharacterController в приседе

    private Vector3 standingCameraLocalPos; // Позиция камеры стоя
    private Vector3 crouchingCameraLocalPos; // Позиция камеры в приседе

    private void Start()
    {
        if (controller == null)
        {
            controller = GetComponent<CharacterController>(); // Берем CharacterController с этого же объекта
        }

        if (controller == null) // Если CharacterController не найден
        {
            enabled = false; // Отключаем скрипт, чтобы не было NullReference
            return; // Выходим из Start
        }

        if (cameraRoot == null)
        {
            Debug.LogError("PlayerCrouch: не назначен cameraRoot."); // Показываем ошибку
            enabled = false; // Отключаем скрипт, чтобы он не работал без камеры
            return; // Выходим из Start
        }

        standingHeight = controller.height; // Запоминаем текущую высоту как высоту стоя
        standingCenter = controller.center; // Запоминаем текущий центр как центр стоя

        crouchingCenter = new Vector3(
            standingCenter.x,
            crouchHeight * 0.5f,
            standingCenter.z
        ); // Считаем центр капсулы в приседе

        standingCameraLocalPos = cameraRoot.localPosition; // Запоминаем позицию камеры стоя
        crouchingCameraLocalPos = standingCameraLocalPos - new Vector3(0f, crouchCameraOffset, 0f); // Считаем позицию камеры в приседе
    }

    private void Update()
    {
        bool wantsToCrouch = Input.GetKey(crouchKey); // Пока держим Ctrl — сидим

        float targetHeight = wantsToCrouch ? crouchHeight : standingHeight; // Выбираем целевую высоту
        Vector3 targetCenter = wantsToCrouch ? crouchingCenter : standingCenter; // Выбираем целевой центр
        Vector3 targetCameraPos = wantsToCrouch ? crouchingCameraLocalPos : standingCameraLocalPos; // Выбираем позицию камеры

        controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchSpeed); // Плавно меняем высоту
        controller.center = Vector3.Lerp(controller.center, targetCenter, Time.deltaTime * crouchSpeed); // Плавно меняем центр
        cameraRoot.localPosition = Vector3.Lerp(cameraRoot.localPosition, targetCameraPos, Time.deltaTime * crouchSpeed); // Плавно двигаем камеру
    }
}