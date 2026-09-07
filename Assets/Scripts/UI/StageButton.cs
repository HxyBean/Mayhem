using UnityEngine;
using UnityEngine.UI;

// Gắn vào mỗi nút Level trong panel Stage Select (Scene MainMenu).
// Tự khóa nút nếu Stage chưa mở khóa, bấm vào thì lưu Stage đã chọn rồi mở panel Character Select.
public class StageButton : MonoBehaviour
{
    [SerializeField] private StageData stageData;
    [SerializeField] private GameObject lockedOverlay; // Icon ổ khóa hiển thị khi Stage chưa mở, có thể để trống
    [SerializeField] private MainMenuUI mainMenuUI;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(OnClick);
    }

    private void OnEnable()
    {
        RefreshLockState();
    }

    private void RefreshLockState()
    {
        bool unlocked = stageData != null && GameProgress.IsStageUnlocked(stageData.stageIndex);

        if (button != null) button.interactable = unlocked;
        if (lockedOverlay != null) lockedOverlay.SetActive(!unlocked);
    }

    private void OnClick()
    {
        if (stageData == null) return;
        if (!GameProgress.IsStageUnlocked(stageData.stageIndex)) return;

        GameProgress.SelectedStage = stageData;
        if (mainMenuUI != null) mainMenuUI.ShowCharacterSelect();
    }
}
