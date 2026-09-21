using UnityEngine;

public class BasicEnemy : Enemy
{
    // Dùng nguyên hành vi va chạm mặc định từ Enemy (OnPlayerEnter/OnPlayerStay)
    protected override void DropItems()
    {
        if (bigXpObject != null)
        {
            int bigCount = UnityEngine.Random.Range(0, 2); // 1-2 viên to
            for (int i = 0; i < bigCount; i++) SpawnItem(bigXpObject);
        }
        
        if (xpObject != null)
        {
            int smallCount = UnityEngine.Random.Range(1, 3); // 0-2 viên nhỏ
            for (int i = 0; i < smallCount; i++) SpawnItem(xpObject);
        }
    }
}
