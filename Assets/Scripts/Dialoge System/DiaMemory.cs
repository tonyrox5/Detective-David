using System.Collections.Generic;
using UnityEngine;

public class DialogueMemory : MonoBehaviour
{
    public static DialogueMemory instance;
    private void Awake()
    {
        if (instance == null) instance = this;
        else { Destroy(gameObject); return; }
    }

    // npcId -> (nodeId -> seenToday)
    private readonly Dictionary<string, HashSet<string>> seenToday = new();
    // npcId -> (nodeId -> lastHour)
    private readonly Dictionary<string, Dictionary<string, float>> lastSpokenHour = new();

    public void MarkSeenToday(string npcId, string nodeId, float hour)
    {
        if (string.IsNullOrEmpty(npcId) || string.IsNullOrEmpty(nodeId)) return;

        if (!seenToday.TryGetValue(npcId, out var set))
        { set = new HashSet<string>(); seenToday[npcId] = set; }
        set.Add(nodeId);

        if (!lastSpokenHour.TryGetValue(npcId, out var map))
        { map = new Dictionary<string, float>(); lastSpokenHour[npcId] = map; }
        map[nodeId] = hour;
    }

    public bool HasSeenToday(string npcId, string nodeId)
    {
        return !string.IsNullOrEmpty(npcId) && !string.IsNullOrEmpty(nodeId)
               && seenToday.TryGetValue(npcId, out var set) && set.Contains(nodeId);
    }

    public bool HoursSince(string npcId, string nodeId, float now, out float hours)
    {
        hours = 0f;
        if (string.IsNullOrEmpty(npcId) || string.IsNullOrEmpty(nodeId)) return false;
        if (!lastSpokenHour.TryGetValue(npcId, out var map)) return false;
        if (!map.TryGetValue(nodeId, out var when)) return false;

        hours = now >= when ? (now - when) : (24f - when + now);
        return true;
    }

    // Yeni güne geçince çaðýr (istersen TimeManager'dan tetikleyebilirsin)
    public void ResetDailyFlags()
    {
        seenToday.Clear();
        // lastSpokenHour.Clear(); // son konuþma saatini de sýfýrlamak istersen aç
    }
}
