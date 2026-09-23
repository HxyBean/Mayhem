using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Panel giới thiệu nhân vật, mở ĐÈ LÊN màn Chọn nhân vật khi bấm vào BẤT KỲ nhân vật nào (kể cả chưa mở khóa
// - phải xem được thông tin thì mới quyết định có mua hay không).
//
// Panel tự đổi mặt theo trạng thái khóa:
//  - Đã mở khóa  -> hiện nút Vào chơi, ẩn 2 nút thanh toán.
//  - Chưa mở khóa -> ẩn nút Vào chơi, hiện nút trả bằng Coin / Kim cương ngay tại đây.
// Mua xong panel KHÔNG đóng mà vẽ lại thành trạng thái đã mở khóa, để người chơi bấm Vào chơi luôn.
//
// Panel này gộp luôn vai trò của CharacterUnlockPanel cũ.
public class CharacterInfoPanel : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;

    [Header("Thông tin chung")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text nameText;
    [Tooltip("Ranged / Melee")]
    [SerializeField] private TMP_Text combatTypeText;

    [Header("Chỉ số (chỉ số GỐC của nhân vật, chưa cộng bonus mua ở Shop)")]
    [Tooltip("Có thể gán 1 text gộp cho cả 4 chỉ số (để trống 4 ô riêng), hoặc gán từng ô riêng - tùy bố cục bạn dựng")]
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text damageText;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private TMP_Text regenText;

    [Header("Chiêu & lõi")]
    [SerializeField] private TMP_Text abilityText;
    [Tooltip("Danh sách lõi ĐẶC BIỆT của nhân vật, tự sinh từ CharacterData.exclusiveAugments")]
    [SerializeField] private TMP_Text coresText;
    [Tooltip("CHỈ các augment có type nằm trong danh sách này mới được coi là 'lõi đặc biệt' và hiện ra.\n" +
             "exclusiveAugments còn chứa cả loại chỉ nâng chỉ số (Bullet, Reload, AttackSpeed, StaminaRegen...) - " +
             "liệt kê hết ra thì phần lõi dài lê thê và mất luôn ý nghĩa 'đặc biệt'.\n" +
             "Thêm nhân vật mới có lõi riêng thì thêm type của nó vào đây, không cần sửa code.")]
    [SerializeField]
    private string[] featuredCoreTypes = { "Bomb", "Potion", "SwordSpin", "MiniRobot" };

    [Header("Nút - đã mở khóa")]
    [Tooltip("GameObject bao ngoài nút Vào chơi, tự ẩn khi nhân vật chưa mở khóa")]
    [SerializeField] private GameObject playButtonObj;
    [SerializeField] private Button playButton;
    [SerializeField] private Button backButton;

    [Header("Nút - chưa mở khóa (mua ngay tại panel này)")]
    [Tooltip("Dòng báo lỗi khi không đủ tiền, có thể để trống")]
    [SerializeField] private TMP_Text messageText;
    [Tooltip("GameObject chứa nút trả bằng Coin - tự ẩn nếu nhân vật này không cho mua bằng Coin (giá <= 0)")]
    [SerializeField] private GameObject coinPayButtonObj;
    [SerializeField] private Button coinPayButton;
    [SerializeField] private TMP_Text coinPriceText;
    [SerializeField] private GameObject diamondPayButtonObj;
    [SerializeField] private Button diamondPayButton;
    [SerializeField] private TMP_Text diamondPriceText;

    [Header("Giá trị mặc định của Player trong Scene Level")]
    [Tooltip("CharacterData để baseMaxHP = 0 nghĩa là 'giữ nguyên giá trị trên Player trong Scene'. Panel này " +
             "không đọc được Scene Level nên phải khai lại ở đây cho khớp, nếu không nhân vật đó sẽ hiện HP = 0")]
    [SerializeField] private float fallbackMaxHP = 100f;
    [Tooltip("Tương tự baseMoveSpeed = 0 - phải khớp Move Speed đang set trên Player trong Scene Level")]
    [SerializeField] private float fallbackMoveSpeed = 5f;

    private CharacterData currentCharacter;
    private CharacterButton sourceButton;

    // Listener được nối TẠI ĐÂY bằng code. ĐỪNG gán thêm hàm vào OnClick của các nút này trong Inspector nữa,
    // nếu không mỗi cú bấm sẽ chạy 2 lần.
    private void Awake()
    {
        if (playButton != null) playButton.onClick.AddListener(PlaySelectedCharacter);
        if (backButton != null) backButton.onClick.AddListener(Close);
        if (coinPayButton != null) coinPayButton.onClick.AddListener(() => TryUnlock(payWithCoin: true));
        if (diamondPayButton != null) diamondPayButton.onClick.AddListener(() => TryUnlock(payWithCoin: false));

        if (panelRoot != null) panelRoot.SetActive(false);
    }

    // Gọi từ CharacterButton cho MỌI nhân vật, khóa hay chưa đều mở được panel này
    public void Show(CharacterData character, CharacterButton source)
    {
        if (character == null) return;

        currentCharacter = character;
        sourceButton = source;

        // Bật CẢ gameObject của script LẪN panel: 2 cái có thể là 2 object khác nhau, và bật object con KHÔNG
        // làm object cha sống lại (bài học từ NPCDialogueUI ở mục 13).
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (panelRoot != null) panelRoot.SetActive(true);

        if (messageText != null) messageText.text = "";

        Refresh();
    }

    public void Close()
    {
        currentCharacter = null;
        sourceButton = null;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void Refresh()
    {
        if (currentCharacter == null) return;

        FillInfo(currentCharacter);
        RefreshLockState();
    }

    // ==============================================
    // THÔNG TIN NHÂN VẬT
    // ==============================================
    private void FillInfo(CharacterData c)
    {
        if (portraitImage != null && c.selectIcon != null) portraitImage.sprite = c.selectIcon;
        if (nameText != null) nameText.text = c.characterName;
        if (combatTypeText != null) combatTypeText.text = (c.combatType == CombatType.Melee) ? "MELEE" : "RANGED";

        FillStats(c);

        if (abilityText != null) abilityText.text = BuildAbilityText(c);
        if (coresText != null) coresText.text = BuildCoresText(c);
    }

    // Hiện chỉ số GỐC của nhân vật, CỐ Ý không cộng bonus mua ở Shop: đây là bảng so sánh giữa các nhân vật với
    // nhau, mà bonus Shop thì áp cho mọi nhân vật như nhau nên cộng vào chỉ làm nhiễu phần khác biệt thật sự.
    private void FillStats(CharacterData c)
    {
        float hp = (c.baseMaxHP > 0f) ? c.baseMaxHP : fallbackMaxHP;
        float damage = c.baseBulletDamage;
        float speed = (c.baseMoveSpeed > 0f) ? c.baseMoveSpeed : fallbackMoveSpeed;
        float regen = c.baseRegen;

        if (hpText != null) hpText.text = Mathf.RoundToInt(hp).ToString();
        if (damageText != null) damageText.text = Mathf.RoundToInt(damage).ToString();
        if (speedText != null) speedText.text = speed.ToString("0.#");
        if (regenText != null) regenText.text = regen.ToString("0.#") + "/s";

        if (statsText != null)
        {
            statsText.text =
                $"HP: {Mathf.RoundToInt(hp)}\n" +
                $"DAMAGE: {Mathf.RoundToInt(damage)}\n" +
                $"SPEED: {speed:0.#}\n" +
                $"REGEN: {regen:0.#}/s";
        }
    }

    // Mô tả tự viết trong asset LUÔN THẮNG - đó mới là cách đúng để mô tả nhân vật mới.
    // Phần dưới chỉ là mặc định cho 4 nhân vật hiện có, để không phải điền tay mới chạy được.
    //
    // abilityType KHÔNG đủ để suy ra: nó chỉ có Dash/Blink/None, mà Knight lẫn Robot đều rơi vào None dù chiêu
    // của 2 đứa hoàn toàn khác nhau. Nên phải nhìn thêm combatType/usesAmmo - đúng những field mà Player.cs và
    // Gun.cs cũng đang dùng để phân biệt chúng.
    private string BuildAbilityText(CharacterData c)
    {
        if (!string.IsNullOrWhiteSpace(c.abilityDescription)) return c.abilityDescription;

        switch (c.abilityType)
        {
            case AbilityType.Dash:
                return "Dash - Lướt nhanh theo hướng đang di chuyển để né đòn.";

            case AbilityType.Blink:
                return "Blink - Dịch chuyển tức thời tới điểm đã chọn trong tầm ngắn.";
        }

        // Melee (Knight): giữ nút Khiên để vừa giảm sát thương nhận vào vừa chạy nhanh hơn
        if (c.combatType == CombatType.Melee)
        {
            return "Khiên - Giữ nút để tăng khả năng chống chịu và tăng tốc độ di chuyển, tiêu hao thể lực.";
        }

        // Ranged mà không dùng đạn (Robot): bắn thường tích năng lượng cho Laser
        if (!c.usesAmmo)
        {
            return "Laser - Bắn thường không tốn đạn và tích năng lượng; đủ số đòn thì bắn ra tia laser xuyên thấu.";
        }

        return "Không có kỹ năng di chuyển đặc biệt.";
    }

    // Tự sinh từ exclusiveAugments nên KHÔNG phải nhập lại danh sách lõi ở đâu cả, nhưng CHỈ lấy các type nằm
    // trong featuredCoreTypes: exclusiveAugments còn chứa cả đống augment chỉ nâng chỉ số (Bullet, Reload,
    // AttackSpeed, StaminaRegen, BlinkCooldown...) - liệt kê hết thì phần này dài lê thê và mất luôn ý nghĩa
    // "lõi đặc biệt".
    private string BuildCoresText(CharacterData c)
    {
        if (c.exclusiveAugments == null || c.exclusiveAugments.Count == 0) return "Không có lõi riêng.";

        StringBuilder sb = new StringBuilder();
        foreach (Augment augment in c.exclusiveAugments)
        {
            if (augment == null || !IsFeaturedCore(augment.type)) continue;

            sb.Append("• ").Append(augment.name);
            if (!string.IsNullOrWhiteSpace(augment.description)) sb.Append(": ").Append(augment.description);
            sb.AppendLine();
        }

        string result = sb.ToString().TrimEnd();
        return string.IsNullOrEmpty(result) ? "Không có lõi riêng." : result;
    }

    private bool IsFeaturedCore(string augmentType)
    {
        if (featuredCoreTypes == null || string.IsNullOrEmpty(augmentType)) return false;

        foreach (string type in featuredCoreTypes)
        {
            if (type == augmentType) return true;
        }
        return false;
    }

    // ==============================================
    // KHÓA / MỞ KHÓA
    // ==============================================
    private void RefreshLockState()
    {
        bool unlocked = GameProgress.IsCharacterUnlocked(currentCharacter);

        if (playButtonObj != null) playButtonObj.SetActive(unlocked);
        else if (playButton != null) playButton.gameObject.SetActive(unlocked);

        ShowPayOption(coinPayButtonObj, coinPayButton, coinPriceText,
                      !unlocked && currentCharacter.coinPrice > 0, currentCharacter.coinPrice, GameProgress.Coin);
        ShowPayOption(diamondPayButtonObj, diamondPayButton, diamondPriceText,
                      !unlocked && currentCharacter.diamondPrice > 0, currentCharacter.diamondPrice, GameProgress.Diamond);
    }

    private void ShowPayOption(GameObject group, Button button, TMP_Text priceText, bool visible, int price, int balance)
    {
        if (group != null) group.SetActive(visible);
        else if (button != null) button.gameObject.SetActive(visible);

        if (!visible) return;

        if (priceText != null) priceText.text = price.ToString();
        // Không đủ tiền thì làm mờ nút thay vì để bấm rồi mới báo lỗi
        if (button != null) button.interactable = balance >= price;
    }

    private void TryUnlock(bool payWithCoin)
    {
        if (currentCharacter == null) return;

        // Chặn mua 2 lần: bấm nhanh 2 nhịp trên mobile, hoặc lỡ gán thêm OnClick trong Inspector khiến listener
        // chạy đôi. Thiếu dòng này là người chơi bị trừ tiền lần thứ 2 cho nhân vật đã sở hữu.
        if (GameProgress.IsCharacterUnlocked(currentCharacter)) return;

        int price = payWithCoin ? currentCharacter.coinPrice : currentCharacter.diamondPrice;
        if (price <= 0) return;

        bool paid = payWithCoin ? GameProgress.TrySpendCoin(price) : GameProgress.TrySpendDiamond(price);
        if (!paid)
        {
            if (messageText != null) messageText.text = payWithCoin ? "Not enough Coin!" : "Not enough Diamond!";
            RefreshLockState();
            return;
        }

        GameProgress.UnlockCharacter(currentCharacter);

        // Cập nhật lại ô nhân vật phía sau (bỏ lớp phủ ổ khóa + ẩn giá) rồi vẽ lại chính panel này thành trạng
        // thái đã mở khóa. KHÔNG đóng panel: vừa mua xong thì bấm Vào chơi luôn là hợp lý nhất.
        if (sourceButton != null) sourceButton.Refresh();
        if (messageText != null) messageText.text = "";
        RefreshLockState();
    }

    private void PlaySelectedCharacter()
    {
        if (currentCharacter == null) return;

        // Lưới an toàn: nút Vào chơi đã bị ẩn khi chưa mở khóa, nhưng vẫn chặn lần nữa để không có đường nào
        // lọt vào màn chơi bằng nhân vật chưa mua.
        if (!GameProgress.IsCharacterUnlocked(currentCharacter)) return;

        if (GameProgress.SelectedStage == null || string.IsNullOrEmpty(GameProgress.SelectedStage.sceneName))
        {
            Debug.LogWarning("CharacterInfoPanel: chưa chọn Stage nào nên không biết load Scene nào.");
            return;
        }

        string sceneName = GameProgress.SelectedStage.sceneName;

        // SceneManager.LoadScene chỉ load được Scene đã nằm trong Build Settings. Thiếu ở đó thì nó KHÔNG ném
        // exception mà chỉ lặng lẽ không làm gì - nút Play bấm như không bấm, và trong bản build thì không có
        // Console để nhìn ra. Kiểm tra trước để ít nhất còn có dòng lỗi chỉ đúng chỗ cần sửa.
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"CharacterInfoPanel: Scene '{sceneName}' chưa được thêm vào Build Settings " +
                           "(File > Build Profiles/Build Settings > Scene List) nên không load được.");
            return;
        }

        GameProgress.SelectedCharacter = currentCharacter;
        SceneManager.LoadScene(sceneName);
    }
}
