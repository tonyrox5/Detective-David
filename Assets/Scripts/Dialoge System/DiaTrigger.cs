using System.Collections.Generic;
using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [Tooltip("Konu�malar yukar�dan a�a��ya do�ru kontrol edilir. Ko�ulu sa�lanan �LK konu�ma ba�lar.")]
    public List<ConditionalConversation> conversations;

    // MouseLook bu fonksiyonu �a��rarak do�ru konu�may� alacak
    public DialogueNode GetConversation()
    {
        if (DialogueUIManager.instance == null) return null;

        // Listeyi yukar�dan a�a��ya do�ru tara
        foreach (var conditionalConversation in conversations)
        {
            // E�er bu konu�man�n ko�ulu sa�lan�yorsa...
            if (DialogueUIManager.instance.CheckCondition(conditionalConversation.condition))
            {
                // Bu konu�may� d�nd�r ve aramay� durdur.
                return conditionalConversation.conversation;
            }
        }
        // E�er hi�bir ko�ul sa�lanmazsa, null d�nd�r.
        return null;
    }
}

// Bu, Inspector'da daha d�zenli g�r�nmesini sa�layan yard�mc� bir s�n�ft�r.
[System.Serializable]
public class ConditionalConversation
{
    public Condition condition;
    public DialogueNode conversation;
}