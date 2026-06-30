using UnityEngine;
using UnityEngine.UI;

namespace Backend.Object.UI
{
    public class TapIcon : MonoBehaviour
    {
        [SerializeField] private Image _tapIcon;

        public void SetTapIconVisible(bool isActive){
            _tapIcon.gameObject.SetActive(isActive);
        }
    }
}
