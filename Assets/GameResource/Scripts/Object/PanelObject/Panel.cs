using Backend.Util;
using Backend.Util.Enum;
using Backend.Util.Interface;
using UnityEngine;

namespace Backend.Object.PanelObject
{
    public class Panel : CachedMonobehaviour, IPanel
    {
        public PanelType panelType { get; set; }

        private CircleCollider2D circleCollder;
        private SpriteRenderer panelSprite;

        public float Radius => circleCollder.radius;
        public Vector3 SpriteBoundsCenter => panelSprite.bounds.center;

        public AudioClip popSound;

        public void Awake()
        {
            panelSprite = CachedTransform.GetComponentInChildren<SpriteRenderer>(true);
            circleCollder = CachedTransform.GetComponent<CircleCollider2D>();
        }

        private void OnEnable()
        {
            if (panelSprite == null) return;

            Color color = panelSprite.color;
            color.a = 1f;
            panelSprite.color = color;
        }

        public void SetSprite(PanelType type, Sprite sprite)
        {
            panelType = type;
            panelSprite.sprite = sprite;
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
