using UnityEngine;

public class PlacementPoint : MonoBehaviour
{
    [Header("Referanslar")]
    public GameObject itemInSpot = null;
    public GameObject placementVisual;

    [Header("Kural Ayarlarý")]
    [Tooltip("Otel odasýndaki serbest noktalar için TRUE yap. Her eþyayý kabul eder.")]
    public bool isHotelSpot = false;
    // PlacementPoint.cs içine ek (class içinde)
    [Header("Otomatik Nokta Ayarý")]
    public bool isAutoCreatedReturnPoint = false;


    [Header("Çalýþma Zamaný Durumu")]
    [SerializeField] private string reservedItemID = null; // Bu nokta, alýndýktan sonra sadece bu itemID'yi geri kabul eder.

    private void Start()
    {
        UpdateSpotStatus();
    }

    /// <summary>
    /// Bu noktayý belirli bir itemID için rezerve eder (ayný yere geri koyma kuralý).
    /// </summary>
    public void ReserveForItem(string itemId)
    {
        reservedItemID = itemId;
        UpdateSpotStatus();
    }

    /// <summary>
    /// Bu nokta, verilen item'i kabul edebilir mi?
    /// Otel noktalarý her þeyi kabul eder; diðerleri sadece reservedItemID ile eþleþeni.
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
    /// Item'i bu noktaya yerleþtir (önce CanAcceptItem ile kontrol et).
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
    /// Bu noktadaki item'i al ve bu noktayý alýnan itemID için rezerve et.
    /// </summary>
    public GameObject TakeItem()
    {
        if (itemInSpot == null) return null;

        GameObject itemToReturn = itemInSpot;

        // Alýnan eþyanýn ID'sini bu noktaya rezerve et (geri yerine koyulabilsin)
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
            // Boþsa görseli aç; doluysa kapat (istersen burada rezervasyon var/yok diye farklý stil de uygulayabilirsin)
            placementVisual.SetActive(itemInSpot == null);
        }
    }
}
