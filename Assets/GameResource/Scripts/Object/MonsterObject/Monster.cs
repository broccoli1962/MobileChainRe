using System.Collections.Generic;
using System.Threading;
using Backend.Object.Management;
using Backend.Object.UI;
using Backend.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Backend.Object.MonsterObject
{
    public class Monster : CachedMonobehaviour
    {
        [SerializeField] private MonsterHealthBar _monsterHealthBar;
        [SerializeField] private KeyframeColorGradient _phaseColorGradient = new();

        private IReadOnlyList<MonsterBehaviorData> _behaviorData;
        private IReadOnlyList<MonsterActionData> _actionData;
        private float _damage;
        private int _currentMonsterPhaseIndex = 0;
        private int _actionIndex = 0;

        public bool IsDefeated => _monsterHealthBar != null && _monsterHealthBar.IsDefeated;

        public void InitializeMonster(MonsterData monsterData, IReadOnlyList<MonsterBehaviorData> behaviorData)
        {
            _behaviorData = behaviorData;
            _damage = monsterData.monsterDamage;

            var layerMaxHp = new float[behaviorData.Count];
            for (int i = 0; i < behaviorData.Count; i++)
                layerMaxHp[i] = behaviorData[i].phaseHealth;

            _monsterHealthBar.Initialize(layerMaxHp, _phaseColorGradient);
            RefreshPhase(_currentMonsterPhaseIndex);
        }

        public void TakeDamage(float damage)
        {
            if (_monsterHealthBar == null || _monsterHealthBar.IsDefeated)
                return;

            int phaseDelta = _monsterHealthBar.ApplyDamage(damage);
            if (phaseDelta > 0 && !_monsterHealthBar.IsDefeated)
                RefreshPhase(_monsterHealthBar.CurrentLayerIndex);
        }

        public UniTask AdvanceTurnAsync(CancellationToken token)
        {
            //보스의 공격 방식 설정.



            return UniTask.CompletedTask;
        }

        private void RefreshPhase(int phaseIndex)
        {
            if (_behaviorData == null || phaseIndex >= _behaviorData.Count)
                return;

            _currentMonsterPhaseIndex = phaseIndex;
            _damage = _behaviorData[phaseIndex].phaseDamage;

            var actionGroupId = _behaviorData[_currentMonsterPhaseIndex].actionGroupId;
            _actionIndex = 0;
            //_currentCountDown = _actionData != null && _actionData.Count > 0 ? _actionData[0].turnDelay : int.MaxValue;
        }
    }
}