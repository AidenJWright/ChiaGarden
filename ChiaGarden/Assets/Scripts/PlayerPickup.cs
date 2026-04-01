using UnityEngine;

public class PlayerPickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    [SerializeField] private float pickupRange = 3f;
    [SerializeField] private KeyCode pickupKey = KeyCode.E;
    [SerializeField] private KeyCode dropKey = KeyCode.Q;
    [SerializeField] private LayerMask pickupLayer;

    [Header("Hold Settings")]
    [SerializeField] private Transform holdPoint;

    private PickupItem heldItem;

    private void Update()
    {
        if (heldItem == null)
        {
            if (Input.GetKeyDown(pickupKey))
            {
                TryPickUp();
            }
        }
        else
        {
            if (Input.GetKeyDown(dropKey))
            {
                DropHeldItem();
            }
        }
    }

    private void TryPickUp()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupRange, pickupLayer))
        {
            PickupItem item = hit.collider.GetComponent<PickupItem>();

            if (item == null)
            {
                item = hit.collider.GetComponentInParent<PickupItem>();
            }

            if (item != null)
            {
                heldItem = item;
                heldItem.PickUp(holdPoint);
            }
        }
    }

    private void DropHeldItem()
    {
        if (heldItem == null) return;

        heldItem.Drop();
        heldItem = null;
    }

    public bool IsHoldingItem()
    {
        return heldItem != null;
    }

    public PickupItem GetHeldItem()
    {
        return heldItem;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, transform.forward * pickupRange);
    }
}
