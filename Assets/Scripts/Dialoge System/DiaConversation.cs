using UnityEngine;

// Bu, bir konu�man�n tamam�n� tutan veri kab�d�r.
// CreateAssetMenu, Unity'nin "Sa� T�k > Create" men�s�ne eklememizi sa�lar.
[CreateAssetMenu(fileName = "New Conversation", menuName = "Dialogue/Conversation")]
public class DialogueConversation : ScriptableObject
{
    // Konu�an ki�inin ad� (iste�e ba�l�)
    public string speakerName;
    // Konu�man�n sat�rlar�
    public string[] dialogueLines;
}