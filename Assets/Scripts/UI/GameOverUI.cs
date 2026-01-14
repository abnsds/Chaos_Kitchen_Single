using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI recipesDeliveredText;

    private void Start()
    {
        GameMgr.GetInstance().OnStateChanged += GameMgr_OnStateChanged;

        Hide();
    }

    private void Update()
    {
    }

    private void GameMgr_OnStateChanged(object sender, System.EventArgs e)
    {
        if (GameMgr.GetInstance().IsGameOver())
        {
            Show();
            recipesDeliveredText.text = DeliveryMgr.GetInstance().GetSuccessfulRecipesAmount().ToString();

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
