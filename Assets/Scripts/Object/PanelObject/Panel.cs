using Backend.Util.Enum;
using Backend.Util.Interface;
using UnityEngine;

namespace Backend.Object.PanelObject
{
    public class Panel : MonoBehaviour, IPanel
    {
        public PanelType panelType { get; set; }

        private CircleCollider2D circleCollder;
        private SpriteRenderer panelSprite;

        public Vector3 Position => transform.position;
        public float Radius => circleCollder.radius;

        public AudioClip popSound;

        public void Awake()
        {
            panelSprite = GetComponent<SpriteRenderer>();
            circleCollder = GetComponent<CircleCollider2D>();
        }

        //생성될 시 sprite를 정하는 기능
        public void SetSprite(PanelType type, Sprite sprite)
        {
            panelType = type;
            panelSprite.sprite = sprite;
        }

        public void BreakPanel()
        {
            Destroy(gameObject);
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
