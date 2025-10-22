using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/DiaNode")]
public class DiaNode : ScriptableObject
{
    [Header("Kimlik")]
    public string nodeId; // benzersiz: "shop_morning_greet" gibi

    [Header("Metin")]
    [TextArea(2, 6)] public string text;

    [Header("Ko�ullar (bu d���m�n g�sterilmesi i�in)")]
    public Condition nodeCondition;

    [Header("Se�enekler")]
    public List<DiaChoice> choices = new(); // bo�sa tek ak��l� d���m olarak kullan

    [Header("Sonraki D���m (choices bo�sa)")]
    public DiaNode nextIfNoChoices;
}

[Serializable]
public class DiaChoice
{
    public string choiceText;
    public Condition showIf;        // butonun g�r�nmesi i�in ko�ul
    public DiaNode nextNode;        // t�klan�nca gidilecek d���m (bo�sa kapan�r)
}

[Serializable]
public class Condition
{
    public enum ConditionType
    {
        None,
        TimeOfDay,      // minTime <= now < maxTime
        HasItem,        // requiredItemID
        Day,            // requiredDay
        SeenToday,      // nodeIdOverride (bo�sa context node)
        NotSeenToday,   // nodeIdOverride (bo�sa context node)
        CooldownHours   // requiredCooldownHours
    }

    public ConditionType type = ConditionType.None;

    [Header("Zaman Aral���")]
    public float minTime = 0f;
    public float maxTime = 24f;

    [Header("Envanter")]
    public string requiredItemID;

    [Header("G�n")]
    public int requiredDay = 1;

    [Header("Dialogue Memory")]
    public string nodeIdOverride = "";    // bo�sa context node'un nodeId'si
    public float requiredCooldownHours = 0f;
}
