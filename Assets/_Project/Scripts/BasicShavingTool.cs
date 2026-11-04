using UnityEngine;

public class BasicShavingTool : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float shaveRange = 5.0f;       // Jarak jangkauan alat cukur
    [SerializeField] private float toolHeadRadius = 0.35f;  // Ukuran kepala alat cukur (SphereCast)
    [SerializeField] private float shaveRadius = 0.6f;      // Radius pemotongan bulu di sekitar titik kontak
    [SerializeField] private float shaveSpeed = 1.2f;       // Kecepatan mencukur
    [SerializeField] private LayerMask shavableLayer = ~0;

    [Header("Crosshair UI")]
    [SerializeField] private bool showCrosshair = true;
    [SerializeField] private Color crosshairColor = Color.white;

    [Header("Cutting Area Visual Ring")]
    [SerializeField] private LineRenderer visualRing;
    [SerializeField] private int ringSegments = 40;
    [SerializeField] private float ringLineWidth = 0.03f;
    [SerializeField] private Color ringIdleColor = Color.white;
    [SerializeField] private Color ringCuttingColor = new Color(0.4f, 1f, 0.5f, 1f);

    private void Start()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        SetupVisualRing();
    }

    private void SetupVisualRing()
    {
        if (visualRing == null)
        {
            // Buat child GameObject otomatis jika belum dipasang di Inspector
            GameObject ringObj = new GameObject("CuttingVisualRing");
            ringObj.transform.SetParent(transform);
            visualRing = ringObj.AddComponent<LineRenderer>();
        }

        visualRing.loop = true;
        visualRing.useWorldSpace = true;
        visualRing.positionCount = ringSegments;
        visualRing.startWidth = ringLineWidth;
        visualRing.endWidth = ringLineWidth;
        visualRing.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        visualRing.receiveShadows = false;
        visualRing.alignment = LineAlignment.View;

        // Pasang material HDRP Unlit jika tersedia
        if (visualRing.sharedMaterial == null || visualRing.sharedMaterial.name == "Default-Line")
        {
            Material mat = Resources.Load<Material>("CuttingRingMat");
            if (mat == null)
            {
                Shader s = Shader.Find("HDRP/Unlit");
                if (s == null) s = Shader.Find("Universal Render Pipeline/Unlit");
                if (s == null) s = Shader.Find("Sprites/Default");
                if (s != null)
                {
                    mat = new Material(s);
                    if (mat.HasProperty("_UnlitColor")) mat.SetColor("_UnlitColor", Color.white);
                    if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
                }
            }
            if (mat != null) visualRing.material = mat;
        }

        visualRing.enabled = false;
    }

    private void Update()
    {
        bool isClicking = false;

        #if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.isPressed)
        {
            isClicking = true;
        }
        #endif

        if (Input.GetMouseButton(0))
        {
            isClicking = true;
        }

        UpdateVisualRing(isClicking);

        if (isClicking)
        {
            PerformShave();
        }
    }

    private void UpdateVisualRing(bool isCutting)
    {
        if (visualRing == null || playerCamera == null) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, shaveRange, shavableLayer))
        {
            visualRing.enabled = true;

            // Atur warna ring (putih saat mengarah, hijau saat mencukur)
            Color activeColor = isCutting ? ringCuttingColor : ringIdleColor;
            visualRing.startColor = activeColor;
            visualRing.endColor = activeColor;

            DrawCircleOnSurface(hit.point, hit.normal, shaveRadius);
        }
        else
        {
            // Sembunyikan ring jika di luar jangkauan atau tidak membidik permukaan
            visualRing.enabled = false;
        }
    }

    private void DrawCircleOnSurface(Vector3 center, Vector3 normal, float radius)
    {
        // Beri sedikit offset di sepanjang normal agar tidak z-fighting dengan permukaan
        Vector3 surfaceCenter = center + normal * 0.008f;

        // Hitung vektor basis ortogonal pada bidang permukaan (tangent & bitangent)
        Vector3 helper = Mathf.Abs(normal.y) > 0.99f ? Vector3.forward : Vector3.up;
        Vector3 tangent = Vector3.Cross(normal, helper).normalized;
        Vector3 bitangent = Vector3.Cross(normal, tangent).normalized;

        if (visualRing.positionCount != ringSegments)
        {
            visualRing.positionCount = ringSegments;
        }

        for (int i = 0; i < ringSegments; i++)
        {
            float angle = (i / (float)ringSegments) * Mathf.PI * 2f;
            Vector3 point = surfaceCenter + (tangent * Mathf.Cos(angle) + bitangent * Mathf.Sin(angle)) * radius;
            visualRing.SetPosition(i, point);
        }
    }

    private void PerformShave()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null) return;
        }

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        bool hasHit = Physics.SphereCast(ray, toolHeadRadius, out hit, shaveRange, shavableLayer);
        if (!hasHit)
        {
            hasHit = Physics.Raycast(ray, out hit, shaveRange, shavableLayer);
        }

        if (hasHit)
        {
            HairStrand strand = hit.collider.GetComponent<HairStrand>();
            if (strand != null)
            {
                strand.Shave(shaveSpeed * Time.deltaTime);
            }

            HairPatch patch = hit.collider.GetComponentInParent<HairPatch>();
            if (patch != null)
            {
                patch.ShaveNearPoint(hit.point, shaveRadius, shaveSpeed * Time.deltaTime);
            }
        }
    }

    private void OnGUI()
    {
        if (!showCrosshair) return;

        float dotSize = 6f;
        float x = (Screen.width - dotSize) * 0.5f;
        float y = (Screen.height - dotSize) * 0.5f;

        Color prevColor = GUI.color;
        GUI.color = crosshairColor;
        GUI.Box(new Rect(x, y, dotSize, dotSize), GUIContent.none);
        GUI.color = prevColor;
    }
}
