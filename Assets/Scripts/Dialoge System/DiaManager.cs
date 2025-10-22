using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DiaManager : MonoBehaviour
{
    public static DiaManager instance;

    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI npcNameText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private Transform choicesParent;
    [SerializeField] private Button choiceButtonPrefab;

    [Header("Devre D��� B�rak�lacak Sistemler")]
    [SerializeField] private Behaviour[] systemsToDisableWhileDialogue;

    // STATE
    private DiaNode currentNode;
    private readonly List<Button> spawnedButtons = new List<Button>();
    private bool active = false;
    private string currentNpcId = null;   // g�n-i�i haf�za ba�lam�
    private bool nodeRendered = false;    // bu node ger�ekten ekranda g�sterildi mi?

    private void Awake()
    {
        if (instance == null) instance = this;
        else { Destroy(gameObject); return; }

        if (panel != null) panel.SetActive(false);
    }

    // ---------- D�YALOG BA�LATMA ----------
    // NPC kimli�i ile ba�lat (�NER�LEN)
    public void StartDialogue(DiaNode startNode, string npcId, string npcDisplayName = "")
    {
        currentNpcId = npcId;
        if (npcNameText != null) npcNameText.text = npcDisplayName;
        StartDialogue(startNode);
    }

    // Geriye d�n�k uyumluluk (npcId olmadan)
    public void StartDialogue(DiaNode startNode)
    {
        if (startNode == null) return;
        currentNode = startNode;
        OpenPanel();
        RenderNode();
    }

    private void OpenPanel()
    {
        active = true;
        if (panel != null) panel.SetActive(true);
        SetSystemsEnabled(false);
    }

    private void ClosePanel()
    {
        active = false;
        if (panel != null) panel.SetActive(false);
        SetSystemsEnabled(true);
    }

    private void SetSystemsEnabled(bool enabled)
    {
        if (systemsToDisableWhileDialogue == null) return;
        foreach (var b in systemsToDisableWhileDialogue)
        {
            if (b == null) continue;
            b.enabled = enabled;
        }
    }

    // ---------- RENDER ----------
    private void RenderNode()
    {
        nodeRendered = false;

        if (currentNode == null)
        {
            EndDialogue();
            return;
        }

        // Node-level condition: sa�lanm�yorsa hi� g�stermeden kapat (g�r�lm�� sayma)
        if (!CheckCondition(currentNode.nodeCondition, currentNpcId, currentNode))
        {
            EndDialogue();
            return;
        }

        // Art�k bu node ger�ekten g�steriliyor
        nodeRendered = true;

        if (bodyText != null) bodyText.text = currentNode.text;

        // Se�enekleri temizle
        foreach (var b in spawnedButtons) Destroy(b.gameObject);
        spawnedButtons.Clear();

        bool anyChoice = false;
        foreach (var choice in currentNode.choices)
        {
            if (!CheckCondition(choice.showIf, currentNpcId, currentNode)) continue;

            var btn = Instantiate(choiceButtonPrefab, choicesParent);
            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = choice.choiceText;

            // closure g�venli�i i�in local kopya
            var localChoice = choice;
            btn.onClick.AddListener(() => OnChoiceClicked(localChoice));

            spawnedButtons.Add(btn);
            anyChoice = true;
        }

        if (!anyChoice)
        {
            // choices yoksa veya g�r�n�r choice yoksa tek ak��
            if (currentNode.nextIfNoChoices != null)
            {
                // self-loop korumas�
                if (currentNode.nextIfNoChoices == currentNode)
                {
                    EndDialogue();
                }
                else
                {
                    // buton g�stermeden ileri
                    GoToNode(currentNode.nextIfNoChoices);
                }
            }
            else
            {
                // konu�may� bitir
                EndDialogue();
            }
        }
    }

    private void OnChoiceClicked(DiaChoice choice)
    {
        if (choice != null && choice.nextNode != null)
        {
            // self-loop korumas�
            if (choice.nextNode == currentNode)
            {
                EndDialogue();
            }
            else
            {
                GoToNode(choice.nextNode);
            }
        }
        else
        {
            EndDialogue();
        }
    }

    private void GoToNode(DiaNode next)
    {
        // Bu node ekranda ger�ekten g�sterildiyse "g�r�ld�" i�aretle
        if (nodeRendered && DialogueMemory.instance != null && !string.IsNullOrEmpty(currentNpcId) && currentNode != null)
        {
            float now = (TimeManager.instance != null) ? TimeManager.instance.GetCurrentTime() : 0f;
            DialogueMemory.instance.MarkSeenToday(currentNpcId, currentNode.nodeId, now);
        }

        currentNode = next;
        RenderNode();
    }

    private void EndDialogue()
    {
        // Node ekranda g�sterildiyse kapan��ta da g�r�ld� say (�r. choices�z tek ekranlar)
        if (nodeRendered && DialogueMemory.instance != null && !string.IsNullOrEmpty(currentNpcId) && currentNode != null)
        {
            float now = (TimeManager.instance != null) ? TimeManager.instance.GetCurrentTime() : 0f;
            DialogueMemory.instance.MarkSeenToday(currentNpcId, currentNode.nodeId, now);
        }

        currentNode = null;
        currentNpcId = null;
        nodeRendered = false;

        foreach (var b in spawnedButtons) Destroy(b.gameObject);
        spawnedButtons.Clear();

        ClosePanel();
    }

    // ---------- KO�UL KONTROL ----------
    // Geriye d�n�k uyumluluk
    public bool CheckCondition(Condition c)
    {
        return CheckCondition(c, null, null);
    }

    // NPC ba�lam�yla geni�letilmi�
    public bool CheckCondition(Condition c, string npcId, DiaNode contextNode)
    {
        if (c == null || c.type == Condition.ConditionType.None) return true;

        switch (c.type)
        {
            case Condition.ConditionType.TimeOfDay:
                {
                    float now = (TimeManager.instance != null) ? TimeManager.instance.GetCurrentTime() : 0f;
                    return now >= c.minTime && now < c.maxTime;
                }

            case Condition.ConditionType.HasItem:
                {
                    if (string.IsNullOrEmpty(c.requiredItemID)) return true;
                    // Envanter kontrol� (HandSystem):
                    return HandSystem.instance != null && HandSystem.instance.HasItem(c.requiredItemID);
                }

            case Condition.ConditionType.Day:
                {
                    int day = (TimeManager.instance != null) ? TimeManager.instance.GetDayNumber() : 1;
                    return day == c.requiredDay;
                }

            case Condition.ConditionType.SeenToday:
                {
                    string id = !string.IsNullOrEmpty(c.nodeIdOverride)
                                ? c.nodeIdOverride
                                : (contextNode != null ? contextNode.nodeId : null);
                    if (DialogueMemory.instance == null || string.IsNullOrEmpty(npcId) || string.IsNullOrEmpty(id)) return false;
                    return DialogueMemory.instance.HasSeenToday(npcId, id);
                }

            case Condition.ConditionType.NotSeenToday:
                {
                    string id = !string.IsNullOrEmpty(c.nodeIdOverride)
                                ? c.nodeIdOverride
                                : (contextNode != null ? contextNode.nodeId : null);
                    if (DialogueMemory.instance == null || string.IsNullOrEmpty(npcId) || string.IsNullOrEmpty(id)) return true;
                    return !DialogueMemory.instance.HasSeenToday(npcId, id);
                }

            case Condition.ConditionType.CooldownHours:
                {
                    if (c.requiredCooldownHours <= 0f) return true;

                    string id = !string.IsNullOrEmpty(c.nodeIdOverride)
                                ? c.nodeIdOverride
                                : (contextNode != null ? contextNode.nodeId : null);

                    if (DialogueMemory.instance == null || string.IsNullOrEmpty(npcId) || string.IsNullOrEmpty(id)) return true;

                    float now = (TimeManager.instance != null) ? TimeManager.instance.GetCurrentTime() : 0f;
                    float hours;
                    if (!DialogueMemory.instance.HoursSince(npcId, id, now, out hours)) return true; // hi� konu�ulmad� ? serbest
                    return hours >= c.requiredCooldownHours;
                }
        }

        return true;
    }
}
