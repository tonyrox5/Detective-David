using UnityEngine;

[CreateAssetMenu(fileName = "New Dialogue Node", menuName = "Dialogue/Dialogue Node")]
public class DialogueNode : ScriptableObject
{
    [Header("Konuþma Ýçeriði")]
    public string speakerName;
    [TextArea(3, 10)]
    public string dialogueLine;

    [Header("Oyuncu Cevaplarý")]
    public PlayerResponse[] playerResponses;
}

[System.Serializable]
public class PlayerResponse
{
    public string responseText;
    public DialogueNode nextNode;
    [Header("Görünme Koþulu")]
    public Condition condition;
}

[System.Serializable]
public class Condition
{
    public enum ConditionType { None, TimeOfDay, HasItem, Day }
    public ConditionType type = ConditionType.None;

    [Header("Time of Day Koþulu")]
    public float minTime = 0f;
    public float maxTime = 24f;

    [Header("Has Item Koþulu")]
    public string requiredItemID = "";

    [Header("Day Koþulu")]
    [Tooltip("Bu konuþmanýn/cevabýn sadece belirtilen günde görünmesini saðlar.")]
    public int requiredDay = 1;
}