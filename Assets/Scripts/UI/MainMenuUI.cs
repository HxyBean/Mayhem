using UnityEngine;

// Gắn vào 1 GameObject trong Scene MainMenu, điều khiển các panel: Main Menu, Stage Select, Character Select, How To Play, Confirm New Game.
public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject stageSelectPanel;
    [SerializeField] private GameObject characterSelectPanel;
    [SerializeField] private GameObject howToPlayPanel;
    [SerializeField] private GameObject newGameConfirmPanel;
    [Tooltip("Panel Shop Power Up (mua chỉ số nội tại bằng Coin + đổi Coin sang Kim cương) - mở đè lên từ trong " +
             "panel Chọn nhân vật, không thay thế panel phía sau")]
    [SerializeField] private GameObject shopPanel;

    [Tooltip("Pager của màn chọn Level - dùng để tra Level kế tiếp cho nút 'Màn tiếp theo' ở màn hình Thắng. " +
             "PHẢI gán tay: panel đang tắt nên FindFirstObjectByType mặc định không tìm thấy")]
    [SerializeField] private StageSelectPager stageSelectPager;

    private void Start()
    {
        // Phòng trường hợp Scene trước đó (Pause/GameOver) rời đi khi Time.timeScale đang = 0
        Time.timeScale = 1f;

        OpenPendingPanel();
    }

    // Từ trong màn chơi quay về đây, các nút Thua/Thắng đặt cờ GameProgress.PendingPanel để nói rõ muốn mở
    // panel nào thay vì luôn rơi về Main Menu bắt người chơi bấm lại từ đầu.
    private void OpenPendingPanel()
    {
        GameProgress.MenuPanel pending = GameProgress.ConsumePendingPanel();

        switch (pending)
        {
            case GameProgress.MenuPanel.StageSelect:
                ShowStageSelect();
                return;

            case GameProgress.MenuPanel.CharacterSelect:
                if (GameProgress.AdvanceToNextStage)
                {
                    GameProgress.AdvanceToNextStage = false;

                    // Không tìm được Level kế tiếp (vừa phá đảo Level cuối) thì rơi về màn chọn Level, chứ
                    // KHÔNG mở màn chọn nhân vật với Stage cũ - làm vậy là người chơi bấm Vào chơi rồi chơi lại
                    // đúng màn vừa thắng mà tưởng đang sang màn mới.
                    if (!TryAdvanceToNextStage())
                    {
                        ShowStageSelect();
                        return;
                    }
                }

                if (GameProgress.SelectedStage == null)
                {
                    ShowStageSelect();
                    return;
                }

                ShowCharacterSelect();
                return;

            default:
                ShowMainMenu();
                return;
        }
    }

    private bool TryAdvanceToNextStage()
    {
        if (stageSelectPager == null || GameProgress.SelectedStage == null) return false;

        StageData next = stageSelectPager.GetStageByIndex(GameProgress.SelectedStage.stageIndex + 1);
        if (next == null) return false;

        GameProgress.SelectedStage = next;
        return true;
    }

    private void HideAllPanels()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (stageSelectPanel != null) stageSelectPanel.SetActive(false);
        if (characterSelectPanel != null) characterSelectPanel.SetActive(false);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
        if (newGameConfirmPanel != null) newGameConfirmPanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);
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

    // Gọi từ nút "Shop" trong panel Chọn nhân vật. Mở ĐÈ LÊN (không gọi HideAllPanels) nên đóng Shop là thấy
    // lại ngay màn chọn nhân vật, không cần điều hướng qua lại - cùng kiểu với modal newGameConfirmPanel.
    public void ShowShop()
    {
        if (shopPanel != null) shopPanel.SetActive(true);
    }

    // Gọi từ nút đóng/Back của Shop
    public void CloseShop()
    {
        if (shopPanel != null) shopPanel.SetActive(false);
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
