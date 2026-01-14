using UnityEngine;

public class KitchenObject : MonoBehaviour {
    [SerializeField] private KitchenObjectSO KitchenObjectSO;
    private IKitchenObjectParent kitchenObjectParent;

    
    public KitchenObjectSO GetKitchenObjectSO()
    {
        return KitchenObjectSO;
    }
    
    
    /// <summary>
    /// 提供给外部，设置的方法
    /// </summary>
    /// <param name="clearCounter"></param>
    public void SetKitchenObjectParent(IKitchenObjectParent IKitchenObjectParent)
    {       
        if(this.kitchenObjectParent != null)
        {
            this.kitchenObjectParent.ClearKitchenObject();
        }
       
        this.kitchenObjectParent = IKitchenObjectParent;

        if (IKitchenObjectParent.HasKitchenObject())
        {
            Debug.LogError("Counter already has a kichenObject");
        }
        IKitchenObjectParent.SetKitchenObject(this);
        transform.parent = IKitchenObjectParent.GetKitchenObjectFollowTransform();
        transform.localPosition = Vector3.zero;
    }
    public IKitchenObjectParent GetKitchenObjectParent()
    {
        return kitchenObjectParent;
    }

    public void DestroySelf()
    {
        kitchenObjectParent.ClearKitchenObject();
        Destroy(gameObject);
    }

    public bool TryGetPlate(out Plate plate)
    {
        if(this is Plate)
        {
            plate = this as Plate;
            return true;
        }
        else
        {
            plate = null;
            return false;
        }
    }

    public static KitchenObject SpawnKitchenObject(KitchenObjectSO kitchenObjectSO, IKitchenObjectParent kitchenObjectParent)
    {
        Transform kitchenObjectTransform = Instantiate(kitchenObjectSO.prefab);

        KitchenObject kitchenObject = kitchenObjectTransform.GetComponent<KitchenObject>();

        kitchenObject.SetKitchenObjectParent(kitchenObjectParent);
        return kitchenObject;

    }
}