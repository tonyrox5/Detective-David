using UnityEngine;

[CreateAssetMenu(fileName = "New Dialogue Node", menuName = "Dialogue/Dialogue Node")]
public class DialogueNode : ScriptableObject
{
    [Header("Konu�ma ��eri�i")]
    public string speakerName;
    [TextArea(3, 10)]
    public string dialogueLine;

    [Header("Oyuncu Cevaplar�")]
    public PlayerResponse[] playerResponses;
}

[System.Serializable]
public class PlayerResponse
{
    public string responseText;
    public DialogueNode nextNode;

    [Header("G�r�nme Ko�ulu")]
    public Condition condition;
}

[System.Serializable]
public class Condition
{
    // --- YEN� KO�UL T�R�N� EKLED�K ---
    public enum ConditionType { None, TimeOfDay, HasItem }
    public ConditionType type = ConditionType.None;

    [Header("Time of Day Ko�ulu")]
    [Tooltip("Sadece 'TimeOfDay' se�iliyken ge�erlidir.")]
    public float minTime = 0f;
    [Tooltip("Sadece 'TimeOfDay' se�iliyken ge�erlidir.")]
    public float maxTime = 24f;

    // --- YEN� EKLENEN B�L�M ---
    [Header("Has Item Ko�ulu")]
    [Tooltip("Sadece 'HasItem' se�iliyken ge�erlidir. Oyuncunun envanterinde olmas� gereken e�yan�n ID'si.")]
    public string requiredItemID = "";
    // --- YEN� B�L�M�N SONU ---
}