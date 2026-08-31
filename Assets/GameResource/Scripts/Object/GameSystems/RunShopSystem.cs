using System;
using System.Collections.Generic;
using Backend.Object.Management;
using UnityEngine;

namespace Backend.Object.GameSystems
{
    /// <summary>
    /// Classic 층간 상점. 가중 재고 추첨·구매·리롤 1회.
    /// </summary>
    public static class RunShopSystem
    {
        public const int SlotCount = 3;
        public const int RerollCost = 15;

        private static readonly List<ShopItemData> _stock = new();
        private static readonly bool[] _sold = new bool[SlotCount];
        private static ClassicGameSession _session;
        private static int _rerollCount;
        private static bool _opened;

        public static IReadOnlyList<ShopItemData> Stock => _stock;
        public static bool CanReroll => _opened && _rerollCount == 0;

        public static void Open(ClassicGameSession session)
        {
            _session = session;
            _rerollCount = 0;
            _opened = session != null;
            Roll();
        }

        public static void Close()
        {
            _session = null;
            _stock.Clear();
            _opened = false;
            _rerollCount = 0;
            Array.Clear(_sold, 0, _sold.Length);
        }

        public static bool IsSold(int slot)
        {
            return slot >= 0 && slot < SlotCount && _sold[slot];
        }

        public static bool TryPurchase(int slot, out string reason)
        {
            reason = null;
            if (!_opened || _session == null)
            {
                reason = "상점 없음";
                return false;
            }

            if (slot < 0 || slot >= _stock.Count)
            {
                reason = "슬롯 없음";
                return false;
            }

            if (_sold[slot])
            {
                reason = "구매함";
                return false;
            }

            var item = _stock[slot];
            if (item == null)
            {
                reason = "상품 없음";
                return false;
            }

            if (_session.Gold < item.price)
            {
                reason = "골드 부족";
                return false;
            }

            if (item.category == ShopCategory.heal)
            {
                if (!_session.TrySpendGold(item.price))
                {
                    reason = "골드 부족";
                    return false;
                }

                PartySystem.Heal(PartySystem.MaxHp * item.itemRefId / 100f);
                _session.CaptureHp();
                _sold[slot] = true;
                return true;
            }

            if (item.category == ShopCategory.relic)
            {
                if (ContainsOwned(item.itemRefId))
                {
                    reason = "보유 중";
                    return false;
                }

                if (!_session.TrySpendGold(item.price))
                {
                    reason = "골드 부족";
                    return false;
                }

                _session.AddRelic(item.itemRefId);
                _sold[slot] = true;
                return true;
            }

            reason = "구매 불가";
            return false;
        }

        public static bool TryReroll(out string reason)
        {
            reason = null;
            if (!CanReroll)
            {
                reason = "리롤 불가";
                return false;
            }

            if (_session == null || _session.Gold < RerollCost)
            {
                reason = "골드 부족";
                return false;
            }

            if (!_session.TrySpendGold(RerollCost))
            {
                reason = "골드 부족";
                return false;
            }

            _rerollCount++;
            Roll();
            return true;
        }

        private static void Roll()
        {
            _stock.Clear();
            Array.Clear(_sold, 0, _sold.Length);
            if (_session == null) return;

            var candidates = BuildCandidates(_session.CurrentFloor, _session.OwnedRelics);
            if (candidates.Count == 0) return;

            int seed = _session.Seed ^ (_session.CurrentFloor * 397) ^ (_rerollCount * 104729);
            var rng = new System.Random(seed);

            var pool = new List<ShopItemData>(candidates);
            for (int i = 0; i < SlotCount && pool.Count > 0; i++)
            {
                var picked = PickWeighted(pool, rng);
                if (picked == null) break;
                _stock.Add(picked);
                pool.Remove(picked);
            }

            EnsureHealSlot(candidates, rng);
        }

        private static List<ShopItemData> BuildCandidates(int floor, IReadOnlyList<int> owned)
        {
            var items = TableManager.GetShopItemsForFloor(floor);
            var result = new List<ShopItemData>();
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item.category == ShopCategory.heal)
                {
                    result.Add(item);
                    continue;
                }

                if (item.category != ShopCategory.relic) continue;

                var relic = TableManager.GetRelic(item.itemRefId);
                if (relic == null) continue;
                if (!RelicSystem.IsSupported(relic.effectKey)) continue;
                if (ContainsId(owned, item.itemRefId)) continue;
                result.Add(item);
            }

            return result;
        }

        private static void EnsureHealSlot(List<ShopItemData> candidates, System.Random rng)
        {
            if (_stock.Count == 0) return;

            bool hasHeal = false;
            for (int i = 0; i < _stock.Count; i++)
            {
                if (_stock[i].category == ShopCategory.heal)
                {
                    hasHeal = true;
                    break;
                }
            }

            if (hasHeal) return;

            var heals = new List<ShopItemData>();
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].category == ShopCategory.heal && !ContainsItem(_stock, candidates[i]))
                    heals.Add(candidates[i]);
            }

            if (heals.Count == 0)
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    if (candidates[i].category == ShopCategory.heal)
                        heals.Add(candidates[i]);
                }
            }

            if (heals.Count == 0) return;

            var heal = PickWeighted(heals, rng);
            if (heal == null) return;

            int replaceIndex = rng.Next(_stock.Count);
            _stock[replaceIndex] = heal;
        }

        private static ShopItemData PickWeighted(List<ShopItemData> pool, System.Random rng)
        {
            float total = 0f;
            for (int i = 0; i < pool.Count; i++)
                total += Mathf.Max(0f, pool[i].stockWeight);

            if (total <= 0f)
                return pool[rng.Next(pool.Count)];

            float roll = (float)rng.NextDouble() * total;
            float acc = 0f;
            for (int i = 0; i < pool.Count; i++)
            {
                acc += Mathf.Max(0f, pool[i].stockWeight);
                if (roll <= acc)
                    return pool[i];
            }

            return pool[pool.Count - 1];
        }

        private static bool ContainsOwned(int relicId)
        {
            return _session != null && ContainsId(_session.OwnedRelics, relicId);
        }

        private static bool ContainsId(IReadOnlyList<int> ids, int id)
        {
            for (int i = 0; i < ids.Count; i++)
            {
                if (ids[i] == id) return true;
            }

            return false;
        }

        private static bool ContainsItem(List<ShopItemData> list, ShopItemData item)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].shopItemId == item.shopItemId) return true;
            }

            return false;
        }
    }
}
