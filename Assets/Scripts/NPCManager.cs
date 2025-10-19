using System.Collections.Generic;
using UnityEngine;

public class NPCManager : MonoBehaviour
{
    public static NPCManager instance;
    [SerializeField] private List<GenericNPC_Controller> allNpcs = new List<GenericNPC_Controller>();
    private bool wasMorning = false;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    // public void RegisterNPC... fonksiyonunu art�k kullanmad���m�z i�in silebilirsin.

    void Update()
    {
        float currentTime = TimeManager.instance.GetCurrentTime();
        if (currentTime >= 17 && wasMorning) // Ak�am 5'i ge�ti�i an
        {
            // Bu k�s�m ileride "herkes evine d�ns�n" komutu i�in kullan�labilir
        }

        if (currentTime >= 7 && !wasMorning)
        {
            WakeUpAllNPCs();
        }
        wasMorning = (currentTime >= 7);
    }

    private void WakeUpAllNPCs()
    {
        Debug.Log("SABAH OLDU! T�m NPC'ler uyand�r�l�yor...");
        foreach (var npc in allNpcs)
        {
            if (npc != null && !npc.gameObject.activeInHierarchy)
            {
                npc.WakeUpAndGoToWork();
            }
        }
    }

    // --- YEN� EKLENEN FONKS�YON ---
    // Kap�lar�n, kimli�e g�re NPC'yi bulmas�n� sa�layan fonksiyon
    public GenericNPC_Controller FindNPCByID(string id)
    {
        foreach (var npc in allNpcs)
        {
            if (npc.GetNpcID() == id)
            {
                return npc;
            }
        }
        return null; // Bulunamad�
    }
    // --- YEN� FONKS�YONUN SONU ---
}