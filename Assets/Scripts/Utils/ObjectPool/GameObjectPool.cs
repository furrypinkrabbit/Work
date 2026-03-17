using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Utils.ObjectPool
{
    public class GameObjectPool : IDisposable
    {
        private readonly ObjectPool<PooledGameObject> _pool;
        private readonly GameObject _prefab;
        private readonly Transform _parent;

        public GameObjectPool(GameObject prefab, int defaultSize = 10, int maxSize = 100, Transform parent = null)
        {
            _prefab = prefab ? prefab : throw new System.ArgumentNullException(nameof(prefab));
            _parent = parent;

            _pool = new ObjectPool<PooledGameObject>(maxSize, () =>
            {
                var go = UnityEngine.Object.Instantiate(_prefab, _parent);
                var poolable = go.GetOrAddComponent<PooledGameObject>();
                poolable.SetPool(this);
                return poolable;
            });

            // 预创建
            for (int i = 0; i < defaultSize; i++)
            {
                var obj = _pool.Get();
                _pool.Release(obj);
            }
        }

        public GameObject Get() => _pool.Get().gameObject;
        public void Release(GameObject go) => go.GetComponent<PooledGameObject>().Release();
        public void Dispose() => _pool.Clear();
    }

    /// <summary>
    /// 绑定在GameObject上的池化组件
    /// </summary>
    public class PooledGameObject : MonoBehaviour, IPoolable
    {
        private GameObjectPool _pool;

        public void SetPool(GameObjectPool pool) => _pool = pool;

        public void OnCreate() { }
        public void OnGet() => gameObject.SetActive(true);
        public void OnRelease() => gameObject.SetActive(false);
        public void OnDestroy() => Destroy(gameObject);

        public void Release()
        {
            if (_pool != null) _pool.Release(gameObject);
        }

        public void OnDestory()
        {
            
        }
    }

    /// <summary>
    /// 快速获取组件
    /// </summary>
    public static class GameObjectExtension
    {
        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            var comp = go.GetComponent<T>();
            if (comp == null) comp = go.AddComponent<T>();
            return comp;
        }
    }
}
