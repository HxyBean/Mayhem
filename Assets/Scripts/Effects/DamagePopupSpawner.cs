using UnityEngine;

// Nơi DUY NHẤT giữ prefab con số sát thương. Đặt 1 GameObject mang script này trong mỗi Scene Level.
//
// Tách ra thành singleton thay vì cho mỗi Enemy/Player tự giữ prefab: nếu để field trên từng prefab quái thì
// thêm 1 loại quái mới lại phải nhớ kéo prefab vào, quên là con đó im lặng không hiện số - loại lỗi rất khó
// nhận ra. Ở đây quên gán thì KHÔNG con nào hiện số, sai là thấy ngay.
public class DamagePopupSpawner : MonoBehaviour
{
    public static DamagePopupSpawner Instance { get; private set; }

    [Tooltip("Prefab con số bay lên (có script DamagePopup). Để trống = tắt hẳn tính năng này")]
    [SerializeField] private GameObject popupPrefab;
    [Tooltip("Số hiện lệch ngẫu nhiên trong bán kính này quanh mục tiêu, để nhiều con số trúng liên tiếp " +
             "không chồng khít lên nhau thành một cục không đọc được")]
    [SerializeField] private float spawnRadius = 0.5f;

    [Header("Màu")]
    [Tooltip("Màu số khi ENEMY ăn damage (người chơi đang gây sát thương)")]
    [SerializeField] private Color enemyDamageColor = Color.white;
    [Tooltip("Màu số khi PLAYER ăn damage - nên để đỏ cho khác hẳn, đây là thông tin quan trọng hơn nhiều")]
    [SerializeField] private Color playerDamageColor = new Color(1f, 0.35f, 0.35f, 1f);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void SpawnEnemyDamage(Vector3 worldPosition, float amount) => Spawn(worldPosition, amount, enemyDamageColor);
    public void SpawnPlayerDamage(Vector3 worldPosition, float amount) => Spawn(worldPosition, amount, playerDamageColor);

    private void Spawn(Vector3 worldPosition, float amount, Color color)
    {
        if (popupPrefab == null || amount <= 0f) return;

        Vector2 offset = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPos = worldPosition + new Vector3(offset.x, offset.y, 0f);

        GameObject obj = ObjectPoolManager.Instance != null
            ? ObjectPoolManager.Instance.SpawnObject(popupPrefab, spawnPos, Quaternion.identity)
            : Instantiate(popupPrefab, spawnPos, Quaternion.identity);

        // Tìm cả ở object con: script đặt nhầm vào con thay vì gốc là Setup() không bao giờ chạy, con số giữ
        // nguyên nội dung + màu gõ sẵn trong prefab (thường là đen) và trông y như code set màu sai.
        DamagePopup popup = obj.GetComponent<DamagePopup>();
        if (popup == null) popup = obj.GetComponentInChildren<DamagePopup>(true);

        if (popup == null)
        {
            Debug.LogWarning($"DamagePopupSpawner: prefab '{popupPrefab.name}' thiếu component DamagePopup nên " +
                             "con số không được set nội dung/màu.", popupPrefab);
            return;
        }

        popup.Setup(amount, color);
    }
}
