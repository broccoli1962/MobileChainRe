using Backend.AddressableKey;
using Backend.Object.Management;
using Backend.Util;
using Backend.Util.Interface;
using UnityEngine;

namespace Backend.Object.PanelObject
{
    public class Panel : CachedMonobehaviour, IPanel
    {
        private const float ScpScaleMultiplier = 1.5f;
        private const float TypeSymbolWorldSize = 0.6f;

        public PanelType panelType { get; set; }
        public CrashRank CrashRank { get; private set; }

        [SerializeField] private bool _isProtected;
        [SerializeField] private string popSoundKey;

        public bool IsProtected
        {
            get => _isProtected;
            private set => _isProtected = value;
        }

        private int _remainingHits;

        [SerializeField] private CircleCollider2D circleCollder;
        [SerializeField] private SpriteRenderer panelSprite;
        [SerializeField] private SpriteRenderer protectShield;
        [SerializeField] private SpriteRenderer iconVisual;

        private Vector3 _baseVisualScale;
        private float _baseColliderRadius;
        private bool _basesCached;
        private Sprite _crashPortrait;

        public float Radius => circleCollder.radius;
        public Vector3 SpriteBoundsCenter => panelSprite.bounds.center;


        public void Awake()
        {
            CacheBases();
            ApplyCrashRank(CrashRank.None);
        }

        private void OnEnable()
        {
            RestoreRendererAlpha(panelSprite);
            RestoreRendererAlpha(iconVisual);
        }

        public void SetProtected(bool isProtected)
        {
            IsProtected = isProtected;
            protectShield.gameObject.SetActive(isProtected);
        }

        public void Initialize(PanelType type, bool isProtected)
        {
            _crashPortrait = null;
            ApplyCrashRank(CrashRank.None);
            IsProtected = isProtected;
            _remainingHits = isProtected ? 2 : 1;
            SetColor(type);
            SetProtected(isProtected);
            ResetMotion();
        }

        public void InitializeCrash(PanelType type, CrashRank rank, Sprite portrait)
        {
            _crashPortrait = portrait;
            IsProtected = false;
            _remainingHits = 1;
            ApplyCrashRank(rank);
            SetColor(type);
            SetProtected(false);
            ResetMotion();
        }

        public void SetSprite(PanelType type, Sprite sprite)
        {
            panelType = type;
            panelSprite.sprite = sprite;
            ApplyTypeSymbol(type);
        }

        public void SetColor(PanelType type)
        {
            panelType = type;
            panelSprite.color = ColorUtil.GetPanelTypeColor(type);
            ApplyTypeSymbol(type);
        }

        public void BrokenPanel()
        {
            SetRendererAlpha(panelSprite, 0.5f);
            SetRendererAlpha(iconVisual, 0.5f);
        }

        public void PopSound()
        {
            if (string.IsNullOrEmpty(popSoundKey)) return;
            AudioManager.PlaySfx(popSoundKey);
        }

        private void CacheBases()
        {
            if (_basesCached) return;

            if (panelSprite != null)
                _baseVisualScale = panelSprite.transform.localScale;
            if (circleCollder != null)
                _baseColliderRadius = circleCollder.radius;

            _basesCached = true;
        }

        private void ApplyCrashRank(CrashRank rank)
        {
            CacheBases();
            CrashRank = rank;

            float scale = rank == CrashRank.SCP ? ScpScaleMultiplier : 1f;

            if (panelSprite != null)
                panelSprite.transform.localScale = _baseVisualScale * scale;

            ApplyIconScale(scale);

            if (circleCollder != null)
                circleCollder.radius = _baseColliderRadius * scale;
        }

        private void ApplyTypeSymbol(PanelType type)
        {
            if (iconVisual == null) return;

            Sprite icon = CrashRank != CrashRank.None && _crashPortrait != null
                ? _crashPortrait
                : LoadTypeSymbol(type);

            iconVisual.sprite = icon;
            if (icon != null)
                iconVisual.color = Color.white;
            ApplyIconScale(CrashRank == CrashRank.SCP ? ScpScaleMultiplier : 1f);
        }

        private void ApplyIconScale(float crashScale)
        {
            if (iconVisual == null) return;

            float fit = 1f;
            if (iconVisual.sprite != null)
            {
                Vector2 size = iconVisual.sprite.bounds.size;
                float maxDim = Mathf.Max(size.x, size.y);
                if (maxDim > 0f)
                    fit = TypeSymbolWorldSize / maxDim;
            }

            iconVisual.transform.localScale = new Vector3(fit * crashScale, fit * crashScale, 1f);
        }

        private static Sprite LoadTypeSymbol(PanelType type)
        {
            string keyName = type switch
            {
                PanelType.fire => "Symbol_Fire_NoBorder_SDF",
                PanelType.light => "Symbol_Light_NoBorder_SDF",
                PanelType.water => "Symbol_Water_NoBorder_SDF",
                PanelType.grass => "Symbol_Grass_NoBorder_SDF",
                PanelType.heart => "Symbol_Heart_Ring",
                _ => null
            };
            if (keyName == null) return null;

            string address = AddressableKeys.InGame.Get(keyName);
            if (string.IsNullOrEmpty(address)) return null;

            return ResourceManager.LoadResource<Sprite>(address);
        }

        private static void SetRendererAlpha(SpriteRenderer renderer, float alpha)
        {
            if (renderer == null) return;
            Color color = renderer.color;
            color.a = alpha;
            renderer.color = color;
        }

        private static void RestoreRendererAlpha(SpriteRenderer renderer)
        {
            if (renderer == null) return;
            Color color = renderer.color;
            color.a = 1f;
            renderer.color = color;
        }

        private void ResetMotion()
        {
            var rb = CachedTransform.GetComponent<Rigidbody2D>();
            if (rb == null) return;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }
}
