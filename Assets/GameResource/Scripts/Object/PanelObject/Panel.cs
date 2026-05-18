using Backend.Util;
using Backend.Util.Enum;
using Backend.Util.Interface;
using UnityEngine;

namespace Backend.Object.PanelObject
{
    public class Panel : CachedMonobehaviour, IPanel
    {
        public PanelType panelType { get; set; }

        [SerializeField] private bool _isProtected;
        public bool IsProtected
        {
            get => _isProtected;
            private set => _isProtected = value;
        }

        private int _remainingHits;

        private CircleCollider2D circleCollder;
        [SerializeField] private SpriteRenderer panelSprite;
        [SerializeField] private SpriteRenderer protectShield;

        public float Radius => circleCollder.radius;
        public Vector3 SpriteBoundsCenter => panelSprite.bounds.center;

        public AudioClip popSound;

        public void Awake()
        {
            circleCollder = CachedTransform.GetComponent<CircleCollider2D>();
        }

        private void OnEnable()
        {
            if (panelSprite == null) return;

            Color color = panelSprite.color;
            color.a = 1f;
            panelSprite.color = color;
        }

        public void SetProtected(bool isProtected)
        {
            IsProtected = isProtected;
            protectShield.gameObject.SetActive(isProtected);
        }

        public void Initialize(PanelType type, bool isProtected)
        {
            IsProtected = isProtected;
            _remainingHits = isProtected ? 2 : 1;
            SetColor(type);
            SetProtected(isProtected);
        }

        public void SetSprite(PanelType type, Sprite sprite)
        {
            panelType = type;
            panelSprite.sprite = sprite;
        }

        public void SetColor(PanelType type)
        {
            panelType = type;
            panelSprite.color = type switch
            {
                PanelType.fire  => new Color(1f,   0.3f, 0.1f),
                PanelType.light => new Color(1f,   1f,   0.2f),
                PanelType.water => new Color(0.2f, 0.5f, 1f),
                PanelType.grass => new Color(0.2f, 0.8f, 0.2f),
                PanelType.heart => new Color(1f,   0.2f, 0.5f),
                _               => Color.white,
            };
        }

        public void BrokenPanel()
        {
            Color color = panelSprite.color;
            color.a = 0.5f;
            panelSprite.color = color;
        }

        public void PopSound()
        {

        }
    }
}
