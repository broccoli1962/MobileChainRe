using Backend.Object.Management;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Backend.Object.UI
{
    public class QuestBox : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _questName;
        [SerializeField] private TextMeshProUGUI _questDescription;

        private CommonButton _button;
        private QuestData _questData;

        private void Awake()
        {
            _button = GetComponent<CommonButton>();
        }

        public void SetData(QuestData questData)
        {
            _questData = questData;
            _questName.text = questData.questName;
            _questDescription.text = questData.questDescript;
            _button.OnClick.AddListener(OnClicked);
        }

        private void OnClicked()
        {
            OpenDetailAsync().Forget();
        }

        private async UniTaskVoid OpenDetailAsync()
        {
            var panel = await UIManager.OpenAsync<QuestDetailPanel>();
            panel?.SetData(_questData);
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.OnClick.RemoveListener(OnClicked);
        }
    }
}
