using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public enum PanelType
{
    fire,
    light,
    water,
    grass,
    heart
}

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Panel")]
    public Panel panel;
    private List<Panel> nearPanels = new();
    private List<List<Panel>> chainSequence = new();
    private List<Panel> panelGarbage = new(); //후처리용
    private Dictionary<Panel, PanelType> panels = new();
    public int maxPanelCount = 30;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        Application.targetFrameRate = 60;
    }

    private void Start()
    {
        StartCoroutine(PanelDrop());
    }

    private void Update()
    {
        Click();
    }

    IEnumerator PanelDrop()
    {
        while (true)
        {
            if(panels.Count == maxPanelCount)
            {
                StopCoroutine(PanelDrop());
            }
            else
            {
                CreatePanel();
            }
            yield return new WaitForSeconds(0.05f);
        }
    }

    //패널을 생성하는 기능
    private void CreatePanel()
    {
        Panel newPanel = Instantiate(panel);
        int rand = Random.Range(0, 5);
        newPanel.SetSprite(rand);
        newPanel.SetTypes(rand);
        panels.Add(newPanel, newPanel.panelTypes); //생성된 패널들을 리스트에 담기
    }

    private void Click()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit2D ray = Physics2D.GetRayIntersection(Camera.main.ScreenPointToRay(Input.mousePosition));
            if (ray.transform.CompareTag("Panel"))
            {
                Panel p = ray.transform.GetComponent<Panel>();
                if (p != null) {
                    nearPanels = GetNearPanel(p, SameColor(p), new List<Panel>());
                    SortChainSequence();
                    RemoveChainSequence();
                }
            }
        }
    }

    private List<Panel> SameColor(Panel clickedPanel)
    {
        //같은 타입의 패널끼리 모은 패널 리스트 생성
        List<Panel> sameColor = new();
        foreach (var panel in GameManager.instance.panels)
        {
            PanelType kvalue = panel.Value;
            if (GameManager.instance.panels.TryGetValue(clickedPanel, out PanelType value) && kvalue == value)
            {
                sameColor.Add(panel.Key);
            }
        }
        return sameColor;
    }


    private void SortChainSequence()
    {
        if (nearPanels.Count == 0) return;
        chainSequence.Clear();

        Queue<Panel> queue = new();
        HashSet<Panel> visitPanel = new(); //중복 확인

        Panel startPanel = nearPanels[0];
        
        queue.Enqueue(startPanel);
        visitPanel.Add(startPanel);

        while (queue.Count > 0)
        {
            int panelCount = queue.Count;
            List<Panel> currentPanelList = new();

            for(int i = 0; i < panelCount; i++)
            {
                Panel currentPanel = queue.Dequeue();
                currentPanelList.Add(currentPanel);

                foreach (Panel near in GetNextPanel(currentPanel))
                {
                    if (!visitPanel.Contains(near))
                    {
                        visitPanel.Add(near);
                        queue.Enqueue(near);
                    }
                }
            }
            chainSequence.Add(currentPanelList);
        }
    }

    private void RemoveChainSequence()
    {
        if(chainSequence.Count > 0)
        {
            List<Panel> list = chainSequence[0];
            chainSequence.RemoveAt(0);
            foreach(Panel obj in list)
            {
                obj.BrokenPanel();
                panelGarbage.Add(obj);
            }
            Invoke("RemoveChainSequence", 0.2f);
        }
        else
        {
            DestroyPanel();
        }
    }

    private void DestroyPanel()
    {
        foreach(Panel obj in panelGarbage)
        {
            panels.Remove(obj);
            obj.BreakPanel();
        }
        panelGarbage.Clear();
    }

    //가까운 패널 반환
    private List<Panel> GetNextPanel(Panel obj)
    {
        List<Panel> list = new List<Panel>();
        foreach (Panel obj2 in nearPanels)
        {
            if (FindNearPanel(obj, obj2)) list.Add(obj2);
        }
        return list;
    }

    //가까운 전체 패널 리스트 반환
    private List<Panel> GetNearPanel(Panel p, List<Panel> sameList, List<Panel> sortedList)
    {
        sortedList.Add(p);
        foreach (Panel panel in sameList)
        {
            if (!sortedList.Contains(panel) && FindNearPanel(p, panel))
            {
                GetNearPanel(panel, sameList, sortedList);
            }
        }
        return sortedList;
    }

    private Boolean FindNearPanel(Panel p1, Panel p2)
    {
        SpriteRenderer render1 = p1.GetComponent<SpriteRenderer>();
        SpriteRenderer render2 = p2.GetComponent<SpriteRenderer>();

        CircleCollider2D co1 = p1.GetComponent<CircleCollider2D>();
        CircleCollider2D co2 = p2.GetComponent<CircleCollider2D>();

        //실제 범위
        float a = co1.radius / 0.5f;
        float b = co2.radius / 0.5f;

        //이미지의 중심점
        Vector3 center1 = render1.bounds.center;
        Vector3 center2 = render2.bounds.center;

        //크기가 다른 패널끼리 탐색가능하게 설정
        return Vector3.Distance(center1, center2) < 1.3 * ((a + b) / 2f);
    }
}
