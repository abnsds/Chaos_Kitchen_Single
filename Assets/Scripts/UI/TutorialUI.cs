using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialUI : MonoBehaviour
{
    private void Start()
    {
        GameMgr.GetInstance().OnStateChanged += GameMgr_OnStateChanged;
        Show();
    }

    private void GameMgr_OnStateChanged(object sender, System.EventArgs e)
    {
        if(GameMgr.GetInstance().IsCountdownToStartActive())
        {
            Hide();
        }
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
