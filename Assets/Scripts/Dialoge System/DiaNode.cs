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

    [Header("Koþullar (bu düðümün gösterilmesi için)")]
    public Condition nodeCondition;

    [Header("Seçenekler")]
    public List<DiaChoice> choices = new(); // boþsa tek akýþlý düðüm olarak kullan

    [Header("Sonraki Düðüm (choices boþsa)")]
    public DiaNode nextIfNoChoices;
}

[Serializable]
public class DiaChoice
{
    public string choiceText;
    public Condition showIf;        // butonun görünmesi için koþul
    public DiaNode nextNode;        // týklanýnca gidilecek düðüm (boþsa kapanýr)
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
        SeenToday,      // nodeIdOverride (boþsa context node)
        NotSeenToday,   // nodeIdOverride (boþsa context node)
        CooldownHours   // requiredCooldownHours
    }

    public ConditionType type = ConditionType.None;

    [Header("Zaman Aralýðý")]
    public float minTime = 0f;
    public float maxTime = 24f;

    [Header("Envanter")]
    public string requiredItemID;

    [Header("Gün")]
    public int requiredDay = 1;

    [Header("Dialogue Memory")]
    public string nodeIdOverride = "";    // boþsa context node'un nodeId'si
    public float requiredCooldownHours = 0f;
}
