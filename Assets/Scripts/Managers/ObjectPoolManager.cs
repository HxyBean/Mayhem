using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    // Dictionary để ánh xạ từ Prefab gốc sang ObjectPool của nó
    private Dictionary<GameObject, ObjectPool<GameObject>> _pools = new Dictionary<GameObject, ObjectPool<GameObject>>();
    
    // Thành phần ẩn được gắn vào Object mỗi khi sinh ra để biết nó cần quay về Pool nào
    public class PooledObject : MonoBehaviour
    {
        public ObjectPool<GameObject> pool;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public GameObject SpawnObject(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        if (!_pools.ContainsKey(prefab))
        {
            // Tạo pool động mới cho prefab này nếu chưa có
            _pools[prefab] = new ObjectPool<GameObject>(
                createFunc: () => {
                    GameObject obj = Instantiate(prefab);
                    // Đính kèm thông tin tham chiếu Pool
                    PooledObject po = obj.AddComponent<PooledObject>();
                    po.pool = _pools[prefab];
                    obj.SetActive(false); // Tắt ngay để lần Get() đầu tiên cũng kích hoạt OnEnable đúng lúc, đúng vị trí
                    return obj;
                },
                actionOnGet: (obj) => {
                    // Không set position/rotation/SetActive ở đây: closure này chỉ được tạo 1 lần
                    // (khi Pool khởi tạo) nên nếu set vị trí ở đây, mọi lần Spawn sau sẽ bị dùng nhầm
                    // vị trí của lần gọi SpawnObject() đầu tiên. Việc set vị trí + kích hoạt được
                    // thực hiện ngay sau khi Get() trả về bên dưới, luôn dùng đúng tham số hiện tại.
                },
                actionOnRelease: (obj) => {
                    obj.SetActive(false);
                },
                actionOnDestroy: (obj) => {
                    Destroy(obj);
                },
                collectionCheck: false,
                defaultCapacity: 20,
                maxSize: 500
            );
        }

        // Lấy Object từ Pool
        GameObject spawnedObj = _pools[prefab].Get();

        // Set đúng vị trí/xoay TRƯỚC khi kích hoạt, để OnEnable() của Object nhận đúng vị trí spawn
        spawnedObj.transform.SetPositionAndRotation(position, rotation);
        spawnedObj.SetActive(true);

        return spawnedObj;
    }

    public void ReturnObjectToPool(GameObject obj)
    {
        if (obj == null) return;
        
        // Kiểm tra xem object có thuốc Pool nào không
        PooledObject po = obj.GetComponent<PooledObject>();
        if (po != null && po.pool != null)
        {
            // Nếu object vẫn đang Active thì mới Release để tránh lỗi Release 2 lần
            if (obj.activeSelf)
            {
                // Gỡ khỏi parent (VD hiệu ứng được gắn theo Player lúc Spawn) trước khi trả về Pool,
                // để lần Spawn tiếp theo không bị dính nhầm vào parent cũ.
                obj.transform.SetParent(null);
                po.pool.Release(obj);
            }
        }
        else
        {
            // Nhánh dự phòng, lỡ chẳng may tạo ra mà không thông qua Pool
            Destroy(obj);
        }
    }
}
