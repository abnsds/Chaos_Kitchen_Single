using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectModeUI : MonoBehaviour
{
    [SerializeField] private Button CasualBtn;
    [SerializeField] private Button ChallengingBtn;

    private void Start()
    {
        CasualBtn.onClick.AddListener(() =>
        {
            GameModeMgr.GetInstance().SetGameMode(GameModeMgr.GameMode.Casual);
            ScenesMgr.GetInstance().LoadSceneWithLoadingScene("CasualScene", () => { });
        });

        ChallengingBtn.onClick.AddListener(() =>
        {
            GameModeMgr.GetInstance().SetGameMode(GameModeMgr.GameMode.Challenging);

            ScenesMgr.GetInstance().LoadSceneWithLoadingScene("ChallengingScene", () => { });
        });
        Hide();
        
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }


}
