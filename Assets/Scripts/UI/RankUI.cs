using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RankUI : MonoBehaviour
{
    public static RankUI Instance { get; private set; }

    [Header("休闲模式UI")]
    [SerializeField] private Transform casualContent; // LeftArea→Scroll View→Viewport→Content
    [SerializeField] private GameObject casualRankItemPrefab;
    //[SerializeField] private GameObject casualEmptyTip;

    [Header("挑战模式UI")]
    [SerializeField] private Transform challengingContent; // RightArea→Scroll View→Viewport→Content
    [SerializeField] private GameObject challengingRankItemPrefab;
    //[SerializeField] private GameObject challengingEmptyTip;

    [SerializeField] private Button closeBtn;

    // 适配你的Item结构：0=排名，1=菜品数/分数，2=时间
    private const int RANK_TEXT_INDEX = 0;
    private const int VALUE_TEXT_INDEX = 1;
    private const int TIME_TEXT_INDEX = 2;

    private void Awake()
    {
        Instance = this;
    }
    private void Start()
    {
        closeBtn.onClick.AddListener(OnCloseBtnClick);
        RefreshRankUI();
        Hide();
    }

    public void RefreshRankUI()
    {
        ClearRankItems(casualContent);
        ClearRankItems(challengingContent);
        RenderCasualRank();
        RenderChallengingRank();
    }

    private void RenderCasualRank()
    {
        List<RankData> casualRankList = RankMgr.GetInstance().GetCasualRankList();
        //casualEmptyTip.SetActive(casualRankList.Count == 0);

        for (int i = 0; i < casualRankList.Count; i++)
        {
            RankData data = casualRankList[i];
            GameObject itemObj = Instantiate(casualRankItemPrefab, casualContent);
            itemObj.tag = "RankItem";
            itemObj.name = $"CasualRankItem_{i + 1}";

            // 获取Item下的所有Legacy Text组件
            Text[] textComponents = itemObj.GetComponentsInChildren<Text>();

            if (textComponents.Length > RANK_TEXT_INDEX)
                textComponents[RANK_TEXT_INDEX].text = $"{i + 1}"; // 排名

            if (textComponents.Length > VALUE_TEXT_INDEX)
                textComponents[VALUE_TEXT_INDEX].text = $"{data.dishesCount}"; // 菜品数

            if (textComponents.Length > TIME_TEXT_INDEX)
                textComponents[TIME_TEXT_INDEX].text = data.time; // 时间
        }
    }

    private void RenderChallengingRank()
    {
        List<RankData> challengingRankList = RankMgr.GetInstance().GetChallengingRankList();
        //challengingEmptyTip.SetActive(challengingRankList.Count == 0);

        for (int i = 0; i < challengingRankList.Count; i++)
        {
            RankData data = challengingRankList[i];
            GameObject itemObj = Instantiate(challengingRankItemPrefab, challengingContent);
            itemObj.tag = "RankItem";
            itemObj.name = $"ChallengingRankItem_{i + 1}";

            // 获取Item下的所有Legacy Text组件
            Text[] textComponents = itemObj.GetComponentsInChildren<Text>();

            if (textComponents.Length > RANK_TEXT_INDEX)
                textComponents[RANK_TEXT_INDEX].text = $"{i + 1}"; // 排名

            if (textComponents.Length > VALUE_TEXT_INDEX)
                textComponents[VALUE_TEXT_INDEX].text = $"{data.score}"; // 分数

            if (textComponents.Length > TIME_TEXT_INDEX)
                textComponents[TIME_TEXT_INDEX].text = data.time; // 时间
        }
    }

    private void ClearRankItems(Transform contentTransform)
    {
        for (int i = contentTransform.childCount - 1; i >= 0; i--)
        {
            Transform child = contentTransform.GetChild(i);
            if (child.CompareTag("RankItem"))
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void OnCloseBtnClick()
    {
        Hide();
    }

    private void OnEnable()
    {
        RefreshRankUI();
    }

    private void OnDestroy()
    {
        closeBtn.onClick.RemoveListener(OnCloseBtnClick);
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }
    private void Hide()
    {
        gameObject.SetActive(false);
    }
}