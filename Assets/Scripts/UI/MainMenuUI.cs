using UnityEngine;

// Gắn vào 1 GameObject trong Scene MainMenu, điều khiển các panel: Main Menu, Stage Select, Character Select, How To Play, Confirm New Game.
public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject stageSelectPanel;
    [SerializeField] private GameObject characterSelectPanel;
    [SerializeField] private GameObject howToPlayPanel;
    [SerializeField] private GameObject newGameConfirmPanel;

    private void Start()
    {
        // Phòng trường hợp Scene trước đó (Pause/GameOver) rời đi khi Time.timeScale đang = 0
        Time.timeScale = 1f;
        ShowMainMenu();
    }

    private void HideAllPanels()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (stageSelectPanel != null) stageSelectPanel.SetActive(false);
        if (characterSelectPanel != null) characterSelectPanel.SetActive(false);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
        if (newGameConfirmPanel != null) newGameConfirmPanel.SetActive(false);
    }

    public void ShowMainMenu()
    {
        HideAllPanels();
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    public void ShowStageSelect()
    {
        HideAllPanels();
        if (stageSelectPanel != null) stageSelectPanel.SetActive(true);
    }

    // Gọi từ StageButton sau khi đã chọn 1 Level còn mở khóa
    public void ShowCharacterSelect()
    {
        HideAllPanels();
        if (characterSelectPanel != null) characterSelectPanel.SetActive(true);
    }

    // Gọi từ nút "How To Play" trên Main Menu
    public void ShowHowToPlay()
    {
        HideAllPanels();
        if (howToPlayPanel != null) howToPlayPanel.SetActive(true);
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
