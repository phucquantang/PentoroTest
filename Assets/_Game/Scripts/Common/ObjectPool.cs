using System.Collections.Generic;
using UnityEngine;

public class ObjectPool<T> where T : Component
{
    private readonly Queue<T> _pool = new Queue<T>();

    public T Get(GameObject prefab, Transform parent)
    {
        if (_pool.Count > 0)
        {
            var item = _pool.Dequeue();

            item.gameObject.SetActive(true);
            item.transform.SetParent(parent);
            return item;
        }

        var newItem = Object.Instantiate(prefab, parent).GetComponent<T>();
        return newItem;
    }

    public void Return(T item)
    {
        item.gameObject.SetActive(false);
        _pool.Enqueue(item);
    }

    public void Clear()
    {
        while (_pool.Count > 0)
        {
            var item = _pool.Dequeue();
            if (item != null)
                Object.Destroy(item.gameObject);
        }
    }
}