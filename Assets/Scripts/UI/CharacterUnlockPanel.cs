using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Modal xác nhận mở khóa nhân vật + chọn đơn vị thanh toán (Coin hoặc Kim cương).
// ĐẶT SCRIPT NÀY LÊN CHÍNH Character Select Panel (object luôn bật khi đang ở màn chọn nhân vật), còn
// panelRoot trỏ tới GameObject modal con bên trong - nhờ vậy modal luôn bắt đầu ở trạng thái đóng mỗi lần
// mở lại màn chọn nhân vật (script nằm trên object đang tắt sẽ không chạy được OnEnable để tự đóng).
public class CharacterUnlockPanel : MonoBehaviour
{
    [Tooltip("GameObject của modal xác nhận (con của Character Select Panel) - script sẽ tự bật/tắt object này")]
    [SerializeField] private GameObject panelRoot;
    [Tooltip("Dòng tiêu đề, VD \"Unlock Mage?\"")]
    [SerializeField] private TMP_Text titleText;
    [Tooltip("Dòng thông báo lỗi khi không đủ tiền, có thể để trống")]
    [SerializeField] private TMP_Text messageText;

    [Header("Thanh toán bằng Coin")]
    [Tooltip("GameObject chứa nút trả bằng Coin - tự ẩn nếu nhân vật này không cho mua bằng Coin (giá <= 0)")]
    [SerializeField] private GameObject coinPayButtonObj;
    [SerializeField] private Button coinPayButton;
    [SerializeField] private TMP_Text coinPriceText;

    [Header("Thanh toán bằng Kim cương")]
    [SerializeField] private GameObject diamondPayButtonObj;
    [SerializeField] private Button diamondPayButton;
    [SerializeField] private TMP_Text diamondPriceText;

    private CharacterData pendingCharacter;
    private CharacterButton sourceButton;

    private void OnEnable()
    {
        Hide();
    }

    // Gọi từ CharacterButton khi bấm vào 1 nhân vật đang bị khóa
    public void Show(CharacterData character, CharacterButton source)
    {
        pendingCharacter = character;
        sourceButton = source;

        if (character == null) return;

        if (panelRoot != null) panelRoot.SetActive(true);
        if (titleText != null) titleText.text = "Unlock " + character.characterName + "?";
        if (messageText != null) messageText.text = "";

        RefreshPayOptions();
    }

    // Gọi từ nút "No/Cancel" trên modal
    public void Hide()
    {
        pendingCharacter = null;
        sourceButton = null;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    // Gán 2 hàm này vào OnClick của 2 nút thanh toán tương ứng
    public void UnlockWithCoin()
    {
        TryUnlock(true);
    }

    public void UnlockWithDiamond()
    {
        TryUnlock(false);
    }

    private void RefreshPayOptions()
    {
        if (pendingCharacter == null) return;

        bool canPayByCoin = pendingCharacter.coinPrice > 0;
        if (coinPayButtonObj != null) coinPayButtonObj.SetActive(canPayByCoin);
        if (canPayByCoin)
        {
            if (coinPriceText != null) coinPriceText.text = pendingCharacter.coinPrice.ToString();
            // Không đủ tiền thì làm mờ nút thay vì để bấm rồi mới báo lỗi
            if (coinPayButton != null) coinPayButton.interactable = GameProgress.Coin >= pendingCharacter.coinPrice;
        }

        bool canPayByDiamond = pendingCharacter.diamondPrice > 0;
        if (diamondPayButtonObj != null) diamondPayButtonObj.SetActive(canPayByDiamond);
        if (canPayByDiamond)
        {
            if (diamondPriceText != null) diamondPriceText.text = pendingCharacter.diamondPrice.ToString();
            if (diamondPayButton != null) diamondPayButton.interactable = GameProgress.Diamond >= pendingCharacter.diamondPrice;
        }
    }

    private void TryUnlock(bool payWithCoin)
    {
        if (pendingCharacter == null) return;

        int price = payWithCoin ? pendingCharacter.coinPrice : pendingCharacter.diamondPrice;
        if (price <= 0) return;

        bool paid = payWithCoin ? GameProgress.TrySpendCoin(price) : GameProgress.TrySpendDiamond(price);
        if (!paid)
        {
            if (messageText != null) messageText.text = payWithCoin ? "Not enough Coin!" : "Not enough Diamond!";
            RefreshPayOptions();
            return;
        }

        GameProgress.UnlockCharacter(pendingCharacter);
        if (sourceButton != null) sourceButton.RefreshLockState();
        Hide();
    }
}
