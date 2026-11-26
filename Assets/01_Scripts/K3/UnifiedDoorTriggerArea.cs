using UnityEngine;

/// <summary>
/// Meneruskan event trigger dari child TRG_PintuArea ke induk UnifiedDoorController.
/// Mendukung OnTriggerEnter, OnTriggerStay, dan OnTriggerExit dengan verifikasi pemain VR & PC yang andal.
/// </summary>
public class UnifiedDoorTriggerArea : MonoBehaviour
{
    private UnifiedDoorController parentDoor;

    void Awake()
    {
        FindParentDoor();
    }

    void Start()
    {
        FindParentDoor();
    }

    private void FindParentDoor()
    {
        if (parentDoor == null)
        {
            parentDoor = GetComponentInParent<UnifiedDoorController>();
            if (parentDoor == null)
            {
#if UNITY_2023_1_OR_NEWER
                parentDoor = Object.FindFirstObjectByType<UnifiedDoorController>();
#else
                parentDoor = Object.FindObjectOfType<UnifiedDoorController>();
#endif
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (UnifiedDoorController.IsPlayerCollider(other))
        {
            FindParentDoor();
            if (parentDoor != null) parentDoor.OnPlayerEnterTrigger();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (UnifiedDoorController.IsPlayerCollider(other))
        {
            FindParentDoor();
            if (parentDoor != null)
            {
                if (!parentDoor.isOpen && parentDoor.autoOpenOnPlayer)
                {
                    parentDoor.OnPlayerEnterTrigger();
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (UnifiedDoorController.IsPlayerCollider(other))
        {
            FindParentDoor();
            if (parentDoor != null) parentDoor.OnPlayerExitTrigger();
        }
    }
}
