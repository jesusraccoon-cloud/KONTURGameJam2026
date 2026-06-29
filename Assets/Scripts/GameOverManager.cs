using UnityEngine; // Основные функции Unity
using UnityEngine.SceneManagement; // Для перезагрузки сцены

public class GameOverManager : MonoBehaviour // Управление экраном Game Over
{
    public GameObject gameOverPanel; // Панель Game Over

    public void ShowGameOver() // Показать экран поражения
    {
        if (gameOverPanel != null) // Если панель назначена
        {
            gameOverPanel.SetActive(true); // Включаем панель
        }

        Cursor.lockState = CursorLockMode.None; // Освобождаем курсор
        Cursor.visible = true; // Показываем курсор

        Time.timeScale = 0f; // Останавливаем игру
    }

    public void RestartScene() // Перезапустить текущую сцену
    {
        Time.timeScale = 1f; // Возвращаем нормальное время

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); // Загружаем сцену заново
    }
}