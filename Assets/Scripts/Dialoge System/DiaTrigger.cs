using System.Collections.Generic;
using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [Tooltip("Konuþmalar yukarýdan aþaðýya doðru kontrol edilir. Koþulu saðlanan ÝLK konuþma baþlar.")]
    public List<ConditionalConversation> conversations;

    // MouseLook bu fonksiyonu çaðýrarak doðru konuþmayý alacak
    public DialogueNode GetConversation()
    {
        if (DialogueUIManager.instance == null) return null;

        // Listeyi yukarýdan aþaðýya doðru tara
        foreach (var conditionalConversation in conversations)
        {
            // Eðer bu konuþmanýn koþulu saðlanýyorsa...
            if (DialogueUIManager.instance.CheckCondition(conditionalConversation.condition))
            {
                // Bu konuþmayý döndür ve aramayý durdur.
                return conditionalConversation.conversation;
            }
        }
        // Eðer hiçbir koþul saðlanmazsa, null döndür.
        return null;
    }
}

// Bu, Inspector'da daha düzenli görünmesini saðlayan yardýmcý bir sýnýftýr.
[System.Serializable]
public class ConditionalConversation
{
    public Condition condition;
    public DialogueNode conversation;
}