using UnityEngine;

// Gắn vào Prefab vật phẩm "trái tim" rớt ra khi tiêu diệt HealEnemy.
// GameObject cần đặt Tag "Heart" và có Collider2D (isTrigger) để PlayerCollision nhận diện.
public class HeartPickup : MonoBehaviour
{
    public float healValue = 10f;
}
