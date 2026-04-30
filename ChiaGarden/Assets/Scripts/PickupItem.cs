using UnityEngine;

public class PickupItem : MonoBehaviour
{
    [Header("Item Info")]
    [SerializeField] private string itemName = "Item";

    [Header("Hold Offset")]
    [SerializeField] private Vector3 holdPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 holdRotationOffset = Vector3.zero;

    private Rigidbody rb;
    private Collider col;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    public string GetItemName()
    {
        return itemName;
    }

    public void PickUp(Transform holdPoint)
    {
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (col != null)
        {
            col.enabled = false;
        }

        transform.SetParent(holdPoint);
        transform.localPosition = holdPositionOffset;
        transform.localRotation = Quaternion.Euler(holdRotationOffset);
    }

    public void Drop()
    {
        transform.SetParent(null);

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        if (col != null)
        {
            col.enabled = true;
        }
    }
}