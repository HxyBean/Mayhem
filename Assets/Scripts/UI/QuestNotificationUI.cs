using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class QuestNotificationUI : MonoBehaviour
{
    public static QuestNotificationUI Instance { get; private set; }

    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private TMP_Text notificationText;
    [SerializeField] private float displayDuration = 3f;

    private Coroutine currentCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (notificationPanel != null)
        {
            notificationPanel.SetActive(false);
        }
    }

    public void ShowNotification(string message)
    {
        if (notificationPanel == null || notificationText == null) return;

        notificationText.text = message;

        // PHẢI bật panel TRƯỚC khi StartCoroutine - cùng lý do với NPCDialogueUI: nếu notificationPanel chính là
        // GameObject đang gắn script này (Awake() tự tắt mình) thì Unity không cho khởi động coroutine, mà lệnh
        // bật panel lại nằm bên trong chính coroutine đó nên không bao giờ chạy tới.
        // Bật CẢ object gắn script lẫn panel: 2 cái có thể là 2 object khác nhau, mà coroutine chỉ chạy được
        // khi object gắn script đang bật (xem chi tiết ở NPCDialogueUI.ShowDialogue).
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        notificationPanel.SetActive(true);

        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }

        // Còn tắt nghĩa là object CHA đang tắt - banner cũng chẳng hiện được, bỏ qua để không ném lỗi
        if (!gameObject.activeInHierarchy) return;

        currentCoroutine = StartCoroutine(HideAfterDelayRoutine());
    }

    private IEnumerator HideAfterDelayRoutine()
    {
        // Realtime chứ không phải WaitForSeconds: thông báo có thể còn đang hiện lúc game bị dừng (mở hội thoại
        // NPC hay màn chọn augment đều set timeScale = 0), lúc đó WaitForSeconds sẽ đứng im và banner treo lại
        // trên màn hình cho tới khi chơi tiếp.
        yield return new WaitForSecondsRealtime(displayDuration);

        notificationPanel.SetActive(false);
        currentCoroutine = null;
    }
}

