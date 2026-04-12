using Backend.Util.Enum;
using System;
using UnityEngine;

public class Panel : MonoBehaviour
{
    public int idx = 0;
    SpriteRenderer panelSprite;
    public PanelType panelTypes;

    public AudioClip popSound;

    public void Awake()
    {
        panelSprite = GetComponent<SpriteRenderer>();
    }
    //생성될 시 sprite를 정하는 기능
    public void SetSprite(int number)
    {
        idx = number;
        panelSprite.sprite = Resources.Load<Sprite>($"panel{idx}");
    }
    //생성될 시 panelType을 정하는 기능
    public void SetTypes(int number)
    {
        if (Enum.IsDefined(typeof(PanelType), number)) {
            panelTypes = (PanelType)number;
        }
        else
        {
            Debug.Log("no type in panel");
        }
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

        //AudioManager.instance.PlayOneShot(popSound, 0.8f);
    }

    public void PopSound()
    {
        
    }
}