using UnityEngine;

public class HandSystem : MonoBehaviour
{
    public static HandSystem instance;

    [Header("Slotlar ve Tutma Noktas�")]
    public GameObject[] handSlots = new GameObject[3];
    public Transform handHoldPoint;

    private int selectedSlotIndex = -1;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlot(2);
    }

    public bool AddItem(GameObject itemToAdd)
    {
        for (int i = 0; i < handSlots.Length; i++)
        {
            if (handSlots[i] == null)
            {
                handSlots[i] = itemToAdd;
                SelectSlot(i);
                return true;
            }
        }
        Debug.Log("Eller dolu, e�ya al�namad�.");
        return false;
    }

    public void SelectSlot(int index)
    {
        if (index == selectedSlotIndex)
        {
            if (handSlots[index] != null) handSlots[index].SetActive(false);
            selectedSlotIndex = -1;
            return;
        }
        if (selectedSlotIndex != -1 && handSlots[selectedSlotIndex] != null)
        {
            handSlots[selectedSlotIndex].SetActive(false);
        }
        selectedSlotIndex = index;
        if (handSlots[selectedSlotIndex] != null)
        {
            GameObject item = handSlots[selectedSlotIndex];
            item.SetActive(true);
            item.transform.SetParent(handHoldPoint);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;
        }
    }

    public GameObject GetHeldItem()
    {
        if (selectedSlotIndex == -1 || handSlots[selectedSlotIndex] == null)
        {
            return null;
        }
        return handSlots[selectedSlotIndex];
    }

    public void RemoveHeldItem()
    {
        if (selectedSlotIndex != -1)
        {
            handSlots[selectedSlotIndex] = null;
            selectedSlotIndex = -1;
        }
    }

    // --- YEN� EKLENEN FONKS�YON ---
    // Verilen itemID'ye sahip bir e�ya envanterdeki (3 slot) herhangi bir yerde var m�?
    public bool HasItem(string itemID)
    {
        foreach (GameObject item in handSlots)
        {
            // Slot bo� de�ilse
            if (item != null)
            {
                // �zerinde Inspectable script'i var m� ve ID'si e�le�iyor mu?
                if (item.TryGetComponent<Inspectable>(out Inspectable inspectable) && inspectable.itemID == itemID)
                {
                    return true; // E�ya bulundu!
                }
            }
        }
        return false; // E�ya bulunamad�
    }
    // --- YEN� FONKS�YONUN SONU ---
}