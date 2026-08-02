using UnityEngine; // Подключаем Unity-классы.
using UnityEngine.AI; // Подключаем NavMeshObstacle.
using System.Collections; // Подключаем корутины.

public class UniversalDoor : MonoBehaviour // Универсальная дверь.
{
    public enum DoorOpenDirection { Forward, Backward } // Направление открытия.
    public enum DoorRotationAxis { X, Y, Z } // Ось вращения.

    [Header("Door Settings")]
    public bool isOpen = false; // Полностью ли открыта дверь.

    public bool IsOpen => isOpen; // Публичное логическое состояние.
    public bool IsBusy => isBusy; // Выполняется ли команда двери.
    public bool IsPeekOpen => isPeekOpen; // Находится ли дверь в положении щели.
    public bool IsFullyOpen => isOpen && !isPeekOpen && Quaternion.Angle(transform.localRotation, openedRotation) <= fullyOpenAngleTolerance;
    public bool CanMonsterOpenNow => canMonsterOpen && !isBusy && !isOpen && !isPeekOpen && !isLocked && CanOpenDoor();

    public DoorOpenDirection openDirection = DoorOpenDirection.Forward;

    [Header("Rotation Axis")]
    public DoorRotationAxis rotationAxis = DoorRotationAxis.Y;

    [Header("Open Angle")]
    public float openAngle = 90f;
    public float fullyOpenAngleTolerance = 2f;

    [Header("Open / Close Speed")]
    public float openSpeed = 5f;
    public float closeSpeed = 7f;

    [Header("Peek Position")]
    [Min(0f)] public float defaultPeekAngle = 10f; // Угол щели по умолчанию.
    [Min(0.01f)] public float peekSpeed = 5f; // Скорость перехода в положение щели.

    [Header("Auto Close")]
    public bool autoClose = false;
    [Min(0f)] public float autoCloseDelay = 3f;
    public bool AutoCloseEnabled => autoClose;

    [Header("Interact Zone")]
    public Collider doorInteractZone;

    [Header("Handles")]
    public Transform outsideHandle;
    public Transform insideHandle;
    public float handleDownAngle = 20f;
    public float handlePressSpeed = 12f;
    public float handleReturnSpeed = 10f;
    public float handleHoldTime = 0.05f;

    [Header("Door Delay")]
    public float doorOpenDelay = 0.03f;

    [Header("Monster Access")]
    public bool canMonsterOpen = true;
    public NavMeshObstacle navMeshObstacle;

    [Header("Noise")]
    public NoiseEmitter noiseEmitter;
    [Range(1, 10)] public int openNoisePower = 3;
    [Range(1, 10)] public int closeNoisePower = 4;

    [Header("Lock")]
    public bool isLocked = false;

    [Header("Tumbler Lock")]
    public bool requiresTumbler = false;
    public TumblerSwitch requiredTumbler;

    private Quaternion closedRotation;
    private Quaternion openedRotation;
    private Quaternion peekRotation;
    private Quaternion outsideHandleStartRotation;
    private Quaternion insideHandleStartRotation;
    private Quaternion outsideHandlePressedRotation;
    private Quaternion insideHandlePressedRotation;
    private bool isBusy = false;
    private bool isPeekOpen = false;
    private Coroutine autoCloseCoroutine;

    private void OnValidate()
    {
        if (navMeshObstacle == null) navMeshObstacle = GetComponent<NavMeshObstacle>();
        if (autoCloseDelay < 0f) autoCloseDelay = 0f;
        if (defaultPeekAngle < 0f) defaultPeekAngle = 0f;
        if (peekSpeed < 0.01f) peekSpeed = 0.01f;
        SyncNavMeshObstacleWithBoxCollider();
    }

    private void Start()
    {
        closedRotation = transform.localRotation;
        openedRotation = BuildRotationForAngle(openAngle);
        peekRotation = BuildRotationForAngle(defaultPeekAngle);

        if (outsideHandle != null)
        {
            outsideHandleStartRotation = outsideHandle.localRotation;
            outsideHandlePressedRotation = outsideHandleStartRotation * Quaternion.Euler(0f, 0f, -handleDownAngle);
        }

        if (insideHandle != null)
        {
            insideHandleStartRotation = insideHandle.localRotation;
            insideHandlePressedRotation = insideHandleStartRotation * Quaternion.Euler(0f, 0f, -handleDownAngle);
        }

        if (noiseEmitter == null) noiseEmitter = GetComponent<NoiseEmitter>();
        if (navMeshObstacle == null) navMeshObstacle = GetComponent<NavMeshObstacle>();
        SyncNavMeshObstacleWithBoxCollider();
        if (navMeshObstacle != null) navMeshObstacle.carving = false;
        SetupDoorInteractZone();
    }

    private void Update()
    {
        UpdateDoorRotation();
        UpdateNavMeshObstacle();
    }

    private Quaternion BuildRotationForAngle(float angle)
    {
        float direction = openDirection == DoorOpenDirection.Forward ? 1f : -1f;
        Vector3 rotationVector = Vector3.zero;
        if (rotationAxis == DoorRotationAxis.X) rotationVector = new Vector3(angle * direction, 0f, 0f);
        if (rotationAxis == DoorRotationAxis.Y) rotationVector = new Vector3(0f, angle * direction, 0f);
        if (rotationAxis == DoorRotationAxis.Z) rotationVector = new Vector3(0f, 0f, angle * direction);
        return closedRotation * Quaternion.Euler(rotationVector);
    }

    private void SyncNavMeshObstacleWithBoxCollider()
    {
        if (navMeshObstacle == null) return;
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null) return;
        navMeshObstacle.shape = NavMeshObstacleShape.Box;
        navMeshObstacle.center = boxCollider.center;
        navMeshObstacle.size = boxCollider.size;
        navMeshObstacle.carveOnlyStationary = true;
    }

    private void UpdateNavMeshObstacle()
    {
        if (navMeshObstacle == null) return;
        bool shouldCarve = IsFullyOpen;
        if (navMeshObstacle.carving == shouldCarve) return;
        navMeshObstacle.carving = shouldCarve;
    }

    private void SetupDoorInteractZone()
    {
        if (doorInteractZone == null) return;
        UniversalDoorInteractForwarder forwarder = doorInteractZone.GetComponent<UniversalDoorInteractForwarder>();
        if (forwarder == null) forwarder = doorInteractZone.gameObject.AddComponent<UniversalDoorInteractForwarder>();
        forwarder.door = this;
    }

    private bool CanOpenDoor()
    {
        if (!requiresTumbler) return true;
        if (requiredTumbler == null) return false;
        return requiredTumbler.isOn;
    }

    public void Interact()
    {
        if (isBusy || isLocked) return;
        if (isPeekOpen) { OpenDoor(); return; }
        if (!isOpen && CanOpenDoor()) ToggleDoor();
        else if (isOpen) ToggleDoor();
    }

    public void ToggleDoor()
    {
        if (isBusy) return;
        if (isPeekOpen) { OpenDoor(); return; }
        StartCoroutine(InteractSequence(!isOpen));
    }

    public void OpenDoor()
    {
        if (isBusy || isOpen || isLocked || !CanOpenDoor()) return;
        isPeekOpen = false;
        StartCoroutine(InteractSequence(true));
    }

    public void CloseDoor()
    {
        if (isBusy) return;
        CancelAutoCloseCountdown();
        if (isPeekOpen)
        {
            isPeekOpen = false;
            isOpen = false;
            return;
        }
        if (!isOpen) return;
        StartCoroutine(InteractSequence(false));
    }

    public void SetPeekPosition() { SetPeekPosition(defaultPeekAngle); }

    public void SetPeekPosition(float angle)
    {
        if (isBusy) return;
        CancelAutoCloseCountdown();
        float safeAngle = Mathf.Clamp(angle, 0f, Mathf.Abs(openAngle));
        peekRotation = BuildRotationForAngle(safeAngle);
        isOpen = false;
        isPeekOpen = true;
    }

    public void SetAutoClose(bool value)
    {
        autoClose = value;
        if (!autoClose) { CancelAutoCloseCountdown(); return; }
        if (isOpen && !isPeekOpen && !isBusy) StartAutoCloseCountdown();
    }

    public void EnableAutoClose() { SetAutoClose(true); }
    public void DisableAutoClose() { SetAutoClose(false); }
    public void ToggleAutoClose() { SetAutoClose(!autoClose); }

    private void StartAutoCloseCountdown()
    {
        CancelAutoCloseCountdown();
        if (!autoClose || !isOpen || isPeekOpen) return;
        autoCloseCoroutine = StartCoroutine(AutoCloseAfterDelay());
    }

    private void CancelAutoCloseCountdown()
    {
        if (autoCloseCoroutine == null) return;
        StopCoroutine(autoCloseCoroutine);
        autoCloseCoroutine = null;
    }

    private IEnumerator AutoCloseAfterDelay()
    {
        if (autoCloseDelay > 0f) yield return new WaitForSeconds(autoCloseDelay);
        while (isBusy) yield return null;
        if (!autoClose || !isOpen || isPeekOpen)
        {
            autoCloseCoroutine = null;
            yield break;
        }
        autoCloseCoroutine = null;
        CloseDoor();
    }

    public void SetLocked(bool value) { isLocked = value; }
    public void UnlockDoor() { isLocked = false; }
    public void LockDoor() { isLocked = true; }
    public void SetMonsterCanOpen(bool value) { canMonsterOpen = value; }

    public bool OpenDoorForMonster()
    {
        if (!CanMonsterOpenNow) return false;
        isPeekOpen = false;
        StartCoroutine(InteractSequence(true));
        return true;
    }

    private IEnumerator InteractSequence(bool targetOpenState)
    {
        if (!targetOpenState) CancelAutoCloseCountdown();
        isBusy = true;
        yield return StartCoroutine(PressHandlesDown());
        if (doorOpenDelay > 0f) yield return new WaitForSeconds(doorOpenDelay);
        isPeekOpen = false;
        isOpen = targetOpenState;
        EmitDoorNoise(targetOpenState);
        if (handleHoldTime > 0f) yield return new WaitForSeconds(handleHoldTime);
        yield return StartCoroutine(ReturnHandlesBack());
        isBusy = false;
        if (targetOpenState && autoClose) StartAutoCloseCountdown();
    }

    private IEnumerator PressHandlesDown()
    {
        float t = 0f;
        Quaternion outStart = outsideHandle != null ? outsideHandle.localRotation : Quaternion.identity;
        Quaternion inStart = insideHandle != null ? insideHandle.localRotation : Quaternion.identity;
        while (t < 1f)
        {
            t += Time.deltaTime * handlePressSpeed;
            if (outsideHandle != null) outsideHandle.localRotation = Quaternion.Lerp(outStart, outsideHandlePressedRotation, t);
            if (insideHandle != null) insideHandle.localRotation = Quaternion.Lerp(inStart, insideHandlePressedRotation, t);
            yield return null;
        }
    }

    private IEnumerator ReturnHandlesBack()
    {
        float t = 0f;
        Quaternion outStart = outsideHandle != null ? outsideHandle.localRotation : Quaternion.identity;
        Quaternion inStart = insideHandle != null ? insideHandle.localRotation : Quaternion.identity;
        while (t < 1f)
        {
            t += Time.deltaTime * handleReturnSpeed;
            if (outsideHandle != null) outsideHandle.localRotation = Quaternion.Lerp(outStart, outsideHandleStartRotation, t);
            if (insideHandle != null) insideHandle.localRotation = Quaternion.Lerp(inStart, insideHandleStartRotation, t);
            yield return null;
        }
    }

    private void UpdateDoorRotation()
    {
        Quaternion targetRotation;
        float movementSpeed;
        if (isPeekOpen)
        {
            targetRotation = peekRotation;
            movementSpeed = peekSpeed;
        }
        else if (isOpen)
        {
            targetRotation = openedRotation;
            movementSpeed = openSpeed;
        }
        else
        {
            targetRotation = closedRotation;
            movementSpeed = closeSpeed;
        }
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * movementSpeed);
    }

    private void EmitDoorNoise(bool targetOpenState)
    {
        if (noiseEmitter == null) return;
        int noisePower = targetOpenState ? openNoisePower : closeNoisePower;
        noiseEmitter.EmitNoise(noisePower);
    }
}

public class UniversalDoorInteractForwarder : MonoBehaviour, IInteractable
{
    public UniversalDoor door;
    public void Interact()
    {
        if (door == null) door = GetComponentInParent<UniversalDoor>();
        if (door == null) return;
        door.Interact();
    }
}