using UnityEngine;

// Quái rơi vật phẩm USB. KHÔNG nằm trong danh sách spawn ngẫu nhiên của EnemySpawner - chỉ xuất hiện sau mỗi
// killsPerUsbEnemy con quái bị hạ (xem EnemySpawner.OnEnemyKilled).
//
// Chiêu Lướt nằm ở component EnemyDashSkill (gắn cùng GameObject, bật Auto Trigger By Proximity) chứ không
// viết trong file này - Boss cũng dùng chung đúng component đó. Enemy.MoveToPlayer() tự đứng im khi chiêu
// đang chạy nên ở đây không cần override Update().
public class USBEnemy : Enemy
{
    [Header("Vật phẩm rơi ra")]
    [SerializeField] private GameObject usbObject;

    protected override void DropItems()
    {
        if (usbObject != null) SpawnItem(usbObject);

        if (xpObject != null)
        {
            int smallCount = UnityEngine.Random.Range(1, 3);
            for (int i = 0; i < smallCount; i++) SpawnItem(xpObject);
        }
    }
}
