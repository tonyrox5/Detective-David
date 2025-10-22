using System.Collections;
using System.Collections.Generic; // List i�in bu gerekli
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class GenericNPC_Controller : MonoBehaviour
{
    private enum State { GoingToWorkDoor, GoingToWork, Working, GoingHomeFromWork, GoingToHomeDoor, AtHome }

    [Header("Yapay Zeka Ayarlar�")]
    [SerializeField] private Transform workLocation;
    [SerializeField] private Transform workDoorstepLocation;
    [SerializeField] private Transform homeLocation;
    [SerializeField] private Transform workLookAtTarget;
    [SerializeField] private float rotationSpeed = 2f;

    // --- YEN� EKLENEN B�L�M: HIRSIZLIK TESP�T� ---
    [Header("G�zlem Ayarlar�")]
    [Tooltip("Bu NPC'nin i�e ba�lad���nda kontrol edece�i �nemli e�ya noktalar�.")]
    [SerializeField] private List<PlacementPoint> importantSpots;
    private bool hasNoticedTheft = false; // H�rs�zl��� fark etti mi?
    // --- YEN� B�L�M�N SONU ---

    [Header("Kimlik ve Kap�lar")]
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
                // ��e var�nca bu fonksiyon �a�r�lacak
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
        // Uyand���nda h�rs�zl�k fark etme durumunu s�f�rla (yeni g�n)
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

    // --- BU FONKS�YON G�NCELLEND� (HIRSIZLIK KONTROL� EKLEND�) ---
    private void StartWorking()
    {
        currentState = State.Working;

        if (workLookAtTarget != null)
        {
            StartCoroutine(RotateTowards(workLookAtTarget));
        }

        if (workDoor != null) { workDoor.CloseDoor(); }

        // YEN�: ��e ba�lad���nda, �nemli noktalar� kontrol et.
        CheckForMissingItems();
    }
    // --- G�NCELLEMEN�N SONU ---

    // --- YEN� EKLENEN FONKS�YON ---
    // �nemli e�ya noktalar�n� kontrol eden fonksiyon
    public void CheckForMissingItems()
    {
        // E�er zaten bir h�rs�zl�k fark ettiyse, tekrar kontrol etme (o g�n i�in)
        if (hasNoticedTheft) return;

        // �nemli noktalar listesindeki her bir yuvay� kontrol et
        foreach (var spot in importantSpots)
        {
            // E�er yuva BO�SA...
            if (spot != null && spot.itemInSpot == null)
            {
                // HIRSIZLIK TESP�T ED�LD�!
                hasNoticedTheft = true;
                Debug.LogWarning(npcID + " bir h�rs�zl�k fark etti! " + spot.gameObject.name + " noktas�ndaki e�ya kay�p!");

                // �leride NPC tepkisi burada tetiklenecek (�rne�in diyalo�u de�i�ebilir)

                break; // Bir tane bulmak yeterli, d�ng�den ��k.
            }
        }
    }
    // --- YEN� FONKS�YONUN SONU ---

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

    // Kap� script'inin NPC'nin durumunu sormas� i�in gerekli fonksiyonlar
    public bool IsApproachingWork() { return currentState == State.GoingToWorkDoor; }
    public bool IsGoingHome() { return currentState == State.GoingHomeFromWork || currentState == State.GoingToHomeDoor; }
}