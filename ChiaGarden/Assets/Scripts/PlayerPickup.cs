using UnityEngine;
using TMPro;

public class PlayerPickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    [SerializeField] private float pickupRange = 3f;
    [SerializeField] private KeyCode pickupKey = KeyCode.E;
    [SerializeField] private KeyCode dropKey = KeyCode.Q;
    [SerializeField] private LayerMask pickupLayer;

    [Header("Hold Settings")]
    [SerializeField] private Transform holdPoint;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI pickupPromptText;

    private PickupItem heldItem;
    private PickupItem itemInView;

    private void Start()
    {
        if (pickupPromptText != null)
        {
            pickupPromptText.text = "";
        }
    }

    private void Update()
    {
        CheckForPickupItem();
        UpdatePrompt();

        if (heldItem == null)
        {
            if (Input.GetKeyDown(pickupKey) && itemInView != null)
            {
                PickUpItem(itemInView);
            }
        }
        else
        {
            if (Input.GetKeyDown(dropKey))
            {
                DropHeldItem();
            }
        }

        //this method is for seeds (left click to use) only when holding seeds
        if (Input.GetMouseButtonDown(0))
        {
            PickupItem held = GetHeldItem();

            if (held != null)
            {
                SeedTool seedTool = held.GetComponent<SeedTool>();

                if (seedTool != null)
                {
                    seedTool.Use();
                }
            }
        }

    }

    private void CheckForPickupItem()
    {
        itemInView = null;

        if (heldItem != null)
        {
            return;
        }

        Ray ray = new Ray(transform.position, transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange, pickupLayer))
        {
            PickupItem item = hit.collider.GetComponent<PickupItem>();

            if (item == null)
            {
                item = hit.collider.GetComponentInParent<PickupItem>();
            }

            if (item != null)
            {
                itemInView = item;
            }
        }
    }

    private void UpdatePrompt()
    {
        if (pickupPromptText == null) return;

        if (heldItem != null)
        {
            pickupPromptText.text = "[Q] to drop " + heldItem.GetItemName();
        }
        else if (itemInView != null)
        {
            pickupPromptText.text = "[E] to pick up " + itemInView.GetItemName();
        }
        else
        {
            pickupPromptText.text = "";
        }
    }

    private void PickUpItem(PickupItem item)
    {
        heldItem = item;
        heldItem.PickUp(holdPoint);
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