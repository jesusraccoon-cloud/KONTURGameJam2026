using UnityEngine; // Подключаем Unity-классы

public class ObjectGrabber : MonoBehaviour // Скрипт захвата и движения предметов
{
    public Camera playerCamera; // Камера игрока

    public Transform holdPoint; // Точка удержания предмета перед камерой

    public float grabDistance = 3f; // Дистанция захвата

    public float moveSpeed = 10f; // Скорость движения лёгкого предмета

    public float heavyMoveSpeed = 4f; // Скорость движения тяжёлого предмета

    public LayerMask grabbableLayer; // Слой лёгких предметов

    public LayerMask movableHeavyLayer; // Слой тяжёлых предметов

    private Rigidbody grabbedRigidbody; // Текущий лёгкий предмет

    private Rigidbody movingHeavyRigidbody; // Текущий тяжёлый предмет

    private bool oldUseGravity; // Старое состояние гравитации лёгкого предмета

    private RigidbodyConstraints oldConstraints; // Старые ограничения лёгкого предмета

    private RigidbodyConstraints oldHeavyConstraints; // Старые ограничения тяжёлого предмета

    private void FixedUpdate() // Физическое обновление
    {
        if (grabbedRigidbody != null) // Если держим лёгкий предмет
        {
            HoldLightObject(); // Удерживаем предмет
        }

        if (movingHeavyRigidbody != null) // Если двигаем тяжёлый предмет
        {
            MoveHeavyObject(); // Двигаем предмет
        }
    }

    public void Interact() // Вызывается PlayerInteractor при коротком E
    {
        if (grabbedRigidbody == null && movingHeavyRigidbody == null) // Если ничего не держим
        {
            TryInteract(); // Пытаемся взять или начать двигать объект
        }
        else // Если уже держим или двигаем
        {
            ReleaseLightObject(); // Отпускаем лёгкий объект

            StopMovingHeavyObject(); // Останавливаем тяжёлый объект
        }
    }

    private void TryInteract() // Проверяет объект перед камерой
    {
        if (playerCamera == null) return; // Если камера не назначена — выходим

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward); // Луч из камеры

        int interactMask = grabbableLayer.value | movableHeavyLayer.value; // Объединяем слои

        if (Physics.Raycast(ray, out RaycastHit hit, grabDistance, interactMask)) // Пускаем луч
        {
            if (IsInLayerMask(hit.collider.gameObject.layer, movableHeavyLayer)) // Если объект тяжёлый
            {
                TryStartMovingHeavyObject(hit); // Начинаем двигать тяжёлый объект

                return; // Выходим
            }

            if (IsInLayerMask(hit.collider.gameObject.layer, grabbableLayer)) // Если объект лёгкий
            {
                TryGrabLightObject(hit); // Берём лёгкий объект

                return; // Выходим
            }
        }
    }

    private void TryGrabLightObject(RaycastHit hit) // Пытается взять лёгкий объект
    {
        Rigidbody rb = hit.collider.attachedRigidbody; // Получаем Rigidbody

        if (rb == null) return; // Если Rigidbody нет — выходим

        grabbedRigidbody = rb; // Запоминаем предмет

        oldUseGravity = grabbedRigidbody.useGravity; // Запоминаем гравитацию

        oldConstraints = grabbedRigidbody.constraints; // Запоминаем ограничения

        grabbedRigidbody.useGravity = false; // Отключаем гравитацию

        grabbedRigidbody.velocity = Vector3.zero; // Обнуляем скорость

        grabbedRigidbody.angularVelocity = Vector3.zero; // Обнуляем вращение

        grabbedRigidbody.constraints = RigidbodyConstraints.FreezeRotation; // Замораживаем вращение
    }

    private void HoldLightObject() // Удерживает лёгкий предмет
    {
        if (holdPoint == null) return; // Если точки удержания нет — выходим

        Vector3 newPosition = Vector3.Lerp(
            grabbedRigidbody.position,
            holdPoint.position,
            moveSpeed * Time.fixedDeltaTime
        ); // Считаем плавную позицию

        grabbedRigidbody.MovePosition(newPosition); // Двигаем предмет через физику
    }

    private void ReleaseLightObject() // Отпускает лёгкий предмет
    {
        if (grabbedRigidbody == null) return; // Если предмета нет — выходим

        grabbedRigidbody.useGravity = oldUseGravity; // Возвращаем гравитацию

        grabbedRigidbody.constraints = oldConstraints; // Возвращаем ограничения

        grabbedRigidbody.velocity = Vector3.zero; // Обнуляем скорость

        grabbedRigidbody.angularVelocity = Vector3.zero; // Обнуляем вращение

        grabbedRigidbody = null; // Очищаем ссылку
    }

    private void TryStartMovingHeavyObject(RaycastHit hit) // Начинает двигать тяжёлый объект
    {
        Rigidbody rb = hit.collider.attachedRigidbody; // Получаем Rigidbody

        if (rb == null) return; // Если Rigidbody нет — выходим

        movingHeavyRigidbody = rb; // Запоминаем тяжёлый объект

        oldHeavyConstraints = movingHeavyRigidbody.constraints; // Запоминаем ограничения

        movingHeavyRigidbody.velocity = Vector3.zero; // Обнуляем скорость

        movingHeavyRigidbody.angularVelocity = Vector3.zero; // Обнуляем вращение

        movingHeavyRigidbody.constraints = RigidbodyConstraints.FreezeRotation; // Замораживаем вращение
    }

    private void MoveHeavyObject() // Двигает тяжёлый объект
    {
        if (playerCamera == null) return; // Если камеры нет — выходим

        float input = Input.GetAxisRaw("Vertical"); // Читаем W/S

        if (input == 0) return; // Если W/S не нажаты — выходим

        Vector3 forward = playerCamera.transform.forward; // Берём направление камеры

        forward.y = 0f; // Убираем вертикаль

        forward.Normalize(); // Нормализуем направление

        Vector3 newPosition = movingHeavyRigidbody.position + forward * input * heavyMoveSpeed * Time.fixedDeltaTime; // Считаем новую позицию

        movingHeavyRigidbody.MovePosition(newPosition); // Двигаем через физику
    }

    private void StopMovingHeavyObject() // Останавливает тяжёлый объект
    {
        if (movingHeavyRigidbody == null) return; // Если объекта нет — выходим

        movingHeavyRigidbody.constraints = oldHeavyConstraints; // Возвращаем ограничения

        movingHeavyRigidbody.velocity = Vector3.zero; // Обнуляем скорость

        movingHeavyRigidbody.angularVelocity = Vector3.zero; // Обнуляем вращение

        movingHeavyRigidbody = null; // Очищаем ссылку
    }

    private bool IsInLayerMask(int layer, LayerMask layerMask) // Проверяет слой объекта
    {
        return (layerMask.value & (1 << layer)) != 0; // Возвращает true, если слой есть в маске
    }
}