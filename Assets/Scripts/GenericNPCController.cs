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
    [SerializeField] private Transform homeDoorstepLocation; // eve giri�te hedeflenecek kald�r�m noktas� (opsiyonel)
    [SerializeField] private Transform workLookAtTarget;

    [Header("Kap�lar")]
    [SerializeField] private DoorController workDoor;
    [SerializeField] private DoorController homeDoor;

    [Header("�al��ma Saatleri")]
    [Range(0, 24)] public float workStartHour = 9f;
    [Range(0, 24)] public float workEndHour = 17f;

    [Header("Hareket Ayarlar�")]
    [SerializeField] private float rotationSpeed = 2f;
    [SerializeField] private float arriveTolerance = 0.35f; // stoppingDistance'a eklenecek tolerans

    [Header("G�zlem / H�rs�zl�k Tespiti")]
    [Tooltip("NPC i�e ba�lad���nda kontrol edece�i �nemli e�ya noktalar�")]
    [SerializeField] private List<PlacementPoint> importantSpots = new List<PlacementPoint>();
    private bool hasNoticedTheft = false; // bug�nk� h�rs�zl�k bayra��

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
        if (agent == null) Debug.LogError($"{name}: NavMeshAgent bulunamad�.");

        // Zorunlu referans kontrolleri
        if (workLocation == null) Debug.LogWarning($"{name}: workLocation atanmam��.");
        if (workDoorstepLocation == null) Debug.LogWarning($"{name}: workDoorstepLocation atanmam��.");
        if (homeLocation == null) Debug.LogWarning($"{name}: homeLocation atanmam��.");
        if (workDoor == null) Debug.LogWarning($"{name}: workDoor atanmam��.");
        if (homeDoor == null) Debug.LogWarning($"{name}: homeDoor atanmam��.");
    }

    private void OnEnable()
    {
        // Zaman atlama olay�na abone ol ? uyku sonras� hizalama
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
        // Ba�lang�� state�ini SAATE g�re se� (yak�nl��a g�re de�il)
        float now = SafeNow();
        AlignToSchedule(now, dayInc: 0, warp: true);
    }

    private void Update()
    {
        // �al��ma biti� saati ge�tiyse ve h�l� Working ise eve d�n
        float now = SafeNow();
        if (currentState == State.Working && IsAfterOrEqual(now, workEndHour))
        {
            GoHome();
            return;
        }

        // Var�� kontrol�
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
        // G�n de�i�tiyse g�nl�k bayraklar� temizle
        if (dayInc > 0) hasNoticedTheft = false;

        // Z�plama sonras� mevcut saate g�re hizala (warp: ev/i�e ���nla)
        AlignToSchedule(toHour, dayInc, warp: true);
    }

    private float SafeNow()
    {
        return (TimeManager.instance != null) ? TimeManager.instance.GetCurrentTime() : 8f;
    }

    private bool IsWithinWorkHours(float hour)
    {
        // wrap yok: workStartHour < workEndHour varsay�yoruz (�rn 09�17)
        return hour >= workStartHour && hour < workEndHour;
    }

    private bool IsAfterOrEqual(float hour, float when)
    {
        // wrap yok, do�rudan kar��la�t�r
        return hour >= when;
    }

    /// <summary>
    /// Verilen saate g�re state se� ve konumland�r.
    /// </summary>
    private void AlignToSchedule(float hour, int dayInc, bool warp)
    {
        if (IsWithinWorkHours(hour))
        {
            // �� saatlerinde: �al���yor olmal�
            if (warp)
            {
                WarpTo(workLocation != null ? workLocation.position : transform.position);
                // Kap�y� kapat, bak�� ver, h�rs�zl�k kontrol�
                StartWorking();
            }
            else
            {
                // y�r�yerek git
                GoToWork();
            }
        }
        else
        {
            // �� saatleri d���nda: evde
            if (warp)
            {
                WarpTo(homeLocation != null ? homeLocation.position : transform.position);
                EnterHome();
            }
            else
            {
                // y�r�yerek eve
                GoHome();
            }
        }
    }

    // --------------------------- STATE TRANSITIONS ---------------------------

    private void GoToWork()
    {
        if (workDoorstepLocation == null)
        {
            // do�rudan i� lokasyonuna git
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
            // ��erideyken kapat
            if (!workDoor.IsLocked()) workDoor.CloseDoor();
        }

        // ��e ba�larken �nemli noktalar� kontrol et
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
            // kap� noktas� yoksa do�rudan eve
            currentState = State.GoingToHomeDoor;
            SetDestinationSafe(homeDoorstepLocation != null ? homeDoorstepLocation : homeLocation);
        }
    }

    private void EnterHome()
    {
        currentState = State.AtHome;

        // Eve giri�te ev kap�s� kapat
        if (homeDoor != null && homeDoor.IsOpen())
            homeDoor.CloseDoor();

        // Evdeyken NPC�yi g�r�nmez yapmak istiyorsan:
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Yeni g�n ba�larken �a�r�labilir (opsiyonel).
    /// </summary>
    public void WakeUpAndGoToWork()
    {
        // Sabah uyand� ? evde do�
        if (homeLocation != null) transform.position = homeLocation.position;
        gameObject.SetActive(true);

        // G�nl�k bayraklar� temizle
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
                // �� kap�s�ndan ��k
                StartCoroutine(HandleDoorInteraction(
                    door: workDoor,
                    shouldUnlockFirst: false,
                    nextState: State.GoingToHomeDoor,
                    nextDestination: (homeDoorstepLocation != null ? homeDoorstepLocation : homeLocation)
                ));
                break;

            case State.GoingToHomeDoor:
                // Ev kap�s�ndan i�eri gir
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

        // �� kap�s�ndan ��karken, kap�y� kendili�inden kapat (1�2 sn sonra)
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

    // --------------------------- HIRSIZLIK KONTROL� ---------------------------

    public void CheckForMissingItems()
    {
        if (hasNoticedTheft) return; // ayn� g�n tekrar etme

        foreach (var spot in importantSpots)
        {
            if (spot != null && spot.itemInSpot == null)
            {
                hasNoticedTheft = true;
                Debug.LogWarning($"{npcID} h�rs�zl�k fark etti! {spot.gameObject.name} noktas�ndaki e�ya kay�p!");
                // TODO: Buradan diyalo�u/davran��� tetikle (�r. alarm, guard �a��rma vs.)
                break;
            }
        }
    }

    // --------------------------- YARDIMCI FONKS�YONLAR ---------------------------

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

    // --------------------------- DI� API ---------------------------

    public string GetNpcID() => npcID;

    // Kap� scriptlerinin sorabilece�i yard�mc�lar:
    public bool IsApproachingWork() => currentState == State.GoingToWorkDoor;
    public bool IsGoingHome() => currentState == State.GoingHomeFromWork || currentState == State.GoingToHomeDoor;
}
