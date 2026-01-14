using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor.Search;
using UnityEngine;

public class GameStartCountdownUI : MonoBehaviour
{
    private const string NUMBER_POP = "NumberPop";
    [SerializeField] private TextMeshProUGUI countdownText;

    private Animator animator;
    private int previousCountdownNumber;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        GameMgr.GetInstance().OnStateChanged += GameMgr_OnStateChanged;

        Hide();
    }

    private void Update()
    {
        int countdownNumber = Mathf.CeilToInt(GameMgr.GetInstance().GetCountdownToStartTimer());
        countdownText.text = countdownNumber.ToString();

        if(previousCountdownNumber != countdownNumber )
        {
            previousCountdownNumber = countdownNumber;
            animator.SetTrigger(NUMBER_POP);
            SoundMgr.GetInstance().PlayCountdownSound();
        }
    }

    private void GameMgr_OnStateChanged(object sender, System.EventArgs e)
    {
        if (GameMgr.GetInstance().IsCountdownToStartActive())
        {
            Show();
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
