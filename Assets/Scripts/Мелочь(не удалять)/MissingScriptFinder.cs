using UnityEngine; // Подключаем библиотеку Unity с GameObject, Component, MonoBehaviour и Debug

public class MissingScriptFinder : MonoBehaviour // Создаем компонент MissingScriptFinder который можно повесить на объект
{
    [ContextMenu("Find Missing Scripts")] // Добавляем кнопку в контекстное меню компонента (три точки в Inspector)
    public void Find() // Метод Find запускается вручную через Inspector
    {
        // Получаем ВСЕ GameObject в сцене
        // true = включая отключенные объекты
        GameObject[] objects = FindObjectsOfType<GameObject>(true);

        // Проходим по каждому объекту сцены
        foreach (GameObject go in objects)
        {
            // Получаем все компоненты на текущем объекте
            Component[] components = go.GetComponents<Component>();

            // Проходим по каждому компоненту
            foreach (Component component in components)
            {
                // Если component == null
                // значит Unity потерял скрипт
                // это и есть Missing Script
                if (component == null)
                {
                    // Выводим сообщение в Console
                    // GetPath(go) = полный путь объекта
                    // go передаем чтобы можно было кликнуть по сообщению
                    // и Unity выделила объект
                    Debug.Log("Missing script on: " + GetPath(go), go);
                }
            }
        }
    }

    // Метод получения полного пути объекта в Hierarchy
    private string GetPath(GameObject obj)
    {
        // Сохраняем имя текущего объекта
        string path = obj.name;

        // Пока у объекта есть родитель
        while (obj.transform.parent != null)
        {
            // Поднимаемся на уровень выше
            obj = obj.transform.parent.gameObject;

            // Добавляем родителя в начало строки
            // Получится:
            // SceneRoot/Player/MainCamera
            path = obj.name + "/" + path;
        }

        // Возвращаем полный путь
        return path;
    }
}