using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Inspectable : MonoBehaviour
{
    [Header("Identity")]
    public string itemID = "Default_Item";

    [Header("Pickup Rules")]
    public bool isTakableAtNight = true;

    [Header("Auto Home PlacementPoint")]
    public bool autoCreateHomePoint = true;
    public PlacementPoint placementPointPrefab;
    private PlacementPoint homePoint;

    [Header("Inspection Fit-to-Camera")]
    [Range(0.1f, 0.95f)] public float screenFill = 0.55f;
    public float minCameraPadding = 0.2f;
    public float extraDistance = 0.0f;

    [Header("Inspection")]
    public Transform inspectAnchor;
    public float moveDuration = 0.35f;
    public float rotateSensitivity = 120f;
    public float zoomSensitivity = 2f;
    public Vector2 zoomRange = new Vector2(0.6f, 1.6f);

    private bool inspecting = false;
    private bool transitioning = false;

    private Transform originalParent;
    private Vector3 originalPos;
    private Quaternion originalRot;
    private Vector3 originalLocalScale;
    private Vector3 originalWorldScale;            // NEW: dünya ölçeði
    private Vector3 inspectionBaseLocalScale;      // NEW: anchor altýnda zoom’un tabaný
    private bool hadParent;

    private Rigidbody rb;
    private readonly List<Collider> colliders = new List<Collider>();
    private float currentZoom = 1f;
    private Transform runtimeAnchor;
    private Transform mainCameraTransform;
    private Camera currentCam;

    private void CacheCamera()
    {
        currentCam = Camera.main;
        if (currentCam == null) currentCam = FindFirstObjectByType<Camera>();
        if (currentCam != null) mainCameraTransform = currentCam.transform;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        GetComponentsInChildren<Collider>(colliders);
        CacheCamera();
    }

    void Start()
    {
        homePoint = GetComponentInParent<PlacementPoint>();
        if (autoCreateHomePoint && homePoint == null && placementPointPrefab != null)
        {
            homePoint = Instantiate(placementPointPrefab, transform.position, transform.rotation);
            homePoint.isHotelSpot = false;
            homePoint.isAutoCreatedReturnPoint = true;
            homePoint.itemInSpot = this.gameObject;

            transform.SetParent(homePoint.transform);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            // !!! ÖNEMLÝ: Burada scale’i 1’e ZORLAMAYIN. Dünya ölçeðini bozuyordu.
            // transform.localScale = Vector3.one;

            if (homePoint.placementVisual != null) homePoint.placementVisual.SetActive(false);
        }

        if (homePoint != null && homePoint.itemInSpot == null)
        {
            homePoint.itemInSpot = this.gameObject;
            if (homePoint.placementVisual != null) homePoint.placementVisual.SetActive(false);
        }

        if (homePoint == null && !autoCreateHomePoint)
        {
            Debug.LogWarning($"Inspectable '{gameObject.name}' needs a PlacementPoint parent or Auto Create enabled!", this);
        }
    }

    void Update()
    {
        if (inspecting && !transitioning)
        {
            HandleInspectionRotate();
            HandleInspectionZoom();
        }
    }

    public void StartInspection()
    {
        if (transitioning || inspecting) return;
        CacheCamera();
        if (mainCameraTransform == null) { Debug.LogError("Cannot inspect, no active camera found!"); return; }

        originalParent = transform.parent;
        hadParent = (originalParent != null);

        if (hadParent) { originalPos = transform.localPosition; originalRot = transform.localRotation; }
        else { originalPos = transform.position; originalRot = transform.rotation; }

        originalLocalScale = transform.localScale;
        originalWorldScale = transform.lossyScale; // NEW: dünya ölçeðini kaydet
        currentZoom = 1f;

        SetPhysicsEnabled(false);
        Transform targetAnchor = PrepareAnchor();
        StartCoroutine(MoveToTarget(targetAnchor, moveDuration, () =>
        {
            // Anchor’a geçtikten sonra Unity worldScale’i korur.
            // Biz de o anda oluþan localScale’i zoom tabaný olarak kaydediyoruz.
            inspectionBaseLocalScale = transform.localScale; // NEW
            inspecting = true;
        }));
    }

    public void StopInspection()
    {
        if (transitioning || !inspecting) return;

        inspecting = false;
        StartCoroutine(MoveBack(moveDuration, () =>
        {
            SetPhysicsEnabled(true);
            if (runtimeAnchor != null) Destroy(runtimeAnchor.gameObject);
            runtimeAnchor = null;
        }));
    }

    public bool IsTransitioning() => transitioning;

    private void HandleInspectionRotate()
    {
        if (mainCameraTransform == null) return;
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        transform.Rotate(mainCameraTransform.up, -h * rotateSensitivity * Time.deltaTime, Space.World);
        transform.Rotate(mainCameraTransform.right, v * rotateSensitivity * Time.deltaTime, Space.World);
    }

    private void HandleInspectionZoom()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) < 0.001f) return;

        currentZoom = Mathf.Clamp(currentZoom + scroll * (zoomSensitivity * Time.deltaTime), zoomRange.x, zoomRange.y);

        // ÖNEMLÝ: Zoom’u artýk inspectionBaseLocalScale’e göre yap
        transform.localScale = inspectionBaseLocalScale * currentZoom;
    }

    private IEnumerator MoveToTarget(Transform target, float duration, System.Action onComplete)
    {
        transitioning = true;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        Vector3 startScale = transform.localScale;

        Vector3 endPos = target.position;
        Quaternion endRot = target.rotation;
        Vector3 endScale = startScale; // worldScale korunacak, elle dokunmayacaðýz

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, duration);
            float s = Smooth01(t);
            transform.position = Vector3.Lerp(startPos, endPos, s);
            transform.rotation = Quaternion.Slerp(startRot, endRot, s);
            transform.localScale = Vector3.Lerp(startScale, endScale, s);
            yield return null;
        }

        // WorldPositionStays = true ? world scale korunur
        transform.SetParent(target, true);

        // Burada local’i SIFIRLAMAYINCA pivot kayabilir; isterseniz sadece poz/rot’u sýfýrlayýn:
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        // !!! DÝKKAT: Burada localScale’e DOKUNMUYORUZ. (Büyüme hatasýný yapan satýr buydu)

        transitioning = false;
        onComplete?.Invoke();
    }

    private IEnumerator MoveBack(float duration, System.Action onComplete)
    {
        transitioning = true;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        Vector3 startScale = transform.localScale;

        Vector3 endPos;
        Quaternion endRot;
        if (hadParent && originalParent != null)
        {
            endPos = originalParent.TransformPoint(originalPos);
            endRot = originalParent.rotation * originalRot;
        }
        else
        {
            endPos = originalPos;
            endRot = originalRot;
        }

        // Dünya ölçeðini geri taþýmak için parent’tan ayrýlýp Lerp
        transform.SetParent(null);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, duration);
            float s = Smooth01(t);
            transform.position = Vector3.Lerp(startPos, endPos, s);
            transform.rotation = Quaternion.Slerp(startRot, endRot, s);
            transform.localScale = Vector3.Lerp(startScale, startScale, s); // ölçeði sabit býrak
            yield return null;
        }

        // Nihai yer
        transform.position = endPos;
        transform.rotation = endRot;

        if (hadParent && originalParent != null)
        {
            // Orijinal parent’a dönerken orijinal LOCAL scale’i geri koymak güvenli
            transform.SetParent(originalParent, worldPositionStays: true);
            transform.localPosition = originalPos;
            transform.localRotation = originalRot;
            transform.localScale = originalLocalScale;
        }
        else
        {
            // Parent yoksa dünya ölçeðimizi korumuþtuk; dokunma
        }

        transitioning = false;
        onComplete?.Invoke();
    }

    private float Smooth01(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }

    private Transform PrepareAnchor()
    {
        if (inspectAnchor != null) return inspectAnchor;

        if (currentCam == null)
        {
            runtimeAnchor = new GameObject($"__InspectAnchor_{name}").transform;
            runtimeAnchor.position = transform.position + transform.forward * 0.6f;
            return runtimeAnchor;
        }
        if (runtimeAnchor == null) runtimeAnchor = new GameObject($"__InspectAnchor_{name}").transform;

        // Boyutu renderer bounds + mevcut world scale ile hesapla
        Bounds totalBounds = new Bounds(transform.position, Vector3.zero);
        bool has = false;
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            if (!has) { totalBounds = r.bounds; has = true; }
            else totalBounds.Encapsulate(r.bounds);
        }
        float objectSize = has ? Mathf.Max(totalBounds.size.x, totalBounds.size.y, totalBounds.size.z) : 0.25f;

        float fovRad = (currentCam.fieldOfView * 0.5f) * Mathf.Deg2Rad;
        float distance = (objectSize / (2.0f * screenFill)) / Mathf.Max(0.0001f, Mathf.Tan(fovRad));
        distance = Mathf.Max(distance, currentCam.nearClipPlane + minCameraPadding) + extraDistance;

        runtimeAnchor.position = mainCameraTransform.position + mainCameraTransform.forward * distance;
        runtimeAnchor.rotation = Quaternion.LookRotation(mainCameraTransform.forward, mainCameraTransform.up);

        return runtimeAnchor;
    }

    private void SetPhysicsEnabled(bool enabled)
    {
        if (rb != null)
        {
            rb.isKinematic = !enabled;
            rb.useGravity = enabled;
            if (!enabled)
            {
                rb.linearVelocity = Vector3.zero;          // FIX: linearVelocity deðil
                rb.angularVelocity = Vector3.zero;
            }
        }
        foreach (var col in colliders)
        {
            if (col == null) continue;
            col.enabled = enabled;
        }
    }
}
