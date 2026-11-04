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

    // Mencukur helai rambut terdekat dengan titik kontak alat cukur
    public void ShaveNearPoint(Vector3 hitPoint, float radius, float cutAmount)
    {
        if (hairStrands == null || hairStrands.Length == 0)
        {
            CacheStrands();
        }

        if (hairStrands == null) return;

        foreach (var strand in hairStrands)
        {
            if (strand != null && strand.gameObject.activeSelf)
            {
                float dist = Vector3.Distance(strand.transform.position, hitPoint);
                if (dist <= radius)
                {
                    strand.Shave(cutAmount);
                }
            }
        }
    }
}
