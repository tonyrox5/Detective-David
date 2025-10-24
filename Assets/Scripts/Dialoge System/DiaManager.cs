using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DialogueUIManager : MonoBehaviour
{
    public static DialogueUIManager instance;
    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI dialogueLineText;
    [SerializeField] private GameObject choicesLayout;
    [SerializeField] private Button[] choiceButtons;
    [Header("Oyuncu Kontrol Referanslarý")]
    [SerializeField] private ClassicPlayerMovement playerMovement;
    [SerializeField] private MouseLook mouseLook;
    private DialogueNode currentNode;
    private bool isDialogueActive = false;

    private void Awake() { if (instance == null) instance = this; else Destroy(gameObject); }

    public void StartDialogue(DialogueNode startingNode)
    {
        isDialogueActive = true;
        dialoguePanel.SetActive(true);
        playerMovement.enabled = false;
        mouseLook.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        DisplayNode(startingNode);
    }

    private void DisplayNode(DialogueNode node)
    {
        currentNode = node;
        speakerNameText.text = node.speakerName;
        dialogueLineText.text = node.dialogueLine;
        foreach (var button in choiceButtons) { button.onClick.RemoveAllListeners(); button.gameObject.SetActive(false); }

        if (node.playerResponses.Length > 0)
        {
            choicesLayout.SetActive(true);
            int visibleButtonIndex = 0;
            for (int i = 0; i < node.playerResponses.Length; i++)
            {
                if (CheckCondition(node.playerResponses[i].condition))
                {
                    if (visibleButtonIndex < choiceButtons.Length)
                    {
                        Button button = choiceButtons[visibleButtonIndex];
                        button.gameObject.SetActive(true);
                        button.GetComponentInChildren<TextMeshProUGUI>().text = node.playerResponses[i].responseText;
                        int choiceIndex = i;
                        button.onClick.AddListener(() => OnChoiceSelected(currentNode.playerResponses[choiceIndex]));
                        visibleButtonIndex++;
                    }
                }
            }
        }
        else
        {
            choicesLayout.SetActive(false);
            StartCoroutine(WaitForEndOfDialogue());
        }
    }

    // --- BU FONKSÝYON GÜNCELLENDÝ (YENÝ KOÞUL EKLENDÝ VE PUBLIC YAPILDI) ---
    public bool CheckCondition(Condition condition)
    {
        if (condition == null) return true;
        switch (condition.type)
        {
            case Condition.ConditionType.TimeOfDay:
                float currentTime = TimeManager.instance.GetCurrentTime();
                return currentTime >= condition.minTime && currentTime < condition.maxTime;

            case Condition.ConditionType.HasItem:
                if (HandSystem.instance != null && !string.IsNullOrEmpty(condition.requiredItemID))
                {
                    return HandSystem.instance.HasItem(condition.requiredItemID);
                }
                return false;

            case Condition.ConditionType.Day:
                if (TimeManager.instance != null)
                {
                    return TimeManager.instance.GetDayNumber() == condition.requiredDay;
                }
                return false;

            case Condition.ConditionType.None:
            default:
                return true;
        }
    }

    private void OnChoiceSelected(PlayerResponse response) { EventSystem.current.SetSelectedGameObject(null); if (response.nextNode != null) { DisplayNode(response.nextNode); } else { EndDialogue(); } }
    private IEnumerator WaitForEndOfDialogue() { yield return new WaitUntil(() => Input.GetMouseButtonDown(0)); EndDialogue(); }
    private void EndDialogue() { isDialogueActive = false; dialoguePanel.SetActive(false); playerMovement.enabled = true; mouseLook.enabled = true; Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
    public bool IsDialogueActive() { return isDialogueActive; }
}