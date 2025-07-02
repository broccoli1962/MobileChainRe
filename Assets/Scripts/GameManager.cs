using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Panel")]
    public Panel panel;
    public SpriteRenderer firePanel;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
    }

    private void Start()
    {
        CreatePanel();
    }

    private void Update()
    {
        
    }

    //패널을 생성하는 기능
    public void CreatePanel()
    {
        Panel newPanel = Instantiate(panel);
        newPanel.SetSprite(firePanel);
    }
}
