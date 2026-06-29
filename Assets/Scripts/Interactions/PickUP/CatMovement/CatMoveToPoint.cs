using UnityEngine; // Подключаем библиотеку Unity
using System.Collections.Generic; // Подключаем работу со списками

public class CatMoveToPoint : MonoBehaviour // Скрипт вешается на главный объект кота
{
    public Transform groundPoint; // Опорная точка кота на полу

    public List<Transform> routeToDoor = new List<Transform>(); // Первый маршрут кота

    public List<Transform> routeInsideApartment = new List<Transform>(); // Второй маршрут кота

    public bool waitForDoor = true; // Галочка: ждать ли открытия двери после первого маршрута

    public UniversalDoor targetDoor; // Дверь, открытия которой кот будет ждать

    public bool useTeleportAfterDoorRoute = false; // Галочка: использовать ли телепорт после первого маршрута

    public ObjectTeleport objectTeleport; // Ссылка на отдельный скрипт телепорта

    public float moveSpeed = 4f; // Скорость движения кота

    public float stopDistance = 0.1f; // Дистанция, при которой точка считается достигнутой

    private bool hasStarted = false; // Был ли запущен сценарий кота

    private bool isMoving = false; // Двигается ли кот сейчас

    private bool waitingForDoor = false; // Ждёт ли кот дверь сейчас

    private bool goingInside = false; // Перешёл ли кот ко второму маршруту

    private int currentDoorRouteIndex = 0; // Индекс текущей точки первого маршрута

    private int currentInsideRouteIndex = 0; // Индекс текущей точки второго маршрута

    void Update() // Unity вызывает этот метод каждый кадр
    {
        if (!hasStarted) // Если сценарий кота ещё не запускался
            return; // Ничего не делаем

        if (groundPoint == null) // Если опорная точка кота не назначена
        {
            Debug.Log("Ground Point не назначен"); // Пишем сообщение в консоль
            return; // Выходим из Update
        }

        if (routeToDoor == null || routeToDoor.Count == 0) // Если первый маршрут пустой
        {
            Debug.Log("Route To Door не назначен"); // Пишем сообщение в консоль
            return; // Выходим из Update
        }

        if (waitForDoor && targetDoor == null) // Если включено ожидание двери, но дверь не назначена
        {
            Debug.Log("Target Door не назначена, хотя Wait For Door включён"); // Пишем сообщение в консоль
            return; // Выходим из Update
        }

        if (useTeleportAfterDoorRoute && objectTeleport == null) // Если включён телепорт, но скрипт телепорта не назначен
        {
            Debug.Log("Object Teleport не назначен, хотя Use Teleport включён"); // Пишем сообщение в консоль
            return; // Выходим из Update
        }

        if (waitingForDoor) // Если кот сейчас стоит и ждёт дверь
        {
            if (targetDoor.IsOpen) // Если дверь открылась
            {
                Debug.Log("Дверь открыта, кот продолжает сценарий"); // Пишем сообщение в консоль

                waitingForDoor = false; // Отключаем ожидание двери

                ContinueAfterFirstRoute(); // Продолжаем сценарий после первого маршрута
            }

            return; // Пока кот ждёт дверь, остальную логику не выполняем
        }

        if (isMoving) // Если кот должен двигаться
        {
            if (!goingInside) // Если кот ещё на первом маршруте
            {
                MoveAlongDoorRoute(); // Двигаем кота по первому маршруту
                return; // Выходим из Update
            }

            MoveAlongInsideRoute(); // Двигаем кота по второму маршруту
            return; // Выходим из Update
        }
    }

    public void StartMoving() // Метод запуска сценария кота
    {
        Debug.Log("Кот запущен"); // Пишем сообщение в консоль

        hasStarted = true; // Помечаем, что сценарий запущен

        isMoving = true; // Включаем движение

        waitingForDoor = false; // Сбрасываем ожидание двери

        goingInside = false; // Сбрасываем переход ко второму маршруту

        currentDoorRouteIndex = 0; // Начинаем первый маршрут с первой точки

        currentInsideRouteIndex = 0; // Сбрасываем индекс второго маршрута
    }

