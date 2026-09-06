using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    private Vector3 movementDirection;
    private void OnEnable()
    {
        Invoke(nameof(DisableBullet), 5f);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(DisableBullet));
        movementDirection = Vector3.zero; // Reset vector tránh bay tiếp khi Tái sinh
    }

    private void DisableBullet()
    {
        if (ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnObjectToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Update()
    {
        if (movementDirection == Vector3.zero) return;
        transform.position += movementDirection * Time.deltaTime;
    }

    public void SetMovementDirection(Vector3 direction)
    {
        movementDirection = direction;
    }
}
