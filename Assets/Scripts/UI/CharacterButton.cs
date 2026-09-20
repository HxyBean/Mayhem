using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Gắn vào mỗi Ô nhân vật trong panel Character Select (Scene MainMenu).
// - Nhân vật ĐÃ mở khóa: bấm vào là lưu nhân vật đã chọn rồi load đúng Scene của Stage đã chọn trước đó.
// - Nhân vật CHƯA mở khóa: bấm vào mở modal xác nhận mua (CharacterUnlockPanel), không vào game.
// Ô này KHÔNG gắn cứng với 1 nhân vật: CharacterSelectPager nạp lại dữ liệu cho từng ô mỗi khi lật trang, nên
// chỉ cần dựng sẵn vài ô là hiển thị được bao nhiêu nhân vật cũng đủ.
public class CharacterButton : MonoBehaviour
{
    [Tooltip("Nhân vật mặc định của ô này. Khi dùng CharacterSelectPager thì giá trị ở đây sẽ bị ghi đè lúc lật trang")]
    [SerializeField] private CharacterData characterData;

    [Header("Hiển thị theo nhân vật")]
    [Tooltip("Ảnh chân dung - tự đổi theo CharacterData.selectIcon mỗi khi ô được nạp nhân vật mới")]
    [SerializeField] private Image portraitImage;
    [Tooltip("Tên nhân vật - tự điền theo CharacterData.characterName. Có thể để trống")]
    [SerializeField] private TMP_Text nameText;

    [Header("Trạng thái khóa")]
    [Tooltip("Lớp phủ ổ khóa hiển thị khi nhân vật chưa mở khóa, có thể để trống")]
    [SerializeField] private GameObject lockedOverlay;
    [Tooltip("Text hiển thị giá Coin ngay trên nút, tự ẩn khi đã mở khóa. Có thể để trống")]
    [SerializeField] private TMP_Text coinPriceText;
    [Tooltip("Text hiển thị giá Kim cương ngay trên nút, tự ẩn khi đã mở khóa. Có thể để trống")]
    [SerializeField] private TMP_Text diamondPriceText;
    [Tooltip("Modal xác nhận mở khóa - kéo GameObject có script CharacterUnlockPanel (chính Character Select Panel) vào đây")]
    [SerializeField] private CharacterUnlockPanel unlockPanel;

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

    // Gọi từ CharacterSelectPager khi lật trang - nạp nhân vật khác vào chính ô này
    public void SetCharacter(CharacterData character)
    {
        characterData = character;
        Refresh();
    }

    // Public để CharacterUnlockPanel gọi lại ngay sau khi mua thành công, cập nhật ô không cần đóng/mở panel
    public void Refresh()
    {
        bool unlocked = GameProgress.IsCharacterUnlocked(characterData);

        if (nameText != null) nameText.text = (characterData != null) ? characterData.characterName : "";
        if (portraitImage != null && characterData != null && characterData.selectIcon != null)
        {
            portraitImage.sprite = characterData.selectIcon;
        }

        // KHÁC với StageButton: nút vẫn bấm được khi đang khóa, vì bấm vào chính là để mở modal mua
        if (lockedOverlay != null) lockedOverlay.SetActive(!unlocked);

        ShowPriceTag(coinPriceText, !unlocked && characterData != null && characterData.coinPrice > 0,
                     characterData != null ? characterData.coinPrice : 0);
        ShowPriceTag(diamondPriceText, !unlocked && characterData != null && characterData.diamondPrice > 0,
                     characterData != null ? characterData.diamondPrice : 0);
    }

    private void ShowPriceTag(TMP_Text text, bool visible, int price)
    {
        if (text == null) return;

        text.gameObject.SetActive(visible);
        if (visible) text.text = price.ToString();
    }

    private void OnClick()
    {
        if (characterData == null) return;

        // Chưa mở khóa -> mở modal xác nhận mua, KHÔNG cho vào game
        if (!GameProgress.IsCharacterUnlocked(characterData))
        {
            if (unlockPanel != null) unlockPanel.Show(characterData, this);
            return;
        }

        if (GameProgress.SelectedStage == null || string.IsNullOrEmpty(GameProgress.SelectedStage.sceneName)) return;

        GameProgress.SelectedCharacter = characterData;
        SceneManager.LoadScene(GameProgress.SelectedStage.sceneName);
    }
}
