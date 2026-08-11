using Backend.Util;
using UnityEngine;
using Backend.Object.GameSystems;
using Backend.Object.Management;
using Backend.AddressableKey;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Backend.Object.UI;
using R3;

namespace Backend.Object.Controller
{
    public class TurnController : CachedMonobehaviour
    {
        private RectTransform _turnContainer;
        private readonly List<TapIcon> _tapIcons = new();

        public void Initialize()
        {
            CreateTapIcons();

            TurnSystem.ActionRemainPoint.Subscribe(OnActionRemainPointChanged).AddTo(destroyCancellationToken);
        }

        public void SetTurnContainer(RectTransform turnContainer)
        {
            _turnContainer = turnContainer;
        }

        private void OnDestroy()
        {
            // TapIcon 은 DontDestroyOnLoad HUD 하위에 붙으므로, 씬 컨트롤러 파괴 시 명시적으로 제거한다.
            ClearTapIcons();
        }

        private void CreateTapIcons()
        {
            // 이전 런에서 풀에 남은 HUD 컨테이너 자식이 있으면 먼저 비운다.
            ClearTapIcons();

            var tapIconPrefab = ResourceManager.LoadComponent<TapIcon>(AddressableKeys.UI.Get("TapIcon"));
            if (tapIconPrefab == null || _turnContainer == null) return;

            for (int i = 0; i < TurnSystem.DefaultActionCount; i++)
            {
                var tapIcon = Instantiate(tapIconPrefab, _turnContainer);
                _tapIcons.Add(tapIcon);
            }
        }

        private void ClearTapIcons()
        {
            for (int i = 0; i < _tapIcons.Count; i++)
            {
                if (_tapIcons[i] != null)
                    Destroy(_tapIcons[i].gameObject);
            }
            _tapIcons.Clear();

            if (_turnContainer == null) return;
            for (int i = _turnContainer.childCount - 1; i >= 0; i--)
            {
                var child = _turnContainer.GetChild(i);
                if (child != null)
                    Destroy(child.gameObject);
            }
        }

        private void OnActionRemainPointChanged(int remainPoint)
        {
            for (int i = 0; i < _tapIcons.Count; i++)
            {
                _tapIcons[i].SetTapIconVisible(i < remainPoint);
            }
        }
    }
}