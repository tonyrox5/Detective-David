using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class GenericNPC_Controller : MonoBehaviour
{
    private enum State
    {
        GoingToWorkDoor,
        GoingToWork,
        Working,
        GoingHomeFromWork,
        GoingToHomeDoor,
        AtHome
    }

    [Header("Lokasyonlar")]
    [SerializeField] private Transform workLocation;
    [SerializeField] private Transform workDoorstepLocation;
    [SerializeField] private Transform homeLocation;
    [SerializeField] private Transform homeDoorstepLocation; // eve giriþte hedeflenecek kaldýrým noktasý (opsiyonel)
    [SerializeField] private Transform workLookAtTarget;

    [Header("Kapýlar")]
    [SerializeField] private DoorController workDoor;
    [SerializeField] private DoorController homeDoor;

    [Header("Çalýþma Saatleri")]
    [Range(0, 24)] public float workStartHour = 9f;
    [Range(0, 24)] public float workEndHour = 17f;

    [Header("Hareket Ayarlarý")]
    [SerializeField] private float rotationSpeed = 2f;
    [SerializeField] private float arriveTolerance = 0.35f; // stoppingDistance'a eklenecek tolerans

    [Header("Gözlem / Hýrsýzlýk Tespiti")]
    [Tooltip("NPC iþe baþladýðýnda kontrol edeceði önemli eþya noktalarý")]
    [SerializeField] private List<PlacementPoint> importantSpots = new List<PlacementPoint>();
    private bool hasNoticedTheft = false; // bugünkü hýrsýzlýk bayraðý

    [Header("Kimlik")]
    [SerializeField] private string npcID = "npc_001";
    [SerializeField] private string displayName = "NPC";

    // Runtime
    public NavMeshAgent agent;
    private State currentState;
    private bool hasReachedDestination = false;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null) Debug.LogError($"{name}: NavMeshAgent bulunamadý.");

        // Zorunlu referans kontrolleri
        if (workLocation == null) Debug.LogWarning($"{name}: workLocation atanmamýþ.");
        if (workDoorstepLocation == null) Debug.LogWarning($"{name}: workDoorstepLocation atanmamýþ.");
        if (homeLocation == null) Debug.LogWarning($"{name}: homeLocation atanmamýþ.");
        if (workDoor == null) Debug.LogWarning($"{name}: workDoor atanmamýþ.");
        if (homeDoor == null) Debug.LogWarning($"{name}: homeDoor atanmamýþ.");
    }

    private void OnEnable()
    {
        // Zaman atlama olayýna abone ol ? uyku sonrasý hizalama
        if (TimeManager.instance != null)
            TimeManager.instance.OnTimeAdvanced += HandleTimeAdvanced;
    }

    private void OnDisable()
    {
        if (TimeManager.instance != null)
            TimeManager.instance.OnTimeAdvanced -= HandleTimeAdvanced;
    }

    private void Start()
    {
        // Baþlangýç state’ini SAATE göre seç (yakýnlýða göre deðil)
        float now = SafeNow();
        AlignToSchedule(now, dayInc: 0, warp: true);
    }

    private void Update()
    {
        // Çalýþma bitiþ saati geçtiyse ve hâlâ Working ise eve dön
        float now = SafeNow();
        if (currentState == State.Working && IsAfterOrEqual(now, workEndHour))
        {
            GoHome();
            return;
        }

        // Varýþ kontrolü
        if (!agent.pathPending && agent.enabled)
        {
            float stopDist = agent.stoppingDistance + arriveTolerance;
            if (agent.remainingDistance <= stopDist && !hasReachedDestination)
            {
                hasReachedDestination = true;
                OnArrived();
            }
        }
    }

    // --------------------------- ZAMAN / OLAY ---------------------------

    private void HandleTimeAdvanced(float fromHour, float toHour, int dayInc)
    {
        // Gün deðiþtiyse günlük bayraklarý temizle
        if (dayInc > 0) hasNoticedTheft = false;

        // Zýplama sonrasý mevcut saate göre hizala (warp: ev/iþe ýþýnla)
        AlignToSchedule(toHour, dayInc, warp: true);
    }

    private float SafeNow()
    {
        return (TimeManager.instance != null) ? TimeManager.instance.GetCurrentTime() : 8f;
    }

    private bool IsWithinWorkHours(float hour)
    {
        // wrap yok: workStartHour < workEndHour varsayýyoruz (örn 09–17)
        return hour >= workStartHour && hour < workEndHour;
    }

    private bool IsAfterOrEqual(float hour, float when)
    {
        // wrap yok, doðrudan karþýlaþtýr
        return hour >= when;
    }

    /// <summary>
    /// Verilen saate göre state seç ve konumlandýr.
    /// </summary>
    private void AlignToSchedule(float hour, int dayInc, bool warp)
    {
        if (IsWithinWorkHours(hour))
        {
            // Ýþ saatlerinde: çalýþýyor olmalý
            if (warp)
            {
                WarpTo(workLocation != null ? workLocation.position : transform.position);
                // Kapýyý kapat, bakýþ ver, hýrsýzlýk kontrolü
                StartWorking();
            }
            else
            {
                // yürüyerek git
                GoToWork();
            }
        }
        else
        {
            // Ýþ saatleri dýþýnda: evde
            if (warp)
            {
                WarpTo(homeLocation != null ? homeLocation.position : transform.position);
                EnterHome();
            }
            else
            {
                // yürüyerek eve
                GoHome();
            }
        }
    }

    // --------------------------- STATE TRANSITIONS ---------------------------

    private void GoToWork()
    {
        if (workDoorstepLocation == null)
        {
            // doðrudan iþ lokasyonuna git
            currentState = State.GoingToWork;
            SetDestinationSafe(workLocation);
            return;
        }

        currentState = State.GoingToWorkDoor;
        SetDestinationSafe(workDoorstepLocation);
    }

    private void StartWorking()
    {
        currentState = State.Working;

        if (agent != null) agent.updateRotation = true;

        if (workLookAtTarget != null)
            StartCoroutine(RotateTowards(workLookAtTarget));

        if (workDoor != null)
        {
            // Ýçerideyken kapat
            if (!workDoor.IsLocked()) workDoor.CloseDoor();
        }

        // Ýþe baþlarken önemli noktalarý kontrol et
        CheckForMissingItems();
    }

    private void GoHome()
    {
        if (workDoorstepLocation != null)
        {
            currentState = State.GoingHomeFromWork;
            SetDestinationSafe(workDoorstepLocation);
        }
        else
        {
            // kapý noktasý yoksa doðrudan eve
            currentState = State.GoingToHomeDoor;
            SetDestinationSafe(homeDoorstepLocation != null ? homeDoorstepLocation : homeLocation);
        }
    }

    private void EnterHome()
    {
        currentState = State.AtHome;

        // Eve giriþte ev kapýsý kapat
        if (homeDoor != null && homeDoor.IsOpen())
            homeDoor.CloseDoor();

        // Evdeyken NPC’yi görünmez yapmak istiyorsan:
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Yeni gün baþlarken çaðrýlabilir (opsiyonel).
    /// </summary>
    public void WakeUpAndGoToWork()
    {
        // Sabah uyandý ? evde doð
        if (homeLocation != null) transform.position = homeLocation.position;
        gameObject.SetActive(true);

        // Günlük bayraklarý temizle
        hasNoticedTheft = false;

        // Programla hizala
        float now = SafeNow();
        AlignToSchedule(now, dayInc: 0, warp: false);
    }

    // --------------------------- ARRIVAL HANDLER ---------------------------

    private void OnArrived()
    {
        switch (currentState)
        {
            case State.GoingToWorkDoor:
                StartCoroutine(HandleDoorInteraction(
                    door: workDoor,
                    shouldUnlockFirst: (workDoor != null && workDoor.IsLocked()),
                    nextState: State.GoingToWork,
                    nextDestination: workLocation
                ));
                break;

            case State.GoingToWork:
                StartWorking();
                break;

            case State.GoingHomeFromWork:
                // Ýþ kapýsýndan çýk
                StartCoroutine(HandleDoorInteraction(
                    door: workDoor,
                    shouldUnlockFirst: false,
                    nextState: State.GoingToHomeDoor,
                    nextDestination: (homeDoorstepLocation != null ? homeDoorstepLocation : homeLocation)
                ));
                break;

            case State.GoingToHomeDoor:
                // Ev kapýsýndan içeri gir
                StartCoroutine(HandleHomeEnter());
                break;

            default:
                break;
        }
    }

    private IEnumerator HandleDoorInteraction(DoorController door, bool shouldUnlockFirst, State nextState, Transform nextDestination)
    {
        if (agent != null) agent.isStopped = true;

        if (door != null && !door.IsOpen())
        {
            if (shouldUnlockFirst) { door.Unlock(); yield return new WaitForSeconds(0.2f); }
            door.Interact();
            yield return new WaitForSeconds((1.0f / Mathf.Max(0.01f, door.animationSpeed)) + 0.35f);
        }

        currentState = nextState;
        SetDestinationSafe(nextDestination);
        hasReachedDestination = false;

        if (agent != null) agent.isStopped = false;

        // Ýþ kapýsýndan çýkarken, kapýyý kendiliðinden kapat (1–2 sn sonra)
        if (door != null && nextState == State.GoingToHomeDoor)
        {
            yield return new WaitForSeconds(1.25f);
            door.CloseDoor();
        }
    }

    private IEnumerator HandleHomeEnter()
    {
        if (agent != null) agent.isStopped = true;

        if (homeDoor != null && !homeDoor.IsOpen())
        {
            if (homeDoor.IsLocked()) { homeDoor.Unlock(); yield return new WaitForSeconds(0.2f); }
            homeDoor.Interact();
            yield return new WaitForSeconds((1.0f / Mathf.Max(0.01f, homeDoor.animationSpeed)) + 0.35f);
        }

        EnterHome();
        yield return null;
    }

    // --------------------------- HIRSIZLIK KONTROLÜ ---------------------------

    public void CheckForMissingItems()
    {
        if (hasNoticedTheft) return; // ayný gün tekrar etme

        foreach (var spot in importantSpots)
        {
            if (spot != null && spot.itemInSpot == null)
            {
                hasNoticedTheft = true;
                Debug.LogWarning($"{npcID} hýrsýzlýk fark etti! {spot.gameObject.name} noktasýndaki eþya kayýp!");
                // TODO: Buradan diyaloðu/davranýþý tetikle (ör. alarm, guard çaðýrma vs.)
                break;
            }
        }
    }

    // --------------------------- YARDIMCI FONKSÝYONLAR ---------------------------

    private void SetDestinationSafe(Transform target)
    {
        if (agent == null || target == null) return;
        agent.isStopped = false;
        agent.ResetPath();
        agent.SetDestination(target.position);
    }

    private void WarpTo(Vector3 worldPos)
    {
        if (agent == null) { transform.position = worldPos; return; }
        agent.Warp(worldPos);
        agent.ResetPath();
        hasReachedDestination = false;
    }

    private IEnumerator RotateTowards(Transform target)
    {
        if (agent != null) agent.updateRotation = false;

        if (target == null) yield break;

        while (true)
        {
            Vector3 dir = (target.position - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) break;

            Quaternion lookRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * rotationSpeed);

            if (Quaternion.Angle(transform.rotation, lookRot) <= 1f) break;
            yield return null;
        }

        if (agent != null) agent.updateRotation = true;
    }

    // --------------------------- DIÞ API ---------------------------

    public string GetNpcID() => npcID;

    // Kapý scriptlerinin sorabileceði yardýmcýlar:
    public bool IsApproachingWork() => currentState == State.GoingToWorkDoor;
    public bool IsGoingHome() => currentState == State.GoingHomeFromWork || currentState == State.GoingToHomeDoor;
}
