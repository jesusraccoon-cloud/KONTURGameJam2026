using UnityEngine; // Подключаем Unity-классы
#if ENABLE_INPUT_SYSTEM // Проверяем, включена ли новая Input System
using UnityEngine.InputSystem; // Подключаем PlayerInput из новой Input System
#endif // Конец проверки Input System

namespace StarterAssets // Пространство имён StarterAssets
{
    [RequireComponent(typeof(CharacterController))] // Требуем CharacterController на объекте игрока
#if ENABLE_INPUT_SYSTEM // Если включена новая Input System
    [RequireComponent(typeof(PlayerInput))] // Требуем PlayerInput на объекте игрока
#endif // Конец проверки Input System
    public class FirstPersonController : MonoBehaviour // Основной контроллер игрока от первого лица
    {
        [Header("Player")] // Блок настроек игрока
        public float MoveSpeed = 4.0f; // Скорость ходьбы
        public float SprintSpeed = 6.0f; // Скорость бега
        public float RotationSpeed = 1.0f; // Скорость поворота камеры
        public float SpeedChangeRate = 10.0f; // Скорость разгона и торможения

        [Space(10)] // Отступ в Inspector
        public float JumpHeight = 1.2f; // Высота прыжка
        public float Gravity = -15.0f; // Сила гравитации

        [Space(10)] // Отступ в Inspector
        public float JumpTimeout = 0.1f; // Задержка перед следующим прыжком
        public float FallTimeout = 0.15f; // Задержка перед падением

        [Header("Player Grounded")] // Блок проверки земли
        public bool Grounded = true; // Стоит ли игрок на земле
        public float GroundedOffset = -0.14f; // Смещение сферы проверки земли
        public float GroundedRadius = 0.5f; // Радиус проверки земли
        public LayerMask GroundLayers; // Слои, которые считаются землей

        [Header("Cinemachine")] // Блок камеры
        public GameObject CinemachineCameraTarget; // Объект, за которым следует камера
        public float TopClamp = 90.0f; // Максимальный угол взгляда вверх
        public float BottomClamp = -90.0f; // Максимальный угол взгляда вниз

        [Header("QTE Lock")] // Блок блокировки управления
        public bool canMove = true; // Можно ли игроку двигаться
        public bool canLook = true; // Можно ли игроку крутить камерой

        private float _cinemachineTargetPitch; // Текущий вертикальный угол камеры
        private float _speed; // Текущая скорость игрока
        private float _rotationVelocity; // Скорость поворота игрока
        private float _verticalVelocity; // Вертикальная скорость игрока
        private float _terminalVelocity = 53.0f; // Максимальная скорость падения

        private float _jumpTimeoutDelta; // Текущий таймер задержки прыжка
        private float _fallTimeoutDelta; // Текущий таймер задержки падения

#if ENABLE_INPUT_SYSTEM // Если включена новая Input System
        private PlayerInput _playerInput; // Ссылка на PlayerInput
#endif // Конец проверки Input System

        private CharacterController _controller; // Ссылка на CharacterController
        private StarterAssetsInputs _input; // Ссылка на ввод StarterAssets
        private GameObject _mainCamera; // Ссылка на главную камеру

        private const float _threshold = 0.01f; // Минимальный порог движения мыши/стика

        private bool IsCurrentDeviceMouse // Проверка, используется ли мышь
        {
            get // Получение значения
            {
#if ENABLE_INPUT_SYSTEM // Если включена новая Input System
                return _playerInput.currentControlScheme == "KeyboardMouse"; // true, если используется мышь и клавиатура
#else // Если новая Input System не включена
                return false; // Возвращаем false
#endif // Конец проверки Input System
            }
        }

        private void Awake() // Вызывается при создании объекта
        {
            if (_mainCamera == null) // Если главная камера ещё не найдена
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera"); // Ищем объект с тегом MainCamera
            }
        }

        private void Start() // Вызывается перед первым кадром
        {
            _controller = GetComponent<CharacterController>(); // Получаем CharacterController
            _input = GetComponent<StarterAssetsInputs>(); // Получаем StarterAssetsInputs

#if ENABLE_INPUT_SYSTEM // Если включена новая Input System
            _playerInput = GetComponent<PlayerInput>(); // Получаем PlayerInput
#else // Если Input System отсутствует
            Debug.LogError("Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it"); // Пишем ошибку
#endif // Конец проверки Input System

            _jumpTimeoutDelta = JumpTimeout; // Инициализируем таймер прыжка
            _fallTimeoutDelta = FallTimeout; // Инициализируем таймер падения
        }

