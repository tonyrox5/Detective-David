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

    // public void RegisterNPC... fonksiyonunu artýk kullanmadýðýmýz için silebilirsin.

    void Update()
    {
        float currentTime = TimeManager.instance.GetCurrentTime();
        if (currentTime >= 17 && wasMorning) // Akþam 5'i geçtiði an
        {
            // Bu kýsým ileride "herkes evine dönsün" komutu için kullanýlabilir
        }

        if (currentTime >= 7 && !wasMorning)
        {
            WakeUpAllNPCs();
        }
        wasMorning = (currentTime >= 7);
    }

    private void WakeUpAllNPCs()
    {
        Debug.Log("SABAH OLDU! Tüm NPC'ler uyandýrýlýyor...");
        foreach (var npc in allNpcs)
        {
            if (npc != null && !npc.gameObject.activeInHierarchy)
            {
                npc.WakeUpAndGoToWork();
            }
        }
    }

    // --- YENÝ EKLENEN FONKSÝYON ---
    // Kapýlarýn, kimliðe göre NPC'yi bulmasýný saðlayan fonksiyon
    public GenericNPC_Controller FindNPCByID(string id)
    {
        foreach (var npc in allNpcs)
        {
            if (npc.GetNpcID() == id)
            {
                return npc;
            }
        }
        return null; // Bulunamadý
    }
    // --- YENÝ FONKSÝYONUN SONU ---
}