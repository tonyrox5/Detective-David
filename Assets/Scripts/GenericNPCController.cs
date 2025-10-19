using System.Collections;
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

    // --- YEN� EKLENEN B�L�M ---
    [Tooltip("NPC'nin hedefe vard���nda d�n�� h�z�.")]
    [SerializeField] private float rotationSpeed = 2f;
    // --- YEN� B�L�M�N SONU ---

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
        if (distanceToWork < distanceToHome) { currentState = State.Working; StartWorking(); } // Ba�lang��ta i�teyse direkt d�ns�n
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
            // Art�k durum "GoingToWork" olunca hedefi buluyor, "Working" de�il.
            else if (currentState == State.GoingToWork)
            {
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
    }

    private void GoHome()
    {
        // Gitmeden �nce agent'�n rotasyon kontrol�n� geri verelim
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

    // --- BU FONKS�YON G�NCELLEND� ---
    private void StartWorking()
    {
        currentState = State.Working;

        if (workLookAtTarget != null)
        {
            // An�nda d�nmek yerine, yava��a d�nen Coroutine'i ba�lat
            StartCoroutine(RotateTowards(workLookAtTarget));
        }

        if (workDoor != null) { workDoor.CloseDoor(); }
    }
    // --- G�NCELLEMEN�N SONU ---

    // --- YEN� EKLENEN COROUTINE ---
    private IEnumerator RotateTowards(Transform target)
    {
        // NavMeshAgent'�n karakteri d�nd�rmesini ge�ici olarak devre d��� b�rak
        agent.updateRotation = false;

        Vector3 direction = (target.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));

        // Hedef rotasyona yeterince yakla�ana kadar d�nmeye devam et
        while (Quaternion.Angle(transform.rotation, lookRotation) > 1f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
            yield return null; // Bir sonraki kareye kadar bekle
        }

        // Tam olarak hedefte oldu�undan emin ol
        transform.rotation = lookRotation;
        // NavMeshAgent'�n rotasyon kontrol�n� geri ver (iste�e ba�l�, sabit duraca�� i�in)
        // agent.updateRotation = true; 
    }
    // --- YEN� COROUTINE SONU ---

    public string GetNpcID() { return npcID; }
}