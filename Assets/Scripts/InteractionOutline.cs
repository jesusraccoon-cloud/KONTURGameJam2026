using System.Collections.Generic; // Подключаем списки, чтобы хранить созданные копии Renderer
using UnityEngine; // Подключаем основные Unity-классы
using UnityEngine.Rendering; // Подключаем настройки теней, проб освещения и движения

[DisallowMultipleComponent] // Запрещаем случайно добавлять два одинаковых компонента на один объект
public class InteractionOutline : MonoBehaviour, ILookInteractable // Универсальная обводка объекта при наведении игрока
{
    [Header("Outline Appearance")] // Блок внешнего вида обводки в Inspector
    [ColorUsage(true, true)] // Разрешаем использовать HDR-цвет, чтобы Bloom мог добавить свечение
    public Color outlineColor = new Color(4f, 0.03f, 0.03f, 1f); // Красный HDR-цвет обводки

    [Min(0.0001f)] // Не разрешаем поставить нулевую или отрицательную толщину через Inspector
    public float outlineWidth = 0.012f; // Толщина обводки в мировых единицах Unity

    [Header("Outline Targets")] // Блок выбора моделей, которые должны обводиться
    public Renderer[] outlineRenderers; // Конкретные MeshRenderer или SkinnedMeshRenderer для обводки

    public bool findRenderersInChildrenAutomatically = true; // Если список пустой, автоматически найти модели на объекте и в его дочерних объектах

    public bool includeInactiveRenderers = true; // Учитывать выключенные дочерние модели, например целую и сломанную версии предмета

    [Header("State")] // Блок состояния системы
    public bool canHighlight = true; // Можно ли сейчас показывать обводку этого объекта

    [Header("Shader")] // Блок ссылки на URP-шейдер
    public Shader outlineShader; // Сюда можно назначить шейдер вручную; при пустом поле он будет найден автоматически

    [Header("Debug")] // Блок отладочных настроек
    public bool showDebugWarnings = true; // Показывать предупреждения, если не найден шейдер или Renderer

    private const string ResourceShaderPath = "Shaders/URPInteractionOutline"; // Путь к шейдеру внутри папки Resources
    private const string ShaderName = "KONTUR/URP/Interaction Outline"; // Внутреннее имя шейдера для запасного поиска

    private readonly List<OutlineCopy> outlineCopies = new List<OutlineCopy>(); // Список невидимых служебных копий моделей для рисования контура
    private Material runtimeOutlineMaterial; // Отдельный материал этого объекта, создаваемый во время игры
    private bool isHighlighted; // Запоминаем, включена ли сейчас обводка
    private bool isPrepared; // Запоминаем, была ли уже подготовлена служебная геометрия

    private sealed class OutlineCopy // Внутренний класс, связывающий настоящую модель и её контурную копию
    {
        public Renderer sourceRenderer; // Настоящий Renderer интерактивного объекта
        public Renderer outlineRenderer; // Служебный Renderer, рисующий только контур
        public GameObject outlineObject; // Служебный объект контурной копии
    }

    private void Reset() // Unity вызывает этот метод при первом добавлении компонента в Inspector
    {
        FindRenderersAutomatically(); // Сразу пытаемся заполнить список моделей этого объекта
    }

    private void Awake() // Unity вызывает этот метод при создании объекта во время игры
    {
        PrepareOutline(); // Создаём материал и служебные копии Renderer
        SetOutlineVisible(false); // На старте обязательно прячем контур
    }

    private void LateUpdate() // Выполняется каждый кадр после обычного Update
    {
        if (!isHighlighted) return; // Если обводка выключена, синхронизация не нужна

        ApplyMaterialSettings(); // Применяем цвет и толщину, в том числе после изменения Inspector во время Play Mode
        SynchronizeOutlineRenderers(); // Синхронизируем видимость контура с настоящими моделями
    }

    private void OnDisable() // Вызывается, когда компонент или объект выключается
    {
        isHighlighted = false; // Сбрасываем состояние наведения
        SetOutlineVisible(false); // Выключаем все служебные Renderer, чтобы контур не завис
    }

