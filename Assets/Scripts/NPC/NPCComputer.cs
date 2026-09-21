using UnityEngine;

// NPC trạm dừng nghỉ: nói vài câu rồi mở màn hình giao dịch mã độc.
// Khác NPC.cs (có nhiệm vụ + chế độ đồng hành), NPC này không có trạng thái gì - lúc nào cũng mua bán được.
//
// Danh sách hàng bán nằm ở ĐÂY chứ không nằm trong MalwareShopUI: nhờ vậy đặt được nhiều trạm bán các món khác
// nhau trong cùng 1 màn (hoặc mỗi Level bán 1 kiểu) mà vẫn dùng chung đúng 1 màn hình shop.
public class NPCComputer : InteractableNPC
{
    [Header("References")]
    [SerializeField] private NPCDialogueUI dialogueUI;
    [SerializeField] private MalwareShopUI shopUI;

    [Header("Hội thoại")]
    [SerializeField] private string npcName = "Rest Stop Terminal";
    [SerializeField] private string actionLabel = "Browse";
    [TextArea(2, 5)]
    [SerializeField] private string[] greetingPages = new string[] {
        "Welcome to the rest stop, survivor. Rough night out there, huh?",
        "I deal in malware - nasty little programs. Some make you stronger, some make them weaker.",
        "Coins or USB drives, I take both. Depends on the code."
    };

    [Header("Hàng bán")]
    [Tooltip("Các mã độc trạm này bán. Thêm món mới = thêm 1 phần tử ở đây, không phải sửa code hay dựng thêm nút")]
    [SerializeField] private MalwareData[] stock;

    // Từ lần 2 trở đi thì bỏ qua đoạn chào hỏi dài dòng, mở thẳng shop - nghe lại 3 trang thoại mỗi lần ghé mua
    // rất phiền, nhất là khi giữa trận đang bị rượt.
    private bool hasGreeted = false;

    private void Update()
    {
        UpdateInteractionButton();
    }

    public override void OnInteract()
    {
        if (hasGreeted)
        {
            OpenShop();
            return;
        }

        hasGreeted = true;

        if (dialogueUI == null)
        {
            OpenShop();
            return;
        }

        // NPCDialogueUI.OnAction() đóng hội thoại TRƯỚC rồi mới gọi callback này, nên Time.timeScale = 0 do
        // OpenShop() set sẽ không bị CloseDialogue() ghi đè lại thành 1.
        dialogueUI.ShowDialogue(npcName, greetingPages, actionLabel, OpenShop, portrait);
    }

    private void OpenShop()
    {
        if (shopUI == null)
        {
            Debug.LogWarning($"{name}: chưa gán Shop UI nên không mở được màn hình giao dịch.");
            return;
        }

        shopUI.Open(npcName, stock);
    }
}