        private void Update() // Вызывается каждый кадр
        {
            JumpAndGravity(); // Обрабатываем прыжок и гравитацию
            GroundedCheck(); // Проверяем землю

            if (canMove) // Если движение разрешено
            {
                Move(); // Двигаем игрока
            }
        }

        private void LateUpdate() // Вызывается после Update
        {
            if (canLook) // Если обзор разрешён
            {
                CameraRotation(); // Крутим камеру
            }
        }

        private void GroundedCheck() // Проверка земли
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z); // Создаём позицию сферы проверки

            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore); // Проверяем касание земли
        }

        private void CameraRotation() // Поворот камеры
        {
            if (_input.look.sqrMagnitude >= _threshold) // Если есть ввод поворота
            {
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime; // Для мыши не умножаем на deltaTime

                _cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier; // Меняем вертикальный угол камеры
                _rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier; // Считаем горизонтальный поворот

                _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp); // Ограничиваем вертикальный угол

                CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f); // Поворачиваем цель камеры

                transform.Rotate(Vector3.up * _rotationVelocity); // Поворачиваем игрока по горизонтали
            }
        }

        private void Move() // Метод движения игрока
        {
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed; // Выбираем скорость: бег или ходьба

            if (_input.move == Vector2.zero) targetSpeed = 0.0f; // Если ввода движения нет — скорость 0

            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude; // Считаем горизонтальную скорость

            float speedOffset = 0.1f; // Небольшой допуск скорости
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f; // Сила ввода

            if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset) // Если скорость отличается
            {
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate); // Плавно меняем скорость
                _speed = Mathf.Round(_speed * 1000f) / 1000f; // Округляем скорость
            }
            else // Если скорость уже близка
            {
                _speed = targetSpeed; // Ставим целевую скорость
            }

            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized; // Создаём направление ввода

            if (_input.move != Vector2.zero) // Если игрок нажимает движение
            {
                inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y; // Переводим направление в локальные оси игрока
            }

            _controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime); // Двигаем CharacterController
        }

        private void JumpAndGravity() // Прыжок и гравитация
        {
            if (Grounded) // Если игрок на земле
            {
                _fallTimeoutDelta = FallTimeout; // Сбрасываем таймер падения

                if (_verticalVelocity < 0.0f) // Если вертикальная скорость вниз
                {
                    _verticalVelocity = -2f; // Прижимаем игрока к земле
                }

                if (_input.jump && _jumpTimeoutDelta <= 0.0f && canMove) // Если нажали прыжок и можно прыгать
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity); // Задаём вертикальную скорость прыжка
                }

                if (_jumpTimeoutDelta >= 0.0f) // Если таймер прыжка ещё идёт
                {
                    _jumpTimeoutDelta -= Time.deltaTime; // Уменьшаем таймер прыжка
                }
            }
            else // Если игрок не на земле
            {
                _jumpTimeoutDelta = JumpTimeout; // Сбрасываем таймер прыжка

                if (_fallTimeoutDelta >= 0.0f) // Если таймер падения ещё идёт
                {
                    _fallTimeoutDelta -= Time.deltaTime; // Уменьшаем таймер падения
                }

                _input.jump = false; // Сбрасываем ввод прыжка
            }

            if (_verticalVelocity < _terminalVelocity) // Если вертикальная скорость меньше максимальной
            {
                _verticalVelocity += Gravity * Time.deltaTime; // Добавляем гравитацию
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax) // Метод ограничения угла
        {
            if (lfAngle < -360f) lfAngle += 360f; // Нормализуем слишком маленький угол
            if (lfAngle > 360f) lfAngle -= 360f; // Нормализуем слишком большой угол

            return Mathf.Clamp(lfAngle, lfMin, lfMax); // Ограничиваем угол
        }

        private void OnDrawGizmosSelected() // Рисуется в Scene, когда объект выбран
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f); // Полупрозрачный зелёный
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f); // Полупрозрачный красный

            if (Grounded) Gizmos.color = transparentGreen; // Если на земле — зелёный
            else Gizmos.color = transparentRed; // Если не на земле — красный

            Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius); // Рисуем сферу проверки земли
        }
    }
}