    private void OnDestroy() // Вызывается при уничтожении объекта
    {
        DestroyOutlineCopies(); // Удаляем созданные служебные объекты

        if (runtimeOutlineMaterial != null) // Проверяем, был ли создан материал
        {
            Destroy(runtimeOutlineMaterial); // Освобождаем созданный во время игры материал
            runtimeOutlineMaterial = null; // Очищаем ссылку на удалённый материал
        }
    }

    private void OnValidate() // Вызывается в редакторе после изменения полей Inspector
    {
        outlineWidth = Mathf.Max(0.0001f, outlineWidth); // Защищаем толщину от нулевого и отрицательного значения

        if (Application.isPlaying && runtimeOutlineMaterial != null) // Проверяем, запущена ли игра и создан ли материал
        {
            ApplyMaterialSettings(); // Сразу обновляем внешний вид во время Play Mode
        }
    }

    public void LookUpdate() // PlayerInteractor вызывает этот метод каждый кадр, пока луч смотрит на объект
    {
        if (!canHighlight) // Проверяем, разрешена ли сейчас подсветка
        {
            LookExit(); // Выключаем уже показанный контур, если разрешение изменилось
            return; // Заканчиваем обработку наведения
        }

        if (!isPrepared) PrepareOutline(); // Если объект ещё не подготовлен, создаём всё необходимое сейчас

        if (!isPrepared || outlineCopies.Count == 0) return; // Если подготовка не удалась или видимой модели нет, безопасно выходим

        isHighlighted = true; // Запоминаем активное наведение
        ApplyMaterialSettings(); // Передаём материалу актуальные цвет и толщину
        SynchronizeOutlineRenderers(); // Включаем контур только у реально видимых моделей
    }

    public void LookExit() // PlayerInteractor вызывает этот метод, когда игрок перестал смотреть на объект
    {
        isHighlighted = false; // Сбрасываем состояние наведения
        SetOutlineVisible(false); // Полностью выключаем контур объекта
    }

    public void SetCanHighlight(bool value) // Публичный метод для разрешения или запрета подсветки из других скриптов
    {
        canHighlight = value; // Сохраняем новое разрешение

        if (!canHighlight) LookExit(); // При запрете немедленно выключаем возможную обводку
    }

    [ContextMenu("Find Renderers Automatically")] // Добавляем удобную команду в контекстное меню компонента
    public void FindRenderersAutomatically() // Автоматически собирает поддерживаемые Renderer внутри этого объекта
    {
        Renderer[] foundRenderers = GetComponentsInChildren<Renderer>(includeInactiveRenderers); // Ищем все Renderer на объекте и ниже по Hierarchy
        List<Renderer> supportedRenderers = new List<Renderer>(); // Создаём временный список только поддерживаемых моделей

        for (int i = 0; i < foundRenderers.Length; i++) // Перебираем все найденные Renderer
        {
            Renderer foundRenderer = foundRenderers[i]; // Берём очередной найденный Renderer

            if (foundRenderer is MeshRenderer || foundRenderer is SkinnedMeshRenderer) // Проверяем поддерживаемый тип модели
            {
                supportedRenderers.Add(foundRenderer); // Добавляем модель в итоговый список
            }
        }

        outlineRenderers = supportedRenderers.ToArray(); // Записываем найденные модели в Inspector
    }

    private void PrepareOutline() // Создаёт материал и контурные копии всех назначенных моделей
    {
        if (isPrepared) return; // Не создаём копии повторно

        if ((outlineRenderers == null || outlineRenderers.Length == 0) && findRenderersInChildrenAutomatically) // Проверяем, нужно ли выполнить автоматический поиск
        {
            FindRenderersAutomatically(); // Ищем модели на текущем объекте и в дочерних объектах
        }

        if (!CreateRuntimeMaterial()) // Проверяем, удалось ли создать материал
        {
            isPrepared = true; // Запоминаем неудачную попытку, чтобы не писать одинаковое предупреждение каждый кадр
            return; // Если материал создать невозможно, прекращаем подготовку
        }

        CreateOutlineCopies(); // Создаём отдельную контурную копию для каждого Renderer
        isPrepared = true; // Запоминаем завершённую подготовку, даже если у временного объекта пока нет модели

        if (outlineCopies.Count == 0 && showDebugWarnings) // Проверяем, не остался ли объект без видимой геометрии
        {
            Debug.LogWarning("InteractionOutline на объекте '" + name + "' не нашёл MeshRenderer или SkinnedMeshRenderer. Collider определяет наведение, но для рисования контура нужна видимая модель.", this); // Объясняем проблему в Console
        }
    }

    private bool CreateRuntimeMaterial() // Создаёт отдельный материал для контура этого объекта
    {
        if (runtimeOutlineMaterial != null) return true; // Если материал уже существует, повторно его не создаём

        if (outlineShader == null) // Проверяем, назначен ли шейдер вручную
        {
            outlineShader = Resources.Load<Shader>(ResourceShaderPath); // Сначала ищем шейдер по надёжному пути внутри Resources
        }

        if (outlineShader == null) // Если загрузка из Resources не сработала
        {
            outlineShader = Shader.Find(ShaderName); // Пробуем найти шейдер по его внутреннему имени
        }

        if (outlineShader == null) // Проверяем результат обоих способов поиска
        {
            if (showDebugWarnings) // Проверяем, разрешены ли предупреждения
            {
                Debug.LogWarning("InteractionOutline не нашёл URP-шейдер. Помести URPInteractionOutline.shader в Assets/Resources/Shaders/.", this); // Сообщаем точное место для файла
            }

            return false; // Сообщаем, что материал создать не удалось
        }

        runtimeOutlineMaterial = new Material(outlineShader); // Создаём отдельный экземпляр материала для этого объекта
        runtimeOutlineMaterial.name = name + " Runtime Interaction Outline"; // Даём материалу понятное имя для Frame Debugger
        runtimeOutlineMaterial.hideFlags = HideFlags.DontSave; // Запрещаем сохранять временный материал в сцену или prefab
        ApplyMaterialSettings(); // Передаём материалу начальные цвет и толщину
        return true; // Сообщаем об успешном создании материала
    }

    private void ApplyMaterialSettings() // Передаёт пользовательские параметры в URP-шейдер
    {
        if (runtimeOutlineMaterial == null) return; // Если материала нет, менять нечего

        runtimeOutlineMaterial.SetColor("_OutlineColor", outlineColor); // Передаём HDR-цвет обводки
        runtimeOutlineMaterial.SetFloat("_OutlineWidth", outlineWidth); // Передаём толщину обводки
    }

    private void CreateOutlineCopies() // Создаёт служебные копии назначенных Renderer
    {
        if (outlineRenderers == null) return; // Если список моделей отсутствует, выходим

        HashSet<Renderer> uniqueRenderers = new HashSet<Renderer>(); // Создаём набор для защиты от повторяющихся ссылок

        for (int i = 0; i < outlineRenderers.Length; i++) // Перебираем все назначенные Renderer
        {
            Renderer sourceRenderer = outlineRenderers[i]; // Получаем очередную настоящую модель

            if (sourceRenderer == null) continue; // Пропускаем пустую ячейку Inspector
            if (!uniqueRenderers.Add(sourceRenderer)) continue; // Пропускаем Renderer, который уже был обработан

            OutlineCopy outlineCopy = CreateOutlineCopy(sourceRenderer); // Пытаемся создать контурную копию модели

            if (outlineCopy != null) outlineCopies.Add(outlineCopy); // Сохраняем успешно созданную копию
        }
    }

    private OutlineCopy CreateOutlineCopy(Renderer sourceRenderer) // Создаёт одну контурную копию MeshRenderer или SkinnedMeshRenderer
    {
        GameObject outlineObject = new GameObject(sourceRenderer.gameObject.name + " [Interaction Outline]"); // Создаём служебный дочерний объект
        outlineObject.transform.SetParent(sourceRenderer.transform, false); // Помещаем копию внутрь настоящей модели без изменения локальной позиции
        outlineObject.transform.localPosition = Vector3.zero; // Совмещаем позицию копии с оригиналом
        outlineObject.transform.localRotation = Quaternion.identity; // Совмещаем поворот копии с оригиналом
        outlineObject.transform.localScale = Vector3.one; // Совмещаем масштаб копии с оригиналом
        outlineObject.layer = sourceRenderer.gameObject.layer; // Копируем слой, чтобы контур виделся теми же камерами
        outlineObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave; // Прячем технический объект из Hierarchy и не сохраняем его

        Renderer createdRenderer = null; // Заранее создаём ссылку на будущий контурный Renderer
        int materialSlotCount = 0; // Здесь будет количество submesh, которым нужен контурный материал

        MeshRenderer sourceMeshRenderer = sourceRenderer as MeshRenderer; // Проверяем, является ли источник обычным MeshRenderer

        if (sourceMeshRenderer != null) // Обрабатываем обычную статическую или движущуюся модель
        {
            MeshFilter sourceMeshFilter = sourceMeshRenderer.GetComponent<MeshFilter>(); // Ищем MeshFilter рядом с настоящим MeshRenderer

            if (sourceMeshFilter == null || sourceMeshFilter.sharedMesh == null) // Проверяем наличие самой сетки
            {
                Destroy(outlineObject); // Удаляем бесполезный служебный объект
                return null; // Сообщаем, что копию создать не удалось
            }

            MeshFilter outlineMeshFilter = outlineObject.AddComponent<MeshFilter>(); // Добавляем MeshFilter контурной копии
            outlineMeshFilter.sharedMesh = sourceMeshFilter.sharedMesh; // Используем ту же сетку без копирования памяти

            MeshRenderer outlineMeshRenderer = outlineObject.AddComponent<MeshRenderer>(); // Добавляем Renderer для контура
            createdRenderer = outlineMeshRenderer; // Сохраняем общий Renderer
            materialSlotCount = Mathf.Max(1, sourceMeshFilter.sharedMesh.subMeshCount); // Получаем число частей сетки
        }
        else // Если это не обычный MeshRenderer, проверяем SkinnedMeshRenderer
        {
            SkinnedMeshRenderer sourceSkinnedRenderer = sourceRenderer as SkinnedMeshRenderer; // Пробуем получить анимированный Renderer

            if (sourceSkinnedRenderer == null || sourceSkinnedRenderer.sharedMesh == null) // Проверяем поддерживаемый тип и наличие сетки
            {
                Destroy(outlineObject); // Удаляем неподходящий служебный объект
                return null; // Сообщаем, что этот Renderer не поддерживается
            }

            SkinnedMeshRenderer outlineSkinnedRenderer = outlineObject.AddComponent<SkinnedMeshRenderer>(); // Создаём анимированную контурную копию
            outlineSkinnedRenderer.sharedMesh = sourceSkinnedRenderer.sharedMesh; // Используем ту же сетку
            outlineSkinnedRenderer.rootBone = sourceSkinnedRenderer.rootBone; // Копируем корневую кость
            outlineSkinnedRenderer.bones = sourceSkinnedRenderer.bones; // Копируем все кости анимации
            outlineSkinnedRenderer.localBounds = sourceSkinnedRenderer.localBounds; // Копируем границы модели
            outlineSkinnedRenderer.quality = sourceSkinnedRenderer.quality; // Копируем качество расчёта костей
            outlineSkinnedRenderer.updateWhenOffscreen = sourceSkinnedRenderer.updateWhenOffscreen; // Копируем режим обновления вне экрана
            createdRenderer = outlineSkinnedRenderer; // Сохраняем общий Renderer
            materialSlotCount = Mathf.Max(1, sourceSkinnedRenderer.sharedMesh.subMeshCount); // Получаем число частей анимированной сетки
        }

        ConfigureOutlineRenderer(createdRenderer, sourceRenderer, materialSlotCount); // Настраиваем созданный Renderer для рисования только контура

        OutlineCopy result = new OutlineCopy(); // Создаём запись о связи оригинала и копии
        result.sourceRenderer = sourceRenderer; // Сохраняем настоящий Renderer
        result.outlineRenderer = createdRenderer; // Сохраняем контурный Renderer
        result.outlineObject = outlineObject; // Сохраняем служебный объект
        return result; // Возвращаем готовую контурную копию
    }

    private void ConfigureOutlineRenderer(Renderer outlineRenderer, Renderer sourceRenderer, int materialSlotCount) // Применяет общие настройки к контурной копии
    {
        Material[] outlineMaterials = new Material[materialSlotCount]; // Создаём по одному слоту материала на каждый submesh

        for (int i = 0; i < outlineMaterials.Length; i++) // Перебираем все слоты материала
        {
            outlineMaterials[i] = runtimeOutlineMaterial; // Назначаем один контурный материал каждой части сетки
        }

        outlineRenderer.sharedMaterials = outlineMaterials; // Применяем массив без создания лишних копий материалов
        outlineRenderer.shadowCastingMode = ShadowCastingMode.Off; // Контур не должен отбрасывать тени
        outlineRenderer.receiveShadows = false; // Контур не должен принимать тени
        outlineRenderer.lightProbeUsage = LightProbeUsage.Off; // Контур не использует пробы освещения
        outlineRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off; // Контур не использует отражения
        outlineRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion; // Контур не создаёт отдельные векторы движения
        outlineRenderer.allowOcclusionWhenDynamic = false; // Не разрешаем системе окклюзии случайно скрывать отдельную копию
        outlineRenderer.renderingLayerMask = sourceRenderer.renderingLayerMask; // Копируем URP Rendering Layer Mask
        outlineRenderer.sortingLayerID = sourceRenderer.sortingLayerID; // Копируем Sorting Layer
        outlineRenderer.sortingOrder = sourceRenderer.sortingOrder + 1; // Рисуем контур сразу после оригинальной модели
        outlineRenderer.enabled = false; // До наведения держим контур выключенным
    }

    private void SynchronizeOutlineRenderers() // Синхронизирует контур с текущей видимостью настоящих моделей
    {
        for (int i = 0; i < outlineCopies.Count; i++) // Перебираем все созданные контурные копии
        {
            OutlineCopy outlineCopy = outlineCopies[i]; // Берём очередную связь оригинала и копии

            if (outlineCopy == null || outlineCopy.outlineRenderer == null) continue; // Пропускаем удалённую копию

            bool sourceIsVisible = outlineCopy.sourceRenderer != null // Проверяем, существует ли оригинал
                && outlineCopy.sourceRenderer.enabled // Проверяем, включён ли настоящий Renderer
                && outlineCopy.sourceRenderer.gameObject.activeInHierarchy; // Проверяем, активен ли настоящий объект в Hierarchy

            outlineCopy.outlineRenderer.enabled = isHighlighted && canHighlight && sourceIsVisible; // Показываем контур только при наведении и видимом оригинале
        }
    }

    private void SetOutlineVisible(bool visible) // Включает или выключает все контурные Renderer
    {
        for (int i = 0; i < outlineCopies.Count; i++) // Перебираем все контурные копии
        {
            OutlineCopy outlineCopy = outlineCopies[i]; // Берём очередную копию

            if (outlineCopy != null && outlineCopy.outlineRenderer != null) // Проверяем, что Renderer ещё существует
            {
                outlineCopy.outlineRenderer.enabled = visible; // Устанавливаем требуемую видимость
            }
        }
    }

    private void DestroyOutlineCopies() // Удаляет все созданные служебные объекты
    {
        for (int i = 0; i < outlineCopies.Count; i++) // Перебираем сохранённые копии
        {
            OutlineCopy outlineCopy = outlineCopies[i]; // Берём очередную копию

            if (outlineCopy != null && outlineCopy.outlineObject != null) // Проверяем наличие служебного объекта
            {
                Destroy(outlineCopy.outlineObject); // Удаляем служебную геометрию
            }
        }

        outlineCopies.Clear(); // Очищаем список удалённых копий
    }
}
