using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Gắn vào mỗi Ô Level trong panel Stage Select (Scene MainMenu).
// Tự khóa nút nếu Stage chưa mở khóa, bấm vào thì lưu Stage đã chọn rồi mở panel Character Select.
// Ô này KHÔNG gắn cứng với 1 Level: StageSelectPager nạp lại dữ liệu cho từng ô mỗi khi lật trang, nên chỉ cần
// dựng sẵn vài ô là hiển thị được bao nhiêu Level cũng đủ.
public class StageButton : MonoBehaviour
{
    [Tooltip("Level mặc định của ô này. Khi dùng StageSelectPager thì giá trị ở đây sẽ bị ghi đè lúc lật trang")]
    [SerializeField] private StageData stageData;

    [Header("Hiển thị theo Level")]
    [Tooltip("Ảnh thu nhỏ - tự đổi theo StageData.previewImage mỗi khi ô được nạp Level mới")]
    [SerializeField] private Image previewImage;
    [Tooltip("Tên Level - tự điền theo StageData.stageName. Có thể để trống")]
    [SerializeField] private TMP_Text nameText;
    [Tooltip("Icon ổ khóa hiển thị khi Stage chưa mở, có thể để trống")]
    [SerializeField] private GameObject lockedOverlay;

    [SerializeField] private MainMenuUI mainMenuUI;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(OnClick);
    }

    private void OnEnable()
    {
        Refresh();
    }

    // Gọi từ StageSelectPager khi lật trang - nạp Level khác vào chính ô này
    public void SetStage(StageData stage)
    {
        stageData = stage;
        Refresh();
    }

    public void Refresh()
    {
        bool unlocked = stageData != null && GameProgress.IsStageUnlocked(stageData.stageIndex);

        if (nameText != null) nameText.text = (stageData != null) ? stageData.stageName : "";
        if (previewImage != null && stageData != null && stageData.previewImage != null)
        {
            previewImage.sprite = stageData.previewImage;
        }

        // KHÁC với CharacterButton: Level chưa mở khóa thì bấm cũng không được (không có cách nào mua để mở),
        // nên khóa luôn nút thay vì chỉ hiện ổ khóa.
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
