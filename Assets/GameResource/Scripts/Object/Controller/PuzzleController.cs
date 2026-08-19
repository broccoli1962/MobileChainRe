using System;
using System.Threading;
using System.Threading.Tasks;
using Backend.AddressableKey;
using Backend.Object.CharacterObject;
using Backend.Object.GameSystems;
using Backend.Object.Management;
using Backend.Object.Management.Pool;
using Backend.Object.PanelObject;
using Backend.Util;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace Backend.Object.Controller
{
    public class PuzzleController : CachedMonobehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private int maxPanelCount = 30;
        [SerializeField] private float spawnInterval = 0.05f;
        [SerializeField] private int preloadCount = 30;
        [SerializeField] private float spawnWidthMargin = 0.9f;

        [Header("Color Panel Weights (fire, light, water, grass, heart)")]
        [SerializeField] private float[] _baseColorWeights = { 1, 1, 1, 1, 1 };

        [Header("Special Panel Base Rates (0~100)")]
        [SerializeField] private float _baseObstacleRate = 0f;
        [SerializeField] private float _baseProtectionRate = 0f;

        private ChainLine _chainLine = null;
        private float[] _colorSkillShares;
        private float _obstacleSkillBoost;
        private float _protectionSkillBoost;

        private Pooling<Panel> panelPool;
        private CancellationTokenSource spawnCts;
        private readonly CompositeDisposable _disposables = new();

        private void Awake()
        {
            _colorSkillShares = new float[_baseColorWeights.Length];
            InitializeAndSubscribeAsync().Forget();
        }

        private async UniTaskVoid InitializeAndSubscribeAsync()
        {
            await CreateChainLineAsync();
            panelPool = await ObjectPoolManager.GetOrCreatePoolAsync<Panel>(
                AddressableKeys.InGame.Get("Panel"),
                preloadCount,
                parent: CachedTransform,
                defaultCapacity: maxPanelCount,
                maxSize: 100,
                onGet: p => p.gameObject.SetActive(true),
                onRelease: p => p.gameObject.SetActive(false));

            PuzzleSystem.OnPanelBroken = ReleasePanel;
            PuzzleSystem.OnCrashPanelRequested = SpawnCrashPanel;
            PuzzleSystem.SetChainLine(_chainLine);

            GameManager.OnStateChanged
                .Subscribe(OnGameStateChanged)
                .AddTo(_disposables);
        }

        private void OnGameStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.Playing:
                    StartSpawning();
                    break;
                case GameState.GameOver:
                case GameState.Clear:
                case GameState.Pause:
                    StopSpawning();
                    break;
            }
        }

        public void StartSpawning()
        {
            StopSpawning();
            spawnCts = new CancellationTokenSource();
            PanelDropRoutine(spawnCts.Token).Forget();
        }

        public void StopSpawning()
        {
            if (spawnCts != null)
            {
                spawnCts.Cancel();
                spawnCts.Dispose();
                spawnCts = null;
            }
        }

        public void ReleasePanel(Panel panel) => panelPool?.Release(panel);

        private void SpawnCrashPanel(Vector3 pos, PanelType type, CrashRank rank)
        {
            var panel = panelPool?.Get();
            if (panel == null) return;

            panel.transform.position = pos;
            panel.InitializeCrash(type, rank, LoadFrontUnitPortrait());
            PuzzleSystem.RegisterPanel(panel);
        }

        private static Sprite LoadFrontUnitPortrait()
        {
            if (CharacterSystem.Count < 1
                || CharacterSystem.GetCharacter(1) is not CharacterSlot front
                || front.UnitData == null)
                return null;

            string address = AddressableKeys.InGame.Get($"Unit_{front.UnitData.unitId}");
            if (string.IsNullOrEmpty(address)) return null;

            return ResourceManager.LoadResource<Sprite>(address);
        }

        private async UniTaskVoid PanelDropRoutine(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (PuzzleSystem.ActivePanelCount < maxPanelCount && !PuzzleSystem.IsProcessing)
                    SpawnPanel();

                await UniTask.Delay(TimeSpan.FromSeconds(spawnInterval), cancellationToken: token);
            }
        }

        private float GetRandomSpawnX()
        {
            float halfWidth = Camera.main.orthographicSize * Camera.main.aspect * spawnWidthMargin;
            return UnityEngine.Random.Range(-halfWidth, halfWidth);
        }

        private void SpawnPanel()
        {
            var panel = panelPool?.Get();
            if (panel == null) return;

            var pos = CachedTransform.position;
            pos.x = GetRandomSpawnX();
            panel.transform.position = pos;
            var (type, isProtected) = RollPanelType();
            panel.Initialize(type, isProtected);
            PuzzleSystem.RegisterPanel(panel);
        }

        private (PanelType type, bool isProtected) RollPanelType()
        {
            // Stage 1: 방해 패널 점유 판정
            float obstacleRate = Mathf.Clamp(_baseObstacleRate + _obstacleSkillBoost, 0f, 100f);
            if (UnityEngine.Random.Range(0f, 100f) < obstacleRate)
                return (PanelType.obstacle, false);

            // Stage 2: 색상 예산 알고리즘
            int colorCount = _baseColorWeights.Length;

            float totalClaims = 0f;
            foreach (var s in _colorSkillShares) totalClaims += s;

            float[] effectiveWeights = new float[colorCount];

            if (totalClaims >= 100f)
            {
                for (int i = 0; i < colorCount; i++)
                    effectiveWeights[i] = _colorSkillShares[i];
            }
            else
            {
                float remaining = 100f - totalClaims;
                float nonClaimedSum = 0f;
                for (int i = 0; i < colorCount; i++)
                    if (_colorSkillShares[i] <= 0f)
                        nonClaimedSum += _baseColorWeights[i];

                for (int i = 0; i < colorCount; i++)
                {
                    effectiveWeights[i] = _colorSkillShares[i] > 0f
                        ? _colorSkillShares[i]
                        : nonClaimedSum > 0f ? (_baseColorWeights[i] / nonClaimedSum) * remaining : 0f;
                }
            }

            float total = 0f;
            foreach (var w in effectiveWeights) total += w;

            PanelType selectedType = (PanelType)0;
            float colorRoll = UnityEngine.Random.Range(0f, total);
            for (int i = 0; i < colorCount; i++)
            {
                colorRoll -= effectiveWeights[i];
                if (colorRoll < 0f)
                {
                    selectedType = (PanelType)i;
                    break;
                }
            }

            // Stage 3: 보호 수식자 판정
            float protectionRate = Mathf.Clamp(_baseProtectionRate + _protectionSkillBoost, 0f, 100f);
            bool isProtected = UnityEngine.Random.Range(0f, 100f) < protectionRate;

            return (selectedType, isProtected);
        }

        public void SetColorSkillShare(PanelType type, float share)
        {
            int index = (int)type;
            if (index < 0 || index >= _colorSkillShares.Length) return;
            _colorSkillShares[index] = Mathf.Clamp(share, 0f, 100f);
        }

        public void SetObstacleSkillBoost(float boost) =>
            _obstacleSkillBoost = Mathf.Clamp(boost, 0f, 100f);

        public void SetProtectionSkillBoost(float boost) =>
            _protectionSkillBoost = Mathf.Clamp(boost, 0f, 100f);

        public void ClearSkillShare(PanelType type)
        {
            int index = (int)type;
            if (index < 0 || index >= _colorSkillShares.Length) return;
            _colorSkillShares[index] = 0f;
        }

        public void ClearAllSkillShares()
        {
            for (int i = 0; i < _colorSkillShares.Length; i++)
                _colorSkillShares[i] = 0f;
            _obstacleSkillBoost = 0f;
            _protectionSkillBoost = 0f;
        }

        private void OnDestroy()
        {
            StopSpawning();
            _disposables.Dispose();
            
            ObjectPoolManager.ReleasePool<Panel>();

            if (PuzzleSystem.OnPanelBroken == ReleasePanel)
                PuzzleSystem.OnPanelBroken = null;
            if (PuzzleSystem.OnCrashPanelRequested == SpawnCrashPanel)
                PuzzleSystem.OnCrashPanelRequested = null;
        }

        public async UniTask CreateChainLineAsync(){
            var chainPrefab = await ResourceManager.LoadComponentAsync<ChainLine>(AddressableKeys.InGame.Get("ChainLine"));
            _chainLine = Instantiate(chainPrefab, CachedTransform);
        }
    }
}
