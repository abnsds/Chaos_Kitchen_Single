using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button playBtn;
    [SerializeField] private Button quitBtn;
    [SerializeField] private GameObject SelectModeUI;
    private void Awake()
    {
        playBtn.onClick.AddListener(() =>
        {
            SelectModeUI.SetActive(true);
            Hide();
        });

        quitBtn.onClick.AddListener(() =>
        {
            Application.Quit();
        });

        Time.timeScale = 1.0f;
    }

   

    private void Hide()
    {
        gameObject.SetActive(false);
    }


}
