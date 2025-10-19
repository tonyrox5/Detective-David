using UnityEngine;

public class PlacementPoint : MonoBehaviour
{
    [Header("Referanslar")]
    public GameObject itemInSpot = null;
    public GameObject placementVisual;

    [Header("Kural Ayarlar�")]
    [Tooltip("Otel odas�ndaki serbest noktalar i�in TRUE yap. Her e�yay� kabul eder.")]
    public bool isHotelSpot = false;
    // PlacementPoint.cs i�ine ek (class i�inde)
    [Header("Otomatik Nokta Ayar�")]
    public bool isAutoCreatedReturnPoint = false;


    [Header("�al��ma Zaman� Durumu")]
    [SerializeField] private string reservedItemID = null; // Bu nokta, al�nd�ktan sonra sadece bu itemID'yi geri kabul eder.

    private void Start()
    {
        UpdateSpotStatus();
    }

    /// <summary>
    /// Bu noktay� belirli bir itemID i�in rezerve eder (ayn� yere geri koyma kural�).
    /// </summary>
    public void ReserveForItem(string itemId)
    {
        reservedItemID = itemId;
        UpdateSpotStatus();
    }

    /// <summary>
    /// Bu nokta, verilen item'i kabul edebilir mi?
    /// Otel noktalar� her �eyi kabul eder; di�erleri sadece reservedItemID ile e�le�eni.
    /// </summary>
    public bool CanAcceptItem(GameObject item)
    {
        if (itemInSpot != null || item == null) return false;

        var insp = item.GetComponent<Inspectable>();
        if (insp == null) return false;

        if (isHotelSpot) return true; // serbest nokta
        return !string.IsNullOrEmpty(reservedItemID) && reservedItemID == insp.itemID;
    }

    /// <summary>
    /// Item'i bu noktaya yerle�tir (�nce CanAcceptItem ile kontrol et).
    /// </summary>
    public bool PlaceItem(GameObject itemToPlace)
    {
        if (!CanAcceptItem(itemToPlace)) return false;

        itemInSpot = itemToPlace;
        itemToPlace.transform.SetParent(this.transform);
        itemToPlace.transform.localPosition = Vector3.zero;
        itemToPlace.transform.localRotation = Quaternion.identity;
        itemToPlace.SetActive(true);

        // Yerine geri konduysa rezervasyon kalkar
        reservedItemID = null;

        UpdateSpotStatus();
        return true;
    }

    /// <summary>
    /// Bu noktadaki item'i al ve bu noktay� al�nan itemID i�in rezerve et.
    /// </summary>
    public GameObject TakeItem()
    {
        if (itemInSpot == null) return null;

        GameObject itemToReturn = itemInSpot;

        // Al�nan e�yan�n ID'sini bu noktaya rezerve et (geri yerine koyulabilsin)
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
            // Bo�sa g�rseli a�; doluysa kapat (istersen burada rezervasyon var/yok diye farkl� stil de uygulayabilirsin)
            placementVisual.SetActive(itemInSpot == null);
        }
    }
}
