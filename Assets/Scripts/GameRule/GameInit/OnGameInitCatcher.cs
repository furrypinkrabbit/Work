using UnityEngine;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace GameRule.GameInit
{

    public sealed class OnGameInitCatcher
    {
        private static readonly Lazy<OnGameInitCatcher> _lazy = new Lazy<OnGameInitCatcher>(
    () => new OnGameInitCatcher(),
    LazyThreadSafetyMode.ExecutionAndPublication
);
        public static OnGameInitCatcher Instance => _lazy.Value;
        private OnGameInitCatcher() { }

        private Dictionary<string, object> _cache;

        public async Task<Dictionary<string, object>> LoadAllAsync()
        {
            if (_cache != null) return _cache;

            // 找出所有带特性的类型
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .Where(t => t.IsClass && !t.IsAbstract
                    && t.GetCustomAttribute<OnGameInitHandler>() != null)
                .ToList();

            _cache = new Dictionary<string, object>();

            foreach (var type in types)
            {
                // 实例化
                var instance = Activator.CreateInstance(type);

                // 调用初始化逻辑
                if (instance is IOnGameInit initable)
                {
                    await initable.InitAsync();
                }

                _cache[type.Name] = instance;
            }

            return _cache;
        }

        public T Get<T>() where T : class
            => _cache != null && _cache.TryGetValue(typeof(T).Name, out var obj) ? obj as T : null;
    }
}