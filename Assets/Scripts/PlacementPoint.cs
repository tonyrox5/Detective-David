using UnityEngine;

public class PlacementPoint : MonoBehaviour
{
    [Header("Referanslar")]
    public GameObject itemInSpot = null;
    public GameObject placementVisual;

    [Header("Kural Ayarlarý")]
    public bool isHotelSpot = false;
    [Header("Otomatik Nokta Ayarý")]
    public bool isAutoCreatedReturnPoint = false;
    [Header("Çalýþma Zamaný Durumu")]
    [SerializeField] private string reservedItemID = null;

    private void Start()
    {
        UpdateSpotStatus();
    }

    public void ReserveForItem(string itemId)
    {
        reservedItemID = itemId;
        UpdateSpotStatus();
    }

    public bool CanAcceptItem(GameObject item)
    {
        if (itemInSpot != null || item == null) return false;
        var insp = item.GetComponent<Inspectable>();
        if (insp == null) return false;
        if (isHotelSpot) return true;
        return !string.IsNullOrEmpty(reservedItemID) && reservedItemID == insp.itemID;
    }

    // --- BU FONKSÝYON GÜNCELLENDÝ ---
    public bool PlaceItem(GameObject itemToPlace)
    {
        if (!CanAcceptItem(itemToPlace)) return false;

        itemInSpot = itemToPlace;
        itemToPlace.transform.SetParent(this.transform);
        itemToPlace.transform.localPosition = Vector3.zero;
        itemToPlace.transform.localRotation = Quaternion.identity;
        // YENÝ EKLENEN SATIR: Ölçeði her zaman 1'e sýfýrla
        itemToPlace.transform.localScale = Vector3.one;
        itemToPlace.SetActive(true);

        reservedItemID = null;
        UpdateSpotStatus();
        return true;
    }
    // --- GÜNCELLEMENÝN SONU ---

    public GameObject TakeItem()
    {
        if (itemInSpot == null) return null;
        GameObject itemToReturn = itemInSpot;
        var insp = itemToReturn.GetComponent<Inspectable>();
        reservedItemID = (insp != null) ? insp.itemID : null;
        itemInSpot = null;
        UpdateSpotStatus();
        return itemToReturn;
    }

    private void UpdateSpotStatus()
    {
        if (placementVisual != null)
        {
            placementVisual.SetActive(itemInSpot == null);
        }
    }
}