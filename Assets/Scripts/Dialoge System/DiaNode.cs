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
    // --- YENÝ KOÞUL TÜRÜNÜ EKLEDÝK ---
    public enum ConditionType { None, TimeOfDay, HasItem }
    public ConditionType type = ConditionType.None;

    [Header("Time of Day Koþulu")]
    [Tooltip("Sadece 'TimeOfDay' seçiliyken geçerlidir.")]
    public float minTime = 0f;
    [Tooltip("Sadece 'TimeOfDay' seçiliyken geçerlidir.")]
    public float maxTime = 24f;

    // --- YENÝ EKLENEN BÖLÜM ---
    [Header("Has Item Koþulu")]
    [Tooltip("Sadece 'HasItem' seçiliyken geçerlidir. Oyuncunun envanterinde olmasý gereken eþyanýn ID'si.")]
    public string requiredItemID = "";
    // --- YENÝ BÖLÜMÜN SONU ---
}