using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System;

public class NPCDialogueUI : MonoBehaviour
{
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private TMP_Text speakerNameText;
    [Tooltip("Ô ảnh chân dung NPC. Sprite do chính NPC truyền vào lúc gọi ShowDialogue (mỗi NPC một ảnh riêng), " +
             "KHÔNG gán cứng ở đây - gán cứng thì mọi NPC đều dùng chung một mặt")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text actionButtonText;

    private string[] currentPages;
    private int currentPageIndex;
    private Action onActionCallback;

    private void Awake()
    {
        if (nextButton != null) nextButton.onClick.AddListener(OnNextPage);
        if (exitButton != null) exitButton.onClick.AddListener(OnExit);
        if (actionButton != null) actionButton.onClick.AddListener(OnAction);

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }

    // portrait để cuối và có giá trị mặc định, nên các chỗ gọi cũ (NPC chưa có ảnh riêng) vẫn biên dịch bình thường.
    public void ShowDialogue(string speakerName, string[] pages, string actionLabel, Action onAction, Sprite portrait = null)
    {
        if (pages == null || pages.Length == 0) return;

        speakerNameText.text = speakerName;
        ApplyPortrait(portrait);
        currentPages = pages;
        currentPageIndex = 0;
        onActionCallback = onAction;

        if (actionButtonText != null)
        {
            actionButtonText.text = actionLabel;
        }

        // PHẢI bật mọi thứ NGAY TẠI ĐÂY, TRƯỚC khi StartCoroutine - Unity không cho chạy coroutine trên
        // component nằm ở GameObject đang tắt.
        // Bật CẢ HAI: dialoguePanel (thứ người chơi nhìn thấy) LẪN chính gameObject gắn script này. Hai cái này
        // có thể là 2 object khác nhau - nếu dialoguePanel là object CON mà object cha gắn script đang tắt thì
        // bật con KHÔNG làm cha sống lại, coroutine vẫn ném lỗi.
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (dialoguePanel != null) dialoguePanel.SetActive(true);

        Time.timeScale = 0f;

        UpdateDialogueUI();

        if (panelCanvasGroup == null) return;

        // Còn tắt nghĩa là có object CHA nào đó đang tắt - tự bật mình không cứu được. Thà bỏ hiệu ứng fade
        // (hiện thẳng) còn hơn ném lỗi rồi hội thoại không mở ra được.
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(FadeInRoutine());
            return;
        }

        panelCanvasGroup.alpha = 1f;
        panelCanvasGroup.interactable = true;
        panelCanvasGroup.blocksRaycasts = true;
    }

    private void ApplyPortrait(Sprite portrait)
    {
        if (portraitImage == null) return;

        portraitImage.sprite = portrait;

        // Ẩn HẲN ô ảnh khi NPC này không có ảnh riêng. Chỉ gán sprite = null mà để nguyên object thì Image vẫn
        // vẽ ra một ô trắng đặc; tệ hơn là nếu quên bước này, người chơi sẽ thấy lại mặt của NPC vừa nói chuyện
        // lần trước vì cửa sổ hội thoại dùng chung cho mọi NPC.
        portraitImage.gameObject.SetActive(portrait != null);
    }

    private IEnumerator FadeInRoutine()
    {
        // Chặn bấm trong lúc đang fade để không lỡ tay bấm xuyên qua panel còn mờ
        panelCanvasGroup.alpha = 0f;
        panelCanvasGroup.interactable = false;
        panelCanvasGroup.blocksRaycasts = false;

        float fadeDuration = 0.5f;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime; // timeScale đang = 0 nên bắt buộc dùng unscaled
            panelCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        panelCanvasGroup.alpha = 1f;
        panelCanvasGroup.interactable = true;
        panelCanvasGroup.blocksRaycasts = true;
    }

    private void UpdateDialogueUI()
    {
        dialogueText.text = currentPages[currentPageIndex];

        bool isLastPage = (currentPageIndex == currentPages.Length - 1);
        
        if (nextButton != null) nextButton.gameObject.SetActive(!isLastPage);
        if (actionButton != null) actionButton.gameObject.SetActive(isLastPage);
    }

    private void OnNextPage()
    {
        if (currentPageIndex < currentPages.Length - 1)
        {
            currentPageIndex++;
            UpdateDialogueUI();
        }
    }

    private void OnExit()
    {
        CloseDialogue();
    }

    private void OnAction()
    {
        // ĐÓNG TRƯỚC, GỌI CALLBACK SAU. CloseDialogue() set Time.timeScale = 1f, nên nếu gọi callback trước thì
        // mọi màn hình do callback mở ra mà cần dừng game (VD shop của NPCComputer set timeScale = 0) sẽ bị
        // dòng đó ghi đè lại thành 1 ngay sau đấy -> game vẫn chạy trong lúc đang mở shop.
        // Giữ lại tham chiếu callback trước khi đóng cho chắc, phòng khi sau này CloseDialogue() có dọn field.
        Action callback = onActionCallback;
        CloseDialogue();
        callback?.Invoke();
    }

    private void CloseDialogue()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        Time.timeScale = 1f;
    }
}

