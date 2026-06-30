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

        private void CreateTapIcons(){
            var tapIconPrefab = ResourceManager.LoadComponent<TapIcon>(AddressableKeys.UI.Get("TapIcon"));
            
            for(int i = 0; i < TurnSystem.DefaultActionCount; i++){
                var tapIcon = Instantiate(tapIconPrefab, _turnContainer);
                _tapIcons.Add(tapIcon);
            }
        }

        private void OnActionRemainPointChanged(int remainPoint){
            for(int i = 0; i < _tapIcons.Count; i++){
                _tapIcons[i].SetTapIconVisible(i < remainPoint);
            }
        }
    }
}