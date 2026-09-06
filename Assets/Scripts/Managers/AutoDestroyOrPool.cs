using UnityEngine;

public class AutoDestroyOrPool : MonoBehaviour
{
    public float lifetime = 5f;

    private void OnEnable()
    {
        Invoke(nameof(ReturnObj), lifetime);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(ReturnObj));
    }

    private void ReturnObj()
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
}
