using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Chia Level thành nhiều trang cho màn Chọn Level, lật bằng 2 nút < > (không dùng cơ chế vuốt).
// ĐẶT SCRIPT NÀY LÊN CHÍNH Stage Select Panel để OnEnable chạy được mỗi lần mở màn.
//
// Cách hoạt động giống hệt CharacterSelectPager: các ô nút (slots) được dựng sẵn 1 lần và DÙNG CHUNG cho mọi
// Level; mỗi lần lật trang, pager nạp lại StageData tương ứng vào từng ô. Thêm Level mới chỉ cần thêm 1 phần tử
// vào mảng All Stages - KHÔNG phải dựng thêm nút hay thêm trang nào trong Editor.
public class StageSelectPager : MonoBehaviour
{
    [Tooltip("Toàn bộ Level của game, xếp theo đúng thứ tự muốn hiển thị (Level 1 trước)")]
    [SerializeField] private StageData[] allStages;

    [Tooltip("Các ô Level dựng sẵn trên MỘT trang (VD 3 ô). Số ô ở đây chính là số Level mỗi trang")]
    [SerializeField] private StageButton[] slots;

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
            if (allStages == null || allStages.Length == 0) return 1;

            return Mathf.CeilToInt((float)allStages.Length / slots.Length);
        }
    }

    private void Awake()
    {
        if (prevButton != null) prevButton.onClick.AddListener(ShowPreviousPage);
        if (nextButton != null) nextButton.onClick.AddListener(ShowNextPage);
    }

    // Giữ nguyên trang đang xem giữa các lần mở màn chọn Level, nhưng vẫn vẽ lại để cập nhật trạng thái khóa
    // (người chơi có thể vừa phá đảo Level trước đó xong)
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

            int stageIndex = currentPage * slots.Length + i;
            bool hasStage = allStages != null && stageIndex < allStages.Length;

            // Trang cuối có thể thừa ô (VD 4 Level / 3 ô mỗi trang) - ẩn hẳn ô thừa đi
            slots[i].gameObject.SetActive(hasStage);
            if (hasStage) slots[i].SetStage(allStages[stageIndex]);
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
