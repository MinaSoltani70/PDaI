using UnityEngine;

[DefaultExecutionOrder(100)]
public sealed class StationRowLayout : MonoBehaviour
{
    [SerializeField] private Transform viewTransform;
    [SerializeField] private Transform sellingStation;
    [SerializeField] private Transform upgradeShopStation;
    [SerializeField, Min(1f)] private float forwardDistance = 4f;
    [SerializeField, Min(1f)] private float halfSpacing = 2.4f;

    private void Start()
    {
        if (viewTransform == null || sellingStation == null || upgradeShopStation == null)
        {
            Debug.LogError("StationRowLayout requires a view transform and both station transforms.", this);
            return;
        }

        Vector3 forward = viewTransform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
        {
            Debug.LogError("StationRowLayout requires a view transform facing along the ground plane.", this);
            return;
        }

        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 center = viewTransform.position + forward * forwardDistance;
        Quaternion facingPlayer = Quaternion.LookRotation(-forward, Vector3.up);

        PlaceStation(sellingStation, center - right * halfSpacing, facingPlayer);
        PlaceStation(upgradeShopStation, center + right * halfSpacing, facingPlayer);
    }

    private static void PlaceStation(Transform station, Vector3 position, Quaternion rotation)
    {
        position.y = station.position.y;
        station.SetPositionAndRotation(position, rotation);
    }
}
