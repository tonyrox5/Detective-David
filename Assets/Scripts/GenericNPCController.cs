using System.Collections;
using System.Collections.Generic; // List için bu gerekli
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class GenericNPC_Controller : MonoBehaviour
{
    private enum State { GoingToWorkDoor, GoingToWork, Working, GoingHomeFromWork, GoingToHomeDoor, AtHome }

    [Header("Yapay Zeka Ayarlarý")]
    [SerializeField] private Transform workLocation;
    [SerializeField] private Transform workDoorstepLocation;
    [SerializeField] private Transform homeLocation;
    [SerializeField] private Transform workLookAtTarget;
    [SerializeField] private float rotationSpeed = 2f;

    // --- YENÝ EKLENEN BÖLÜM: HIRSIZLIK TESPÝTÝ ---
    [Header("Gözlem Ayarlarý")]
    [Tooltip("Bu NPC'nin iþe baþladýðýnda kontrol edeceði önemli eþya noktalarý.")]
    [SerializeField] private List<PlacementPoint> importantSpots;
    private bool hasNoticedTheft = false; // Hýrsýzlýðý fark etti mi?
    // --- YENÝ BÖLÜMÜN SONU ---

    [Header("Kimlik ve Kapýlar")]
    [SerializeField] private string npcID = "";
    [SerializeField] private DoorController workDoor;
    [SerializeField] private DoorController homeDoor;

    private NavMeshAgent agent;
    private State currentState;
    private bool hasReachedDestination = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        float distanceToWork = Vector3.Distance(transform.position, workLocation.position);
        float distanceToHome = Vector3.Distance(transform.position, homeLocation.position);
        if (distanceToWork < distanceToHome) { currentState = State.Working; StartWorking(); }
        else { currentState = State.AtHome; gameObject.SetActive(false); }
    }

    void Update()
    {
        float currentTime = TimeManager.instance.GetCurrentTime();

        if (currentTime >= 17 && currentState == State.Working)
        {
            GoHome();
            return;
        }

        if (!agent.pathPending && agent.remainingDistance < 0.5f && !hasReachedDestination)
        {
            hasReachedDestination = true;

            if (currentState == State.GoingToWorkDoor)
            {
                StartCoroutine(HandleDoorInteraction(workDoor, workDoor.IsLocked(), State.GoingToWork, workLocation));
            }
            else if (currentState == State.GoingHomeFromWork)
            {
                StartCoroutine(HandleDoorInteraction(workDoor, false, State.GoingToHomeDoor, homeLocation));
            }
            else if (currentState == State.GoingToHomeDoor)
            {
                EnterHome();
            }
            else if (currentState == State.GoingToWork)
            {
                // Ýþe varýnca bu fonksiyon çaðrýlacak
                StartWorking();
            }
        }
    }

    private IEnumerator HandleDoorInteraction(DoorController door, bool shouldUnlockFirst, State nextState, Transform nextDestination)
    {
        agent.isStopped = true;

        if (!door.IsOpen())
        {
            if (shouldUnlockFirst) { door.Unlock(); yield return new WaitForSeconds(0.2f); }
            door.Interact();
            yield return new WaitForSeconds((1.0f / door.animationSpeed) + 0.5f);
        }

        currentState = nextState;
        agent.SetDestination(nextDestination.position);
        hasReachedDestination = false;
        agent.isStopped = false;

        if (nextState == State.GoingToHomeDoor)
        {
            yield return new WaitForSeconds(1.5f);
            door.CloseDoor();
        }
    }

    public void WakeUpAndGoToWork()
    {
        transform.position = homeLocation.position;
        gameObject.SetActive(true);
        currentState = State.GoingToWorkDoor;
        agent.SetDestination(workDoorstepLocation.position);
        hasReachedDestination = false;
        // Uyandýðýnda hýrsýzlýk fark etme durumunu sýfýrla (yeni gün)
        hasNoticedTheft = false;
    }

    private void GoHome()
    {
        agent.updateRotation = true;
        currentState = State.GoingHomeFromWork;
        agent.SetDestination(workDoorstepLocation.position);
        hasReachedDestination = false;
    }

    private void EnterHome()
    {
        currentState = State.AtHome;
        gameObject.SetActive(false);
    }

    // --- BU FONKSÝYON GÜNCELLENDÝ (HIRSIZLIK KONTROLÜ EKLENDÝ) ---
    private void StartWorking()
    {
        currentState = State.Working;

        if (workLookAtTarget != null)
        {
            StartCoroutine(RotateTowards(workLookAtTarget));
        }

        if (workDoor != null) { workDoor.CloseDoor(); }

        // YENÝ: Ýþe baþladýðýnda, önemli noktalarý kontrol et.
        CheckForMissingItems();
    }
    // --- GÜNCELLEMENÝN SONU ---

    // --- YENÝ EKLENEN FONKSÝYON ---
    // Önemli eþya noktalarýný kontrol eden fonksiyon
    public void CheckForMissingItems()
    {
        // Eðer zaten bir hýrsýzlýk fark ettiyse, tekrar kontrol etme (o gün için)
        if (hasNoticedTheft) return;

        // Önemli noktalar listesindeki her bir yuvayý kontrol et
        foreach (var spot in importantSpots)
        {
            // Eðer yuva BOÞSA...
            if (spot != null && spot.itemInSpot == null)
            {
                // HIRSIZLIK TESPÝT EDÝLDÝ!
                hasNoticedTheft = true;
                Debug.LogWarning(npcID + " bir hýrsýzlýk fark etti! " + spot.gameObject.name + " noktasýndaki eþya kayýp!");

                // Ýleride NPC tepkisi burada tetiklenecek (örneðin diyaloðu deðiþebilir)

                break; // Bir tane bulmak yeterli, döngüden çýk.
            }
        }
    }
    // --- YENÝ FONKSÝYONUN SONU ---

    private IEnumerator RotateTowards(Transform target)
    {
        agent.updateRotation = false;
        Vector3 direction = (target.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        while (Quaternion.Angle(transform.rotation, lookRotation) > 1f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
            yield return null;
        }
        transform.rotation = lookRotation;
    }

    public string GetNpcID() { return npcID; }

    // Kapý script'inin NPC'nin durumunu sormasý için gerekli fonksiyonlar
    public bool IsApproachingWork() { return currentState == State.GoingToWorkDoor; }
    public bool IsGoingHome() { return currentState == State.GoingHomeFromWork || currentState == State.GoingToHomeDoor; }
}