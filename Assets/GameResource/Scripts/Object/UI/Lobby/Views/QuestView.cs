using System.Collections.Generic;
using Backend.AddressableKey;
using Backend.Object.Management;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Backend.Object.UI
{
    public class QuestView : UIView
    {
        [SerializeField] private Transform _questBoxRoot;

        private readonly List<QuestBox> _questBoxes = new();

        protected override void OnShow()
        {
            base.OnShow();
            CreateQuestBoxesAsync().Forget();
        }

        protected override void OnHide()
        {
            base.OnHide();
            ClearQuestBoxes();
        }

        private async UniTaskVoid CreateQuestBoxesAsync()
        {
            var prefab = await ResourceManager.LoadComponentAsync<QuestBox>(AddressableKeys.UI.Get("QuestBox"));
            if (prefab == null) return;

            foreach (var quest in TableManager.GetAllQuests())
            {
                var box = Instantiate(prefab, _questBoxRoot);
                box.SetData(quest);
                _questBoxes.Add(box);
            }
        }

        private void ClearQuestBoxes()
        {
            foreach (var box in _questBoxes)
            {
                if (box != null)
                    Destroy(box.gameObject);
            }
            _questBoxes.Clear();
        }
    }
}
