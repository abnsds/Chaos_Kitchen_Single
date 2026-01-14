using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlateCompleteVisual : MonoBehaviour
{
    [Serializable]
    public struct KitchenObjectSO_GameObject
    {
        public KitchenObjectSO kitchenObjectSO;
        public GameObject gameObject;
    }


    [SerializeField] private Plate plate;

    [SerializeField] private List<KitchenObjectSO_GameObject> kitchenObjectSOGameObjectList;

    private void Start()
    {
        plate.OnIngredientAdded += Plate_OnIngredientAdded;

        foreach (var kitchenObjectSOGameObject in kitchenObjectSOGameObjectList)
        {
            
            kitchenObjectSOGameObject.gameObject.SetActive(false);
            
        }
    }

    private void Plate_OnIngredientAdded(object sender, Plate.OnIngredientAddedEventArgs e)
    {
        foreach(var kitchenObjectSOGameObject in kitchenObjectSOGameObjectList)
        {
            if(kitchenObjectSOGameObject.kitchenObjectSO == e.KitchenObjectSO)
            {
                kitchenObjectSOGameObject.gameObject.SetActive(true);
            }
        }
        
    }
}
