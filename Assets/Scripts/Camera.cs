using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [Header("Genel Ayarlar")]
    [SerializeField] private float mouseSensitivity = 100f;
    [SerializeField] private Transform playerBody;

    [Header("Etkile�im Ayarlar�")]
    [SerializeField] private float interactionDistance = 3f;

    private float xRotation = 0f;
    private bool isInspecting = false;
    private Inspectable currentlyInspectedObject;
    private ClassicPlayerMovement playerMovementScript;

    void Start()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
            Cursor.lockState = CursorLockMode.Locked;

        playerMovementScript = GetComponentInParent<ClassicPlayerMovement>();
    }

    void Update()
    {
        // Uyku s�ras�nda etkile�im kapal�
        if (TimeManager.instance != null && TimeManager.instance.IsSleeping) return;

        // E�er DiaManager paneli a��kken bu scriptin �al��mas�n� istemiyorsan,
        // DiaManager taraf�nda sistemleri kapat listesine bu komponenti ekleyebilirsin.

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
        if (playerBody != null)
            playerBody.Rotate(Vector3.up * mouseX);
    }

    private void HandleInteraction()
    {
        RaycastHit hit;
        if (!Physics.Raycast(transform.position, transform.forward, out hit, interactionDistance))
            return;

        // --- D�YALOG ---
        if (hit.collider.TryGetComponent<DiaTrigger>(out DiaTrigger diaTrigger))
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                // NPC kimli�iyle do�ru �ekilde diyalog ba�lat�r (SeenToday haf�zas� �al���r)
                diaTrigger.StartConversation();
            }
            return;
        }

        // --- ETK�LE��M / ENVANTER ---
        float currentTime = (TimeManager.instance != null) ? TimeManager.instance.GetCurrentTime() : 12f;
        bool isNight = (currentTime >= 19f || currentTime < 7f);

        GameObject heldItem = (HandSystem.instance != null) ? HandSystem.instance.GetHeldItem() : null;

        if (heldItem == null)
        {
            // E�ya almak
            if (hit.collider.TryGetComponent<PlacementPoint>(out PlacementPoint pointToTakeFrom) && pointToTakeFrom.itemInSpot != null)
            {
                if (pointToTakeFrom.itemInSpot.TryGetComponent<Inspectable>(out Inspectable item))
                {
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        if (isNight && item.isTakableAtNight)
                        {
                            if (HandSystem.instance != null)
                                HandSystem.instance.AddItem(pointToTakeFrom.TakeItem());
                        }
                        else
                        {
                            StartInspection(item);
                        }
                    }
                }
            }
            // Yatak
            else if (hit.collider.CompareTag("Bed"))
            {
                if (Input.GetKeyDown(KeyCode.E) && TimeManager.instance != null)
                    TimeManager.instance.Sleep();
            }
            // Kap�
            else if (hit.collider.TryGetComponent<DoorController>(out DoorController door))
            {
                if (Input.GetKeyDown(KeyCode.E))
                    door.Interact();
            }
        }
        else
        {
            // Kilitli kap� + anahtar
            if (hit.collider.TryGetComponent<DoorController>(out DoorController lockedDoor) && lockedDoor.IsLocked())
            {
                if (Input.GetKeyDown(KeyCode.E))
                {
                    var insp = heldItem.GetComponent<Inspectable>();
                    string heldItemID = insp != null ? insp.itemID : null;

                    if (!string.IsNullOrEmpty(heldItemID) && heldItemID == lockedDoor.GetRequiredItemID())
                    {
                        lockedDoor.Unlock();
                        lockedDoor.Interact();
                    }
                    else
                    {
                        Debug.Log("Yanl�� anahtar.");
                    }
                }
            }
            // E�ya yerle�tirme
            else if (hit.collider.TryGetComponent<PlacementPoint>(out PlacementPoint pointToPlaceTo) && pointToPlaceTo.itemInSpot == null)
            {
                if (Input.GetKeyDown(KeyCode.E))
                {
                    if (pointToPlaceTo.CanAcceptItem(heldItem))
                    {
                        if (HandSystem.instance != null)
                        {
                            HandSystem.instance.RemoveHeldItem();
                            pointToPlaceTo.PlaceItem(heldItem);
                        }
                    }
                }
            }
        }
    }

    void StartInspection(Inspectable objectToInspect)
    {
        isInspecting = true;
        currentlyInspectedObject = objectToInspect;

        if (playerMovementScript != null)
            playerMovementScript.enabled = false;

        currentlyInspectedObject.StartInspection();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void EndInspection()
    {
        isInspecting = false;

        if (playerMovementScript != null)
            playerMovementScript.enabled = true;

        if (currentlyInspectedObject != null)
            currentlyInspectedObject.StopInspection();

        currentlyInspectedObject = null;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
