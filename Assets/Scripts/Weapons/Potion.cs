using UnityEngine;

// Viên thuốc bay tới vị trí đích (giống cơ chế bay của Bomb), đáp xuống thì sinh ra PotionZone rồi tự dọn dẹp.
public class Potion : MonoBehaviour
{
    [Header("Potion Settings")]
    [SerializeField] private float moveSpeed = 15f;
    [SerializeField] private GameObject potionZonePrefab; // Prefab vùng độc/thuốc gây damage theo thời gian

    private Vector3 targetPosition;
    private bool hasLanded = false;

    private void OnEnable()
    {
        hasLanded = false;
    }

    /// <summary>
    /// Gọi hàm này ngay sau khi Spawn viên thuốc để thiết lập đích đến.
    /// </summary>
    public void SetTarget(Vector3 target)
    {
        targetPosition = target;
        hasLanded = false;
    }

    void Update()
    {
        if (hasLanded) return;

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            Land();
        }
    }

    private void Land()
    {
        if (hasLanded) return;
        hasLanded = true;

        if (potionZonePrefab != null)
        {
            if (ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.SpawnObject(potionZonePrefab, transform.position, Quaternion.identity);
            }
            else
            {
                Instantiate(potionZonePrefab, transform.position, Quaternion.identity);
            }
        }

        // Trả viên thuốc về Pool (vùng damage là 1 object riêng, đã tách ra ở trên)
        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnObjectToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Vẽ tầm bay/điểm đến trong Editor để debug
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(transform.position, targetPosition);
    }
}
