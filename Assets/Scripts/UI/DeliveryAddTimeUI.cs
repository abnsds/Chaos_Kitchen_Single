using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DeliveryAddTimeUI : MonoBehaviour
{
    private const string APPEAR = "Appear";
    [SerializeField] private TextMeshProUGUI TimeTxt;
    private Animator animator;
    private float addedTime;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }
    void Start()
    {
        DeliveryMgr.GetInstance().OnRecipeSuccess += DeliveryMgr_OnRecipeSuccess;
        Hide();
    }

    private void DeliveryMgr_OnRecipeSuccess(object sender, System.EventArgs e)
    {
        Show();
        animator.SetTrigger(APPEAR);
        addedTime = GameMgr.GetInstance().LastAddedRandomTime;
        int intTime = Mathf.RoundToInt(addedTime);
        TimeTxt.text = $"+{intTime}s";
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