    void MoveAlongDoorRoute() // Метод движения по первому маршруту
    {
        if (currentDoorRouteIndex >= routeToDoor.Count) // Если первый маршрут закончился
        {
            Debug.Log("Кот дошёл до конца первого маршрута"); // Пишем сообщение в консоль

            isMoving = false; // Останавливаем движение

            if (waitForDoor) // Если включена галочка ожидания двери
            {
                waitingForDoor = true; // Включаем режим ожидания двери
                return; // Выходим, пока дверь не откроется
            }

            ContinueAfterFirstRoute(); // Если дверь ждать не нужно, сразу продолжаем сценарий

            return; // Выходим из метода
        }

        Transform currentTarget = routeToDoor[currentDoorRouteIndex]; // Берём текущую точку первого маршрута

        if (currentTarget == null) // Если текущая точка не назначена
        {
            Debug.Log("Одна из точек routeToDoor не назначена"); // Пишем сообщение в консоль
            return; // Выходим из метода
        }

        MoveToTarget(currentTarget, true); // Двигаем кота к текущей точке первого маршрута
    }

    void ContinueAfterFirstRoute() // Метод продолжения сценария после первого маршрута
    {
        if (useTeleportAfterDoorRoute) // Если включён телепорт
        {
            objectTeleport.StartTeleport(); // Запускаем отдельный скрипт телепорта
        }

        currentInsideRouteIndex = 0; // Начинаем второй маршрут с первой точки

        goingInside = true; // Переключаемся на второй маршрут

        if (routeInsideApartment != null && routeInsideApartment.Count > 0) // Если второй маршрут назначен
        {
            isMoving = true; // Включаем движение по второму маршруту
        }
        else // Если второго маршрута нет
        {
            isMoving = false; // Оставляем кота стоять
        }
    }

    void MoveAlongInsideRoute() // Метод движения по второму маршруту
    {
        if (routeInsideApartment == null || routeInsideApartment.Count == 0) // Если второй маршрут пустой
        {
            isMoving = false; // Останавливаем кота
            return; // Выходим из метода
        }

        if (currentInsideRouteIndex >= routeInsideApartment.Count) // Если второй маршрут закончился
        {
            Debug.Log("Кот закончил второй маршрут"); // Пишем сообщение в консоль

            isMoving = false; // Останавливаем движение

            return; // Выходим из метода
        }

        Transform currentTarget = routeInsideApartment[currentInsideRouteIndex]; // Берём текущую точку второго маршрута

        if (currentTarget == null) // Если текущая точка второго маршрута не назначена
        {
            Debug.Log("Одна из точек routeInsideApartment не назначена"); // Пишем сообщение в консоль
            return; // Выходим из метода
        }

        MoveToTarget(currentTarget, false); // Двигаем кота к текущей точке второго маршрута
    }

    void MoveToTarget(Transform targetPoint, bool isDoorRoute) // Универсальный метод движения к точке
    {
        Vector3 currentGroundPosition = groundPoint.position; // Берём позицию опорной точки кота

        Vector3 targetPosition = targetPoint.position; // Берём позицию целевой точки

        Vector3 direction = (targetPosition - currentGroundPosition).normalized; // Считаем направление к цели

        float distance = Vector3.Distance(currentGroundPosition, targetPosition); // Считаем расстояние до цели

        if (distance <= stopDistance) // Если кот дошёл до точки
        {
            if (isDoorRoute) // Если это первый маршрут
            {
                Debug.Log("Кот дошёл до точки первого маршрута: " + currentDoorRouteIndex); // Пишем номер точки

                currentDoorRouteIndex++; // Переходим к следующей точке первого маршрута
            }
            else // Если это второй маршрут
            {
                Debug.Log("Кот дошёл до точки второго маршрута: " + currentInsideRouteIndex); // Пишем номер точки

                currentInsideRouteIndex++; // Переходим к следующей точке второго маршрута
            }

            return; // Выходим, чтобы новую точку взять на следующем кадре
        }

        transform.position += direction * moveSpeed * Time.deltaTime; // Двигаем кота к цели

        Vector3 flatDirection = new Vector3(direction.x, 0f, direction.z); // Убираем вертикаль из направления

        if (flatDirection != Vector3.zero) // Если направление не нулевое
        {
            transform.forward = flatDirection; // Поворачиваем кота в сторону движения
        }
    }
}