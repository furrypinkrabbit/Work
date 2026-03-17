using Assets.Scripts.Utils.ObjectPool;
using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

public class ObjectPool<T> where T : class, IPoolable, new() {

    //线程安全队列
    private readonly ConcurrentQueue<T> _poolQueue;

    //对象工厂
    private readonly Func<T> _objectFactory;

    //池最大容量
    private readonly int _maxLoad;

    //当前对象总数
    private int _currentCount;

    public int Count => _poolQueue.Count;
    public int ActiveCount => _currentCount - Count;
    public int MaxPoolLoad => _maxLoad;


    #region 构造函数
    public ObjectPool(int MaxLoad = 100, Func<T> factory = null) {
        if (MaxLoad <= 0) {
            throw new ArgumentOutOfRangeException(nameof(MaxLoad));
        }

        _maxLoad = MaxLoad;
        _objectFactory = factory ?? (() => new T());
        _poolQueue = new ConcurrentQueue<T>();
        _currentCount = 0;
    }
    #endregion

    #region 从池中获取
    public T Get() {
        if (_poolQueue.TryDequeue(out T obj)) {
            obj.OnGet();
            return obj;
        }

        if (_currentCount >= _maxLoad) {
            throw new InvalidOperationException("This gameobjectpool is FULL!"+_currentCount);
        }

        T newObj = _objectFactory();
        Interlocked.Increment(ref _currentCount);
        newObj.OnCreate();
        newObj.OnGet();

        return newObj;
    }
    #endregion


    #region 返回对象
    public void Release(T obj) {

        if (obj == null) {
            throw new ArgumentNullException(nameof(obj));
        }


        obj.OnRelease();

        if (_poolQueue.Count >= _maxLoad) {
            InternalDestory(obj);
            return;
        }

        _poolQueue.Enqueue(obj);

    }

    #endregion

    #region 销毁对象
    public void InternalDestory(T obj) {
        obj.OnDestory();
        Interlocked.Decrement(ref _currentCount);
    }
    #endregion

    #region 清空对象池
    public void Clear() {
        while (_poolQueue.TryDequeue(out T obj)) {

            InternalDestory(obj);
        }

    }


    #endregion

}

