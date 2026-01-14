using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GamePauseUI : MonoBehaviour
{
    [SerializeField] private Button resumeBtn;
    [SerializeField] private Button mainMenuBtn;
    [SerializeField] private Button OptionsBtn;


    private void Awake()
    {
        resumeBtn.onClick.AddListener(() =>
        {
            GameMgr.GetInstance().PauseGame();
        });
        mainMenuBtn.onClick.AddListener(() =>
        {
            ScenesMgr.GetInstance().LoadScene("MainMenuScene",() => { });
        });
        OptionsBtn.onClick.AddListener(() =>
        {
            Hide();
            OptionsUI.Instance.Show(Show);
        });



    }
    private void Start()
    {
        GameMgr.GetInstance().OnGamePaused += GameMgr_OnGamePaused;
        GameMgr.GetInstance().OnGameUnpaused += GameMgr_OnGameUnpaused;

        Hide();

    }

    private void GameMgr_OnGameUnpaused(object sender, EventArgs e)
    {
        Hide();
    }

    private void GameMgr_OnGamePaused(object sender, System.EventArgs e)
    {
        Show();
    }

    private void Show()
    {
        gameObject.SetActive(true);

        //resumeBtn.Select();
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
