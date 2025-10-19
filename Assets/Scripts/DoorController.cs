using System.Collections;
using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Kapý Ayarlarý")]
    [SerializeField] private Transform hinge;
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float closeAngle = 0f;
    [SerializeField] public float animationSpeed = 2f;

    [Header("Kilitleme Ayarlarý")]
    [SerializeField] private bool isLocked = false;
    [SerializeField] private string requiredItemID = "";
    [SerializeField] private string ownerID = "";

    private bool isOpen = false;
    private bool isAnimating = false;

    // Update fonksiyonu tamamen kaldýrýldý.

    public void Interact()
    {
        if (isLocked && !isOpen) { Debug.Log("Kapý kilitli!"); return; }
        if (isAnimating) return;
        float targetAngle = isOpen ? closeAngle : openAngle;
        StartCoroutine(AnimateDoor(targetAngle));
        isOpen = !isOpen;
    }

    // Dýþarýdan kapýyý kapatma komutu
    public void CloseDoor()
    {
        if (isOpen && !isAnimating)
        {
            Interact();
        }
    }

    private IEnumerator AnimateDoor(float targetAngle)
    {
        isAnimating = true;
        Quaternion startRotation = hinge.localRotation;
        Quaternion targetRotation = Quaternion.Euler(0, targetAngle, 0);
        float time = 0;
        while (time < 1)
        {
            hinge.localRotation = Quaternion.Slerp(startRotation, targetRotation, time);
            time += Time.deltaTime * animationSpeed;
            yield return null;
        }
        hinge.localRotation = targetRotation;
        isAnimating = false;
    }

    public void Unlock() { isLocked = false; }
    public bool IsLocked() { return isLocked; }
    public string GetRequiredItemID() { return requiredItemID; }
    public bool IsOpen() { return isOpen; }
    public string GetOwnerID() { return ownerID; }
}