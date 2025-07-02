using UnityEngine;

public class Panel : MonoBehaviour
{
    SpriteRenderer panelSprite;
    public void ClickTest()
    {
        Debug.Log("클릭됨");
    }

    //생성될 시 sprite를 정하는 기능
    public void SetSprite(SpriteRenderer sprite)
    {
        panelSprite = sprite;
    }
}