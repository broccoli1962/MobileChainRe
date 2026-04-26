using Backend.Util.Management;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Backend.Object.Management
{
    public class ResourceManager : SingletonGameObject<ResourceManager>
    {
        private Dictionary<string, AsyncOperationHandle> _resourceCache = new();

        #region #static Method
        public static T LoadResource<T>(string key) where T : UnityEngine.Object
        {
            return Instance.LoadResource_Internal<T>(key);
        }

        public static UniTask<T> LoadResourceAsync<T>(string key) where T : UnityEngine.Object
        {
            return Instance.LoadResourceAsync_Internal<T>(key);
        }

        public static void ReleaseResource(string key)
        {
            Instance.ReleaseResource_Internal(key);
        }
        #endregion

        #region #Internal Method
        private T LoadResource_Internal<T>(string key) where T : UnityEngine.Object
        {
            if(_resourceCache.TryGetValue(key, out AsyncOperationHandle cachedHandle))
            {
                return cachedHandle.Result as T;
            }

            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(key);
            T resource = handle.WaitForCompletion();

            if(handle.Status == AsyncOperationStatus.Succeeded)
            {
                _resourceCache.Add(key, handle);
                return resource;
            }
            else
            {
                Debug.LogError($"Asset Load Fail! Key : {key}");
                Addressables.Release(handle);
                return null;
            }
        }

        private void ReleaseResource_Internal(string key)
        {
            if (_resourceCache.TryGetValue(key, out AsyncOperationHandle handle))
            {
                Addressables.Release(handle);
                _resourceCache.Remove(key);
            }
            else
            {
                Debug.LogWarning($"[ResourceManager] 해제할 에셋이 캐시에 없습니다: {key}");
            }
        }

        private async UniTask<T> LoadResourceAsync_Internal<T>(string key) where T : UnityEngine.Object
        {
            if(_resourceCache.TryGetValue(key, out AsyncOperationHandle cachedHandle))
            {
                if (!cachedHandle.IsDone)
                {
                    await cachedHandle.ToUniTask();
                }
                return cachedHandle.Result as T;
            }

            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(key);

            _resourceCache.Add(key, handle);

            T resource = await handle.ToUniTask();

            if(handle.Status == AsyncOperationStatus.Succeeded)
            {
                return resource;
            }
            else
            {
                Debug.LogError($"Asset Load Fail! Key : {key}");
                _resourceCache.Remove(key);
                Addressables.Release(handle);
                return null;
            }
        }
        
        private void OnDestroy()
        {
            foreach (var handle in _resourceCache.Values)
            {
                Addressables.Release(handle);
            }
            _resourceCache.Clear();
        }
        #endregion
    }
}