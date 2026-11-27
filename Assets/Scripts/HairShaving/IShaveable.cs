
using UnityEngine;

public interface IShaveable
{
    int ShaveNearPoint(
        Vector3 hitPoint,
        float radius,
        float cutAmount,
        int maxHairUnits
    );
}
