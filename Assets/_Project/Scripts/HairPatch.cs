using UnityEngine;

public class HairPatch : MonoBehaviour
{
    private HairStrand[] hairStrands;

    private void Awake()
    {
        CacheStrands();
    }

    public void CacheStrands()
    {
        hairStrands = GetComponentsInChildren<HairStrand>(true);
    }

    public int ShaveNearPoint(Vector3 hitPoint, float radius, float cutAmount)
    {
        if (hairStrands == null || hairStrands.Length == 0)
        {
            CacheStrands();
        }

        if (hairStrands == null) return 0;

        int harvestedUnits = 0;

        foreach (var strand in hairStrands)
        {
            if (strand != null && strand.gameObject.activeSelf)
            {
                float dist = Vector3.Distance(strand.transform.position, hitPoint);
                if (dist <= radius)
                {
                    if (strand.Shave(cutAmount))
                    {
                        harvestedUnits++;
                    }
                }
            }
        }

        return harvestedUnits;
    }
}
