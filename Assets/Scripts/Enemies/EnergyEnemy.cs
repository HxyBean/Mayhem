using UnityEngine;

public class EnergyEnemy : Enemy
{
    [SerializeField] private GameObject energyObject;

    protected override void DropItems()
    {
        if (energyObject != null)
        {
            int energyCount = UnityEngine.Random.Range(1, 3); // 1-2 năng lượng
            for (int i = 0; i < energyCount; i++) SpawnItem(energyObject);
        }
        
        if (xpObject != null)
        {
            int smallCount = UnityEngine.Random.Range(1, 4); // 1-3 viên nhỏ
            for (int i = 0; i < smallCount; i++) SpawnItem(xpObject);
        }
    }
}
