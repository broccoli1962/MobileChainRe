using System.Collections.Generic;
using Backend.AddressableKey;
using Backend.Object.UI;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace Backend.Object.Management
{
    /// <summary>
    /// 현재 활성 IGameSession 홀더. 모드와 세션 인스턴스를 함께 생성·폐기한다.
    /// </summary>
    public static class ActiveSession
    {
        public static IGameSession Current { get; private set; }
        public static SessionMode Mode => Current?.Mode ?? SessionMode.None;

        /// <summary>Classic Run 세션을 시작한다. 파티는 이후 BindParty.</summary>
        public static void BeginClassic()
        {
            Current?.End();
            Current = new ClassicGameSession();
        }

        /// <summary>Quest 세션을 시작한다. 파티는 이후 BindParty.</summary>
        public static void BeginQuest(int questMapId, QuestDifficulty difficulty)
        {
            Current?.End();
            Current = new QuestGameSession(questMapId, difficulty);
        }

        public static void BindParty(IReadOnlyList<UserUnitData> party)
        {
            if (Current == null)
            {
                UnityEngine.Debug.LogError("[ActiveSession] BindParty called with no active session.");
                return;
            }

            Current.BindParty(party);
        }

        public static void OnGameplayStarted() => Current?.OnGameplayStarted();

        public static void OnGameplayEnded() => Current?.OnGameplayEnded();

        /// <summary>세션 종료 후 로비로 복귀.</summary>
        public static async UniTask AbortToLobbyAsync()
        {
            Clear();
            await UIManager.ShowLoadingAsync();
            await UIManager.CloseAllUIAsync();
            string address = AddressableKeys.InGame.Get("LobbyScene");
            await Addressables.LoadSceneAsync(address, LoadSceneMode.Single).ToUniTask();
        }

        /// <summary>현재 세션 파티를 유지한 채 GameScene을 다시 로드한다.</summary>
        public static async UniTask RetryAsync()
        {
            if (Current == null)
            {
                UnityEngine.Debug.LogError("[ActiveSession] RetryAsync called with no active session.");
                return;
            }

            var party = new List<UserUnitData>(Current.Party);
            var mode = Current.Mode;

            if (mode == SessionMode.Classic)
            {
                BeginClassic();
                BindParty(party);
            }
            else if (mode == SessionMode.Quest && Current is QuestGameSession quest)
            {
                var questMapId = quest.QuestMapId;
                var difficulty = quest.SelectedDifficulty;
                BeginQuest(questMapId, difficulty);
                BindParty(party);
            }
            else
            {
                UnityEngine.Debug.LogError($"[ActiveSession] RetryAsync unsupported mode: {mode}");
                return;
            }

            await UIManager.ShowLoadingAsync();
            await UIManager.CloseAllUIAsync();
            string address = AddressableKeys.InGame.Get("GameScene");
            await Addressables.LoadSceneAsync(address, LoadSceneMode.Single).ToUniTask();
        }

        public static void Clear()
        {
            Current?.End();
            Current = null;
        }
    }
}
