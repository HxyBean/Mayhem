using UnityEngine;

public class MiniEnemy : Enemy
{
    protected override void DropItems()
    {
        if (xpObject != null) SpawnItem(xpObject); // Luôn ra 1 viên nhỏ
    }
}
