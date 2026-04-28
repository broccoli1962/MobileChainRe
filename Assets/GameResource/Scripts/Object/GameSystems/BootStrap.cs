using Backend.AddressableKey;
using Backend.Object.Management;
using Backend.Object.UI;
using UnityEngine;

namespace Backend
{
    public class BootStrap : MonoBehaviour
    {
        void Awake()
        {
            UIManager.OpenAsync<TestPopupUI>(AddressableKeys.UI.Get("TestPopupPrefab"));
        }
    }
}
