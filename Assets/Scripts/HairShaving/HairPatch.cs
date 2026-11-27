using UnityEngine;

public class HairPatch : MonoBehaviour, IShaveable
{
    private HairStrand[] hairStrands;

    private void Awake() => CacheStrands();

    public void CacheStrands() => hairStrands = GetComponentsInChildren<HairStrand>(true);

    
public int ShaveNearPoint(Vector3 hitPoint, float radius, float cutAmount, int maxHairUnits)
{
    if (maxHairUnits <= 0) return 0;

    if (hairStrands == null || hairStrands.Length == 0)
        CacheStrands();

    if (hairStrands == null) return 0;

    int harvested = 0;

    foreach (var strand in hairStrands)
    {
        if (harvested >= maxHairUnits)
            break;

        if (strand == null || !strand.gameObject.activeSelf)
            continue;

        if (Vector3.Distance(strand.RootPosition, hitPoint) <= radius)
        {
            if (strand.Shave(cutAmount))
                harvested++;
        }
    }

    return harvested;
}
}