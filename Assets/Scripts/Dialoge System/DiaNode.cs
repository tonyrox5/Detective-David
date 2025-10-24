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
    public enum ConditionType { None, TimeOfDay, HasItem, Day }
    public ConditionType type = ConditionType.None;

    [Header("Time of Day Ko�ulu")]
    public float minTime = 0f;
    public float maxTime = 24f;

    [Header("Has Item Ko�ulu")]
    public string requiredItemID = "";

    [Header("Day Ko�ulu")]
    [Tooltip("Bu konu�man�n/cevab�n sadece belirtilen g�nde g�r�nmesini sa�lar.")]
    public int requiredDay = 1;
}