using System.Collections.Generic;
using UnityEngine;

namespace Backend.Object.Management
{
    public partial class TableManager
    {
        private readonly Dictionary<int, ShopItemData> _dicShopItem = new();
        private readonly Dictionary<int, RelicData> _dicRelic = new();
        private readonly List<ShopItemData> _shopItems = new();

        private void CreateShopDict()
        {
            _dicShopItem.Clear();
            _shopItems.Clear();
            if (_tableLinker.ShopItemTable?.dataList != null)
            {
                foreach (var data in _tableLinker.ShopItemTable.dataList)
                {
                    if (_dicShopItem.TryAdd(data.shopItemId, data))
                        _shopItems.Add(data);
                }
            }

            _dicRelic.Clear();
            if (_tableLinker.RelicTable?.dataList != null)
            {
                foreach (var data in _tableLinker.RelicTable.dataList)
                    _dicRelic.TryAdd(data.relicId, data);
            }
        }

        public static ShopItemData GetShopItem(int shopItemId)
        {
            if (Instance._dicShopItem.TryGetValue(shopItemId, out var data))
                return data;

            Debug.LogWarning($"[TableManager] ShopItemData not found: {shopItemId}");
            return null;
        }

        public static IReadOnlyList<ShopItemData> GetShopItemsForFloor(int floor)
        {
            var result = new List<ShopItemData>();
            var items = Instance._shopItems;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].unlockFloor <= floor)
                    result.Add(items[i]);
            }

            return result;
        }

        public static RelicData GetRelic(int relicId)
        {
            if (Instance._dicRelic.TryGetValue(relicId, out var data))
                return data;

            Debug.LogWarning($"[TableManager] RelicData not found: {relicId}");
            return null;
        }
    }
}
