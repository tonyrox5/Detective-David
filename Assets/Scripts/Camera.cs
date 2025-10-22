using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [Header("Genel Ayarlar")]
    [SerializeField] private float mouseSensitivity = 100f;
    [SerializeField] private Transform playerBody;

    [Header("Etkileþim Ayarlarý")]
    [SerializeField] private float interactionDistance = 3f;

    private float xRotation = 0f;
    private bool isInspecting = false;
    private Inspectable currentlyInspectedObject;
    private ClassicPlayerMovement playerMovementScript;

    void Start()
    {
        if (Cursor.lockState != CursorLockMode.Locked) { Cursor.lockState = CursorLockMode.Locked; }
        playerMovementScript = GetComponentInParent<ClassicPlayerMovement>();
    }

    void Update()
    {
        if (TimeManager.instance != null && TimeManager.instance.IsSleeping) return; // yeni
        if (DialogueUIManager.instance != null && DialogueUIManager.instance.IsDialogueActive()) return;

        if (isInspecting)
        {
            if (Input.GetKeyDown(KeyCode.E) && !currentlyInspectedObject.IsTransitioning())
                EndInspection();
        }
        else
        {
            HandleMouseLook();
            HandleInteraction();
        }
    }


    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }

    // --- BU FONKSÝYON GÜNCELLENDÝ ---
    private void HandleInteraction()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, interactionDistance))
        {
            if (hit.collider.TryGetComponent<DialogueTrigger>(out DialogueTrigger dialogueTrigger))
            {
                if (Input.GetKeyDown(KeyCode.T))
                {
                    // YENÝ MANTIK: Trigger'dan doðru konuþmayý iste
                    DialogueNode conversationToStart = dialogueTrigger.GetConversation();
                    if (conversationToStart != null)
                    {
                        DialogueUIManager.instance.StartDialogue(conversationToStart);
                    }
                    else
                    {
                        Debug.Log("Bu karakterin þu an söyleyecek bir þeyi yok.");
                    }
                }
            }
            else
            {
                float currentTime = TimeManager.instance.GetCurrentTime();
                bool isNight = (currentTime >= 19 || currentTime < 7);
                GameObject heldItem = HandSystem.instance.GetHeldItem();

                if (heldItem == null)
                {
                    if (hit.collider.TryGetComponent<PlacementPoint>(out PlacementPoint pointToTakeFrom) && pointToTakeFrom.itemInSpot != null)
                    {
                        if (pointToTakeFrom.itemInSpot.TryGetComponent<Inspectable>(out Inspectable item))
                        {
                            if (Input.GetKeyDown(KeyCode.E))
                            {
                                if (isNight && item.isTakableAtNight) { HandSystem.instance.AddItem(pointToTakeFrom.TakeItem()); }
                                else { StartInspection(item); }
                            }
                        }
                    }
                    else if (hit.collider.CompareTag("Bed")) { if (Input.GetKeyDown(KeyCode.E)) { TimeManager.instance.Sleep(); } }
                    else if (hit.collider.TryGetComponent<DoorController>(out DoorController door)) { if (Input.GetKeyDown(KeyCode.E)) { door.Interact(); } }
                }
                else
                {
                    if (hit.collider.TryGetComponent<DoorController>(out DoorController lockedDoor) && lockedDoor.IsLocked())
                    {
                        if (Input.GetKeyDown(KeyCode.E))
                        {
                            string heldItemID = heldItem.GetComponent<Inspectable>().itemID;
                            if (heldItemID == lockedDoor.GetRequiredItemID()) { lockedDoor.Unlock(); lockedDoor.Interact(); }
                            else { Debug.Log("Yanlýþ anahtar."); }
                        }
                    }
                    else if (hit.collider.TryGetComponent<PlacementPoint>(out PlacementPoint pointToPlaceTo) && pointToPlaceTo.itemInSpot == null)
                    {
                        if (Input.GetKeyDown(KeyCode.E)) { if (pointToPlaceTo.CanAcceptItem(heldItem)) { HandSystem.instance.RemoveHeldItem(); pointToPlaceTo.PlaceItem(heldItem); } }
                    }
                }
            }
        }
    }

    void StartInspection(Inspectable objectToInspect) { isInspecting = true; currentlyInspectedObject = objectToInspect; playerMovementScript.enabled = false; currentlyInspectedObject.StartInspection(); Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    void EndInspection() { isInspecting = false; playerMovementScript.enabled = true; currentlyInspectedObject.StopInspection(); currentlyInspectedObject = null; Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
}