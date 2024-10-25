using System.Collections.Generic;
using UnityEngine;

public class ColliderPoolManager : MonoBehaviour
{
    public GameObject colliderPrefab; // Prefab with BoxCollider2D
    public int poolSize = 100; // Adjust based on needs

    private Queue<GameObject> colliderPool;

    void Awake()
    {
        colliderPool = new Queue<GameObject>();

        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(colliderPrefab);
            obj.SetActive(false);
            colliderPool.Enqueue(obj);
        }
    }

    public GameObject GetCollider()
    {
        if (colliderPool.Count > 0)
        {
            GameObject obj = colliderPool.Dequeue();
            obj.SetActive(true);
            return obj;
        }
        else
        {
            // Optionally, expand the pool
            GameObject obj = Instantiate(colliderPrefab);
            return obj;
        }
    }

    public void ReturnCollider(GameObject obj)
    {
        obj.SetActive(false);
        colliderPool.Enqueue(obj);
    }
}
