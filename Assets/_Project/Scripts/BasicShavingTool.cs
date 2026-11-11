using UnityEngine;

public class BasicShavingTool : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float shaveRange = 5.0f;       // Maximum distance of the shaving tool.
    [SerializeField] private float toolHeadRadius = 0.35f;  // Radius of the shaving tool head for sphere casts.
    [SerializeField] private float shaveRadius = 0.6f;      // Radius shaved around the contact point.
    [SerializeField] private float shaveSpeed = 1.2f;       // Speed at which hair is shaved.
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

    private HairInventoryManager hairInventory;
    private bool missingInventoryLogged;
    private string harvestFeedback;
    private float harvestFeedbackExpiryTime;

    public void ApplyScissorStats(float range, float headRadius, float cuttingRadius, float speed)
    {
        shaveRange = Mathf.Max(0.1f, range);
        toolHeadRadius = Mathf.Max(0f, headRadius);
        shaveRadius = Mathf.Max(0.01f, cuttingRadius);
        shaveSpeed = Mathf.Max(0.01f, speed);
    }

    private void Start()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        FindHairInventory();

        SetupVisualRing();
    }

    private void SetupVisualRing()
    {
        if (visualRing == null)
        {
            // Create a child GameObject automatically when none is assigned in the Inspector.
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

        // Use an HDRP Unlit material when one is available.
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

            // Show white while aiming and green while shaving.
            Color activeColor = isCutting ? ringCuttingColor : ringIdleColor;
            visualRing.startColor = activeColor;
            visualRing.endColor = activeColor;

            DrawCircleOnSurface(hit.point, hit.normal, shaveRadius);
        }
        else
        {
            // Hide the ring when no surface is within range.
            visualRing.enabled = false;
        }
    }

    private void DrawCircleOnSurface(Vector3 center, Vector3 normal, float radius)
    {
        // Offset the ring slightly along the surface normal to avoid z-fighting.
        Vector3 surfaceCenter = center + normal * 0.008f;

        // Calculate an orthogonal tangent and bitangent on the surface plane.
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
            HairPatch patch = hit.collider.GetComponentInParent<HairPatch>();
            if (patch != null)
            {
                AddHarvestedHair(patch.ShaveNearPoint(hit.point, shaveRadius, shaveSpeed * Time.deltaTime));
            }
            else
            {
                HairStrand strand = hit.collider.GetComponent<HairStrand>();
                if (strand != null && strand.Shave(shaveSpeed * Time.deltaTime))
                {
                    AddHarvestedHair(1);
                }
            }
        }
    }

    private void FindHairInventory()
    {
        if (hairInventory == null)
        {
            hairInventory = FindFirstObjectByType<HairInventoryManager>();
        }
    }

    private void AddHarvestedHair(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        FindHairInventory();
        if (hairInventory == null)
        {
            if (!missingInventoryLogged)
            {
                Debug.LogError("Cannot collect harvested hair because no HairInventoryManager exists in the scene.", this);
                missingInventoryLogged = true;
            }

            return;
        }

        if (!hairInventory.AddHair(amount))
        {
            Debug.LogWarning("Could not add harvested hair to the inventory.", this);
            return;
        }

        harvestFeedback = amount == 1 ? "+1 Hair" : $"+{amount} Hair";
        harvestFeedbackExpiryTime = Time.unscaledTime + 1.5f;
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

        if (!string.IsNullOrEmpty(harvestFeedback) && Time.unscaledTime < harvestFeedbackExpiryTime)
        {
            GUIStyle feedbackStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            GUI.Label(new Rect((Screen.width - 220f) * 0.5f, y + 24f, 220f, 36f), harvestFeedback, feedbackStyle);
        }
    }
}
