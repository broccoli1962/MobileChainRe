using System;
using Backend.Object.GameSystems;
using Backend.Object.Management;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Backend.Object.UI
{
    public class RunShopPopup : UIPopup
    {
        [Serializable]
        public class SlotView
        {
            public TextMeshProUGUI nameText;
            public TextMeshProUGUI priceText;
            public TextMeshProUGUI descriptText;
            public TextMeshProUGUI reasonText;
            public CommonButton buyButton;
        }

        [SerializeField] private TextMeshProUGUI _goldText;
        [SerializeField] private TextMeshProUGUI _floorText;
        [SerializeField] private TextMeshProUGUI _rerollText;
        [SerializeField] private CommonButton _blocker;
        [SerializeField] private CommonButton _exitButton;
        [SerializeField] private CommonButton _rerollButton;
        [SerializeField] private SlotView[] _slots;

        private ClassicGameSession _session;
        private UniTaskCompletionSource _closed;

        public void Bind(ClassicGameSession session)
        {
            _session = session;
            RunShopSystem.Open(session);
            Refresh();
        }

        public UniTask WaitForCloseAsync()
        {
            _closed ??= new UniTaskCompletionSource();
            return _closed.Task;
        }

        protected override void OnOpen()
        {
            base.OnOpen();
            _closed = new UniTaskCompletionSource();
            if (_blocker != null) _blocker.OnClick.AddListener(OnExitClicked);
            if (_exitButton != null) _exitButton.OnClick.AddListener(OnExitClicked);
            if (_rerollButton != null) _rerollButton.OnClick.AddListener(OnRerollClicked);
            BindSlotButtons(true);
        }

        protected override void OnClose()
        {
            BindSlotButtons(false);
            if (_blocker != null) _blocker.OnClick.RemoveListener(OnExitClicked);
            if (_exitButton != null) _exitButton.OnClick.RemoveListener(OnExitClicked);
            if (_rerollButton != null) _rerollButton.OnClick.RemoveListener(OnRerollClicked);
            RunShopSystem.Close();
            _session = null;
            _closed?.TrySetResult();
            base.OnClose();
        }

        private void BindSlotButtons(bool bind)
        {
            if (_slots == null) return;
            for (int i = 0; i < _slots.Length; i++)
            {
                var slot = _slots[i];
                if (slot?.buyButton == null) continue;
                int index = i;
                if (bind)
                    slot.buyButton.OnClick.AddListener(() => OnBuyClicked(index));
                else
                    slot.buyButton.OnClick.RemoveAllListeners();
            }
        }

        private void OnBuyClicked(int slot)
        {
            if (!RunShopSystem.TryPurchase(slot, out _))
            {
                Refresh();
                return;
            }

            Refresh();
        }

        private void OnRerollClicked()
        {
            if (!RunShopSystem.TryReroll(out _))
            {
                Refresh();
                return;
            }

            Refresh();
        }

        private void OnExitClicked()
        {
            UIManager.Close(this);
        }

        private void Refresh()
        {
            int gold = _session != null ? _session.Gold : 0;
            int nextFloor = _session != null ? _session.CurrentFloor : 0;
            SetText(_goldText, $"Gold {gold}");
            SetText(_floorText, $"다음 층 {nextFloor}");

            bool canReroll = RunShopSystem.CanReroll && gold >= RunShopSystem.RerollCost;
            if (_rerollButton != null)
                _rerollButton.interactable = canReroll;
            SetText(_rerollText, RunShopSystem.CanReroll
                ? $"리롤 ({RunShopSystem.RerollCost})"
                : "리롤 완료");

            var stock = RunShopSystem.Stock;
            if (_slots == null) return;

            for (int i = 0; i < _slots.Length; i++)
            {
                var view = _slots[i];
                if (view == null) continue;

                if (i >= stock.Count)
                {
                    SetText(view.nameText, string.Empty);
                    SetText(view.priceText, string.Empty);
                    SetText(view.descriptText, string.Empty);
                    SetText(view.reasonText, "품절");
                    if (view.buyButton != null)
                        view.buyButton.interactable = false;
                    continue;
                }

                var item = stock[i];
                SetText(view.nameText, GetItemName(item));
                SetText(view.priceText, $"{item.price} G");
                SetText(view.descriptText, GetItemDescript(item));

                bool sold = RunShopSystem.IsSold(i);
                bool owned = item.category == ShopCategory.relic
                    && _session != null
                    && ContainsRelic(_session.OwnedRelics, item.itemRefId);
                bool poor = gold < item.price;
                bool canBuy = !sold && !owned && !poor;

                if (view.buyButton != null)
                    view.buyButton.interactable = canBuy;

                string reason;
                if (sold) reason = "구매함";
                else if (owned) reason = "보유 중";
                else if (poor) reason = "골드 부족";
                else reason = "구매";
                SetText(view.reasonText, reason);
            }
        }

        private static string GetItemName(ShopItemData item)
        {
            if (item.category == ShopCategory.heal)
                return $"HP {item.itemRefId}%";

            var relic = TableManager.GetRelic(item.itemRefId);
            return relic != null ? relic.relicName : $"유물 {item.itemRefId}";
        }

        private static string GetItemDescript(ShopItemData item)
        {
            if (item.category == ShopCategory.heal)
                return $"체력 {item.itemRefId}% 회복";

            var relic = TableManager.GetRelic(item.itemRefId);
            return relic != null ? relic.descript : string.Empty;
        }

        private static bool ContainsRelic(System.Collections.Generic.IReadOnlyList<int> ids, int relicId)
        {
            for (int i = 0; i < ids.Count; i++)
            {
                if (ids[i] == relicId) return true;
            }

            return false;
        }

        private static void SetText(TextMeshProUGUI label, string value)
        {
            if (label != null)
                label.text = value ?? string.Empty;
        }
    }
}
