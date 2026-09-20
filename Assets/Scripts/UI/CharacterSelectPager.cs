using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Chia nhân vật thành nhiều trang cho màn Chọn nhân vật, lật bằng 2 nút < > (không dùng cơ chế vuốt).
// ĐẶT SCRIPT NÀY LÊN CHÍNH Character Select Panel (object luôn bật khi đang ở màn chọn nhân vật) để OnEnable
// chạy được mỗi lần mở màn - giống CharacterUnlockPanel.
//
// Cách hoạt động: các ô nút (slots) được dựng sẵn 1 lần và DÙNG CHUNG cho mọi nhân vật; mỗi lần lật trang,
// pager nạp lại CharacterData tương ứng vào từng ô. Nhờ vậy thêm nhân vật mới chỉ cần thêm 1 phần tử vào mảng
// All Characters - KHÔNG phải dựng thêm nút hay thêm trang nào trong Editor.
public class CharacterSelectPager : MonoBehaviour
{
    [Tooltip("Toàn bộ nhân vật của game, xếp theo đúng thứ tự muốn hiển thị (trái→phải, trang 1 trước)")]
    [SerializeField] private CharacterData[] allCharacters;

    [Tooltip("Các ô nhân vật dựng sẵn trên MỘT trang (VD 3 ô). Số ô ở đây chính là số nhân vật mỗi trang")]
    [SerializeField] private CharacterButton[] slots;

    [Header("Điều hướng trang")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [Tooltip("Text số trang dạng \"1/2\", có thể để trống")]
    [SerializeField] private TMP_Text pageText;

    private int currentPage = 0;

    private int PageCount
    {
        get
        {
            if (slots == null || slots.Length == 0) return 1;
            if (allCharacters == null || allCharacters.Length == 0) return 1;

            return Mathf.CeilToInt((float)allCharacters.Length / slots.Length);
        }
    }

    private void Awake()
    {
        if (prevButton != null) prevButton.onClick.AddListener(ShowPreviousPage);
        if (nextButton != null) nextButton.onClick.AddListener(ShowNextPage);
    }

    // Giữ nguyên trang đang xem giữa các lần mở màn chọn nhân vật, nhưng vẫn vẽ lại để cập nhật trạng thái
    // khóa/giá (người chơi có thể vừa mua nhân vật ở Shop xong)
    private void OnEnable()
    {
        ShowPage(currentPage);
    }

    public void ShowPreviousPage()
    {
        ShowPage(currentPage - 1);
    }

    public void ShowNextPage()
    {
        ShowPage(currentPage + 1);
    }

    private void ShowPage(int page)
    {
        if (slots == null || slots.Length == 0) return;

        int pageCount = PageCount;
        currentPage = Mathf.Clamp(page, 0, pageCount - 1);

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;

            int characterIndex = currentPage * slots.Length + i;
            bool hasCharacter = allCharacters != null && characterIndex < allCharacters.Length;

            // Trang cuối có thể thừa ô (VD 4 nhân vật / 3 ô mỗi trang) - ẩn hẳn ô thừa đi
            slots[i].gameObject.SetActive(hasCharacter);
            if (hasCharacter) slots[i].SetCharacter(allCharacters[characterIndex]);
        }

        RefreshNavigation(pageCount);
    }

    private void RefreshNavigation(int pageCount)
    {
        // Chỉ đủ 1 trang thì ẩn hẳn cụm điều hướng cho gọn, thay vì để 2 nút mờ tịt nhìn rất thừa
        bool multiPage = pageCount > 1;

        if (pageText != null)
        {
            pageText.gameObject.SetActive(multiPage);
            pageText.text = (currentPage + 1) + "/" + pageCount;
        }

        // Không cho lật vòng: ở trang đầu thì mờ nút <, ở trang cuối thì mờ nút >
        if (prevButton != null)
        {
            prevButton.gameObject.SetActive(multiPage);
            prevButton.interactable = currentPage > 0;
        }

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(multiPage);
            nextButton.interactable = currentPage < pageCount - 1;
        }
    }
}
