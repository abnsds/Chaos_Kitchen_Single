using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeliveryMgrUI : MonoBehaviour
{
    [SerializeField] private Transform container;
    [SerializeField] private Transform recipeTemplate;


    private void Awake()
    {
        recipeTemplate.gameObject.SetActive(false);
    }

    private void Start()
    {
        DeliveryMgr.GetInstance().OnRecipeSpawned += DeliveryMgr_OnRecipeSpawned;
        DeliveryMgr.GetInstance().OnRecipeCompleted += DeliveryMgr_OnRecipeCompleted;
        UpdareVisual();
    }

    private void DeliveryMgr_OnRecipeCompleted(object sender, EventArgs e)
    {
        UpdareVisual();
    }

    private void DeliveryMgr_OnRecipeSpawned(object sender, EventArgs e)
    {
        UpdareVisual();
    }

    private void UpdareVisual()
    {
        foreach (Transform child in container) 
        {
            if (child == recipeTemplate) continue;
            Destroy(child.gameObject);
        }

        foreach (var recipeSO in DeliveryMgr.GetInstance().GetWaitingRecipeSOList())
        {
            Transform recipeTransform = Instantiate(recipeTemplate, container);
            recipeTransform.gameObject.SetActive(true);
            recipeTransform.GetComponent<DeliveryMgrSingleUI>().SetRecipeSO(recipeSO);
        }
    }
}
