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
    public float screenFill = 0.55f;
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
    private Vector3 originalLocalPos;
    private Quaternion originalLocalRot;
    private Vector3 originalLocalScale;
    private Rigidbody rb;
    private List<Collider> colliders = new List<Collider>();
    private float currentZoom = 1f;
    private Transform runtimeAnchor;
    private Transform mainCameraTransform;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        GetComponentsInChildren(colliders);
        mainCameraTransform = Camera.main.transform;
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
            if (homePoint.placementVisual != null) homePoint.placementVisual.SetActive(false);
        }
        if (homePoint != null && homePoint.itemInSpot == null)
        {
            homePoint.itemInSpot = this.gameObject;
            if (homePoint.placementVisual != null) homePoint.placementVisual.SetActive(false);
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
        originalParent = transform.parent;
        originalLocalPos = transform.localPosition;
        originalLocalRot = transform.localRotation;
        originalLocalScale = transform.localScale;
        currentZoom = 1f;
        SetPhysicsEnabled(false);
        Transform targetAnchor = PrepareAnchor();
        StartCoroutine(MoveToTarget(targetAnchor, moveDuration, () => { inspecting = true; }));
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

    public bool IsTransitioning()
    {
        return transitioning;
    }

    private void HandleInspectionRotate()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        transform.Rotate(mainCameraTransform.up, -horizontalInput * rotateSensitivity * Time.deltaTime, Space.World);
        transform.Rotate(mainCameraTransform.right, verticalInput * rotateSensitivity * Time.deltaTime, Space.World);
    }

    private void HandleInspectionZoom()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) < 0.001f) return;
        currentZoom = Mathf.Clamp(currentZoom + scroll * (zoomSensitivity * Time.deltaTime), zoomRange.x, zoomRange.y);
        transform.localScale = originalLocalScale * currentZoom;
    }

    private IEnumerator MoveToTarget(Transform target, float duration, System.Action onComplete)
    {
        transitioning = true;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        Vector3 startScale = transform.localScale;
        Vector3 endPos = target.position;
        Quaternion endRot = target.rotation;
        Vector3 endScale = originalLocalScale;
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
        transform.SetParent(target, true);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = originalLocalScale;
        transitioning = false;
        onComplete?.Invoke();
    }

    private IEnumerator MoveBack(float duration, System.Action onComplete)
    {
        transitioning = true;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        Vector3 startScale = transform.localScale;
        Vector3 endPos = originalParent.TransformPoint(originalLocalPos);
        Quaternion endRot = originalParent.rotation * originalLocalRot;
        Vector3 endScale = originalLocalScale;
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
        transform.SetParent(originalParent, true);
        transform.localPosition = originalLocalPos;
        transform.localRotation = originalLocalRot;
        transform.localScale = originalLocalScale;
        transitioning = false;
        onComplete?.Invoke();
    }

    private float Smooth01(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * (3f - 2f * x);
    }

    // --- DEÐÝÞÝKLÝK BU FONKSÝYONUN ÝÇÝNDE ---
    private Transform PrepareAnchor()
    {
        if (inspectAnchor != null) return inspectAnchor;

        // Önce transformdan Camera bileþenini alýyoruz
        Camera cam = mainCameraTransform.GetComponent<Camera>();

        if (cam == null) // Güvenlik kontrolü
        {
            runtimeAnchor = new GameObject($"__InspectAnchor_{name}").transform;
            runtimeAnchor.position = transform.position + transform.forward * 0.6f;
            return runtimeAnchor;
        }

        runtimeAnchor = new GameObject($"__InspectAnchor_{name}").transform;

        Bounds totalBounds = new Bounds(transform.position, Vector3.zero);
        bool hasBounds = false;
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            if (!hasBounds) { totalBounds = r.bounds; hasBounds = true; }
            else { totalBounds.Encapsulate(r.bounds); }
        }

        float objectSize = Mathf.Max(totalBounds.size.x, totalBounds.size.y, totalBounds.size.z);

        // Artýk "cam" deðiþkenini kullanarak doðru özelliklere ulaþýyoruz
        float cameraFovToRad = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
        float distance = (objectSize / (2.0f * screenFill)) / Mathf.Tan(cameraFovToRad);

        distance = Mathf.Max(distance, cam.nearClipPlane + minCameraPadding) + extraDistance;

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
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        foreach (var col in colliders)
        {
            if (col == null) continue;
            col.enabled = enabled;
        }
    }
}