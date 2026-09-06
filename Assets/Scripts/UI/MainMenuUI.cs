using UnityEngine;

// Gắn vào 1 GameObject trong Scene MainMenu, điều khiển các panel: Main Menu, Stage Select, Confirm New Game.
public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject stageSelectPanel;
    [SerializeField] private GameObject newGameConfirmPanel;

    private void Start()
    {
        // Phòng trường hợp Scene trước đó (Pause/GameOver) rời đi khi Time.timeScale đang = 0
        Time.timeScale = 1f;
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (stageSelectPanel != null) stageSelectPanel.SetActive(false);
        if (newGameConfirmPanel != null) newGameConfirmPanel.SetActive(false);
    }

    public void ShowStageSelect()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (stageSelectPanel != null) stageSelectPanel.SetActive(true);
        if (newGameConfirmPanel != null) newGameConfirmPanel.SetActive(false);
    }

    // Gọi từ nút "New Game" — chỉ mở modal hỏi xác nhận, chưa reset gì cả
    public void ShowNewGameConfirm()
    {
        if (newGameConfirmPanel != null) newGameConfirmPanel.SetActive(true);
    }

    // Gọi từ nút "No/Cancel" trên modal
    public void CancelNewGameConfirm()
    {
        if (newGameConfirmPanel != null) newGameConfirmPanel.SetActive(false);
    }

    // Gọi từ nút "Yes" trên modal — khóa lại toàn bộ Stage rồi mở Stage Select để chơi lại từ Level 1
    public void ConfirmNewGame()
    {
        GameProgress.ResetProgress();
        ShowStageSelect();
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
