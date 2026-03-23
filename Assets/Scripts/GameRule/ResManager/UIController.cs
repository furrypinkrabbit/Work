using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Collections;

namespace Assets.Scripts.GameRule.ResManager
{

    public class UIController : MonoBehaviour,IDisposable
    {
        private const string UIDefaultPath = "UIConfig";
        private readonly Dictionary<string, GameObject> _uiPrefabCache;
        private readonly Dictionary<string, GameObject> _uiInstanceCache;

        public static UIController Instance;

        private UIController()
        {
            _uiPrefabCache = new Dictionary<string, GameObject>();
            _uiInstanceCache = new Dictionary<string, GameObject>();
        }

        void Awake() {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(this.gameObject);
            }
            else {
                Destroy(gameObject);
            }
        }

        public void LoadUIAsync(string uiName, bool instantiateNow = true, Transform parent = null, Action<GameObject> onComplete = null)
        {
           StartCoroutine(LoadUICoroutine(uiName, instantiateNow, parent, onComplete));
        }

        private IEnumerator LoadUICoroutine(string uiName, bool instantiateNow, Transform parent, Action<GameObject> onComplete)
        {
            if (string.IsNullOrEmpty(uiName))
            {
                Debug.LogError("[UIController] 加载失败：UI 名称不能为空！");
                onComplete?.Invoke(null);
                yield break;
            }

            if (_uiInstanceCache.TryGetValue(uiName, out var existInstance) && existInstance != null)
            {
                existInstance.SetActive(true);
                onComplete?.Invoke(existInstance);
                yield break;
            }

            string uiPath = $"{UIDefaultPath}/{uiName}/show";

            ResourceRequest request = Resources.LoadAsync<GameObject>(uiPath);
            yield return request;

            if (request.asset == null)
            {
                Debug.LogError($"[UIController] 加载失败：路径不存在 -> {uiPath}");
                onComplete?.Invoke(null);
                yield break;
            }

            GameObject prefab = request.asset as GameObject;
            _uiPrefabCache[uiName] = prefab;

            GameObject uiInstance = null;
            if (instantiateNow)
            {
                uiInstance = UnityEngine.Object.Instantiate(prefab, parent);
                uiInstance.name = uiName;
                _uiInstanceCache[uiName] = uiInstance;
            }

            Debug.Log($"[UIController] 加载成功：{uiName}");
            onComplete?.Invoke(uiInstance);
        }

        #region UI 管理功能
        public void ShowUI(string uiName)
        {
            if (_uiInstanceCache.TryGetValue(uiName, out var obj) && obj != null)
                obj.SetActive(true);
        }

        public void HideUI(string uiName)
        {
            if (_uiInstanceCache.TryGetValue(uiName, out var obj) && obj != null)
                obj.SetActive(false);
        }

        public void DestroyUI(string uiName)
        {
            if (_uiInstanceCache.TryGetValue(uiName, out var obj) && obj != null)
            {
                UnityEngine.Object.Destroy(obj);
                _uiInstanceCache.Remove(uiName);
            }
        }

        public void ClearAllUI()
        {
            foreach (var obj in _uiInstanceCache.Values)
                if (obj != null) UnityEngine.Object.Destroy(obj);

            _uiInstanceCache.Clear();
            _uiPrefabCache.Clear();
            Debug.Log("[UIController] 已清空所有 UI 缓存");
        }
        #endregion

        public void Dispose() => ClearAllUI();
    }

}