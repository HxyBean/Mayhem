using UnityEngine;

public class HealEnemy : Enemy
{
    [SerializeField] private float healValue = 10f;
    [SerializeField] private GameObject heartPrefab; // Vật phẩm trái tim, nhặt vào mới hồi máu

    protected override void DropItems()
    {
        if (bigXpObject != null)
        {
            int bigCount = UnityEngine.Random.Range(1, 3); // 1-2 viên to
            for (int i = 0; i < bigCount; i++) SpawnItem(bigXpObject);
        }

        if (xpObject != null)
        {
            int smallCount = UnityEngine.Random.Range(0, 4); // 0-3 viên nhỏ
            for (int i = 0; i < smallCount; i++) SpawnItem(xpObject);
        }

        DropHeart();
    }

    private void DropHeart()
    {
        if (heartPrefab == null) return;

        GameObject heart = SpawnItem(heartPrefab);
        if (heart != null)
        {
            HeartPickup pickup = heart.GetComponent<HeartPickup>();
            if (pickup != null) pickup.healValue = healValue;
        }
    }
}
