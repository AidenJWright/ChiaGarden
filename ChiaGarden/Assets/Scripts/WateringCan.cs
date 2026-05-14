using UnityEngine;

[RequireComponent(typeof(PickupItem))]
public class WateringCan : MonoBehaviour
{
    [Header("Watering Can Settings")]
    [SerializeField] private int maxWater = 5;
    [SerializeField] private string waterTag = "Water";
    [SerializeField] private string chiaPetTag = "ChiaPet";

    private int currentWater = 5;

    // Called by PlayerPickup on left click when this item is held
    public void Use()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit)) return;

        GameObject hitObject = hit.collider.gameObject;

        if (hitObject.CompareTag(waterTag))
        {
            Fill();
            return;
        }

        if (hitObject.CompareTag(chiaPetTag))
        {
            TryWaterChiaPet(hitObject);
        }
    }

    private void Fill()
    {
        if (currentWater >= maxWater)
        {
            Debug.Log($"[WateringCan] Already full! ({currentWater}/{maxWater})");
            return;
        }

        currentWater = maxWater;
        Debug.Log($"[WateringCan] Filled! ({currentWater}/{maxWater})");
    }

    private void TryWaterChiaPet(GameObject chiaPet)
    {
        if (currentWater <= 0)
        {
            Debug.Log("[WateringCan] Out of water! Find a water source to refill.");
            return;
        }

        HairGrowth hairGrowth = chiaPet.GetComponent<HairGrowth>();

        if (hairGrowth == null)
        {
            Debug.LogWarning($"[WateringCan] {chiaPet.name} has no HairGrowth component!");
            return;
        }

        currentWater--;
        hairGrowth.enabled = true;
        hairGrowth.Grow();
        Debug.Log($"[WateringCan] Watered {chiaPet.name}! Water remaining: {currentWater}/{maxWater}");
    }

    // Public accessors for UI or other systems
    public int GetCurrentWater() => currentWater;
    public int GetMaxWater() => maxWater;
    public bool IsFull() => currentWater >= maxWater;
    public bool IsEmpty() => currentWater <= 0;

    public string GetInteractPrompt(GameObject target)
    {
        if (target != null && target.CompareTag(waterTag))
        {
            if (IsFull()) return $"Watering can is already full! ({currentWater}/{maxWater}) | [Q] Drop";
            return $"[Click] Refill watering can ({currentWater}/{maxWater}) | [Q] Drop";
        }

        if (target != null && target.CompareTag(chiaPetTag))
        {
            if (IsEmpty()) return "Watering can is empty! Find a water source to refill. | [Q] Drop";
            return $"[Click] Water this plant ({currentWater}/{maxWater}) | [Q] Drop";
        }

        if (target != null)
            return $"Can't use the watering can on that. | Water: {currentWater}/{maxWater} | [Q] Drop";

        return $"Water: {currentWater}/{maxWater} | [Q] Drop";
    }
}