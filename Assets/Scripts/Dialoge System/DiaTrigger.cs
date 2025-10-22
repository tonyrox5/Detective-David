using System.Collections.Generic;
using UnityEngine;

public class DiaTrigger : MonoBehaviour
{
    [System.Serializable]
    public class ConditionalConversation
    {
        public Condition condition;
        public DiaNode conversation;
    }

    [Tooltip("�stten alta �ncelik: ilk ge�en konu�ma se�ilir")]
    public List<ConditionalConversation> conversations = new();

    [Header("NPC Kimli�i")]
    public string npcId; // bo�sa GenericNPCController'dan al�n�r
    public string npcDisplayName = ""; // UI'de g�stermek i�in (opsiyonel)

    private string ResolveNpcId()
    {
        if (!string.IsNullOrEmpty(npcId)) return npcId;
        var ctrl = GetComponent<GenericNPC_Controller>();
        return (ctrl != null) ? ctrl.GetNpcID() : name; // fallback
    }

    public DiaNode GetConversation()
    {
        if (DiaManager.instance == null) return null;
        string id = ResolveNpcId();

        foreach (var cc in conversations)
        {
            if (cc == null || cc.conversation == null) continue;
            if (DiaManager.instance.CheckCondition(cc.condition, id, cc.conversation))
                return cc.conversation;
        }
        return null;
    }

    public void StartConversation()
    {
        var node = GetConversation();
        if (node != null)
        {
            DiaManager.instance.StartDialogue(node, ResolveNpcId(), npcDisplayName);
        }
    }
}
