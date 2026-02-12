using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameWinUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI recipesDeliveredText;
    [SerializeField] private Button nextBtn;
    [SerializeField] private Button quitBtn;

    private void Start()
    {
        if (nextBtn != null)
        {
            nextBtn.onClick.AddListener(OnNextBtnClick);
        }

        if (quitBtn != null)
        {
            quitBtn.onClick.AddListener(OnQuitBtnClick);
        }
        GameMgr.GetInstance().OnGameWin += GameMgr_OnGameWin;



        Hide();
    }

    private void OnQuitBtnClick()
    {
        ScenesMgr.GetInstance().LoadScene("MainMenuScene", () =>
        {

        });
    }

    private void OnNextBtnClick()
    {

        // 使用场景管理器重新加载游戏场景（会先显示加载场景）
        ScenesMgr.GetInstance().ReloadCurrentWithLoadingScene(() => {
            
        });
    }

    private void GameMgr_OnGameWin(object sender, System.EventArgs e)
    {
        if (GameMgr.GetInstance().IsGameWin())
        {
            Show();
            recipesDeliveredText.text = DeliveryMgr.GetInstance().GetTotalScore().ToString();

        }
        else
        {
            Hide();
        }
    }

    private void Hide()
    {
        gameObject.SetActive(false);

    }

    private void Show()
    {
        gameObject.SetActive(true);
    }
}
