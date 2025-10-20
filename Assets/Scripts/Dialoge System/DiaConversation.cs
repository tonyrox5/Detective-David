using UnityEngine;

// Bu, bir konuþmanýn tamamýný tutan veri kabýdýr.
// CreateAssetMenu, Unity'nin "Sað Týk > Create" menüsüne eklememizi saðlar.
[CreateAssetMenu(fileName = "New Conversation", menuName = "Dialogue/Conversation")]
public class DialogueConversation : ScriptableObject
{
    // Konuþan kiþinin adý (isteðe baðlý)
    public string speakerName;
    // Konuþmanýn satýrlarý
    public string[] dialogueLines;
}