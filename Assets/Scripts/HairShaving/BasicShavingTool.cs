using UnityEngine;

public class BasicShavingTool : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float shaveRange = 5.0f;
    [SerializeField] private float toolHeadRadius = 0.35f;
    [SerializeField] private float shaveRadius = 0.6f;
    [SerializeField] private float shaveSpeed = 1.2f;
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

    private Material runtimeRingMaterial;
    private HairInventoryManager hairInventory;
    private bool missingInventoryLogged;
    private string harvestFeedback;
    private float harvestFeedbackExpiryTime;

    public float ShaveRadius { get => shaveRadius; set => shaveRadius = Mathf.Max(0.01f, value); }
    public float ShaveSpeed { get => shaveSpeed; set => shaveSpeed = Mathf.Max(0f, value); }
    public float ShaveRange { get => shaveRange; set => shaveRange = Mathf.Max(0.1f, value); }

    private void Start()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        FindHairInventory();
        SetupVisualRing();
    }

    private void OnDestroy()
    {
        if (runtimeRingMaterial != null) Destroy(runtimeRingMaterial);
    }

    private void OnDisable()
    {
        if (visualRing != null) visualRing.enabled = false;
    }

    private void SetupVisualRing()
    {
        if (visualRing == null)
        {
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
                    runtimeRingMaterial = mat;
                }
            }
            if (mat != null) visualRing.sharedMaterial = mat;
        }

        visualRing.enabled = false;
    }

    private void Update()
    {
        bool isClicking = IsShaveButtonHeld();
        UpdateVisualRing(isClicking);
        if (isClicking) PerformShave();
    }

    private static bool IsShaveButtonHeld()
    {
#if ENABLE_INPUT_SYSTEM
        var mouse = UnityEngine.InputSystem.Mouse.current;
        return mouse != null && mouse.leftButton.isPressed;
#else
        return Input.GetMouseButton(0);
#endif
    }

    private void UpdateVisualRing(bool isCutting)
    {
        if (visualRing == null || playerCamera == null) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, shaveRange, shavableLayer, QueryTriggerInteraction.Ignore))
        {
            visualRing.enabled = true;

            Color activeColor = isCutting ? ringCuttingColor : ringIdleColor;
            visualRing.startColor = activeColor;
            visualRing.endColor = activeColor;

            DrawCircleOnSurface(hit.point, hit.normal, shaveRadius);
        }
        else visualRing.enabled = false;
    }

    private void DrawCircleOnSurface(Vector3 center, Vector3 normal, float radius)
    {
        Vector3 surfaceCenter = center + normal * 0.008f;

        Vector3 helper = Mathf.Abs(normal.y) > 0.99f ? Vector3.forward : Vector3.up;
        Vector3 tangent = Vector3.Cross(normal, helper).normalized;
        Vector3 bitangent = Vector3.Cross(normal, tangent).normalized;

        if (visualRing.positionCount != ringSegments) visualRing.positionCount = ringSegments;

        for (int i = 0; i < ringSegments; i++)
        {
            float angle = (i / (float)ringSegments) * Mathf.PI * 2f;
            Vector3 point = surfaceCenter + (tangent * Mathf.Cos(angle) + bitangent * Mathf.Sin(angle)) * radius;
            visualRing.SetPosition(i, point);
        }
    }

    private void PerformShave()
    {
        FindHairInventory();

        if (hairInventory == null)
        {
            return;
        }

        if (hairInventory.IsFull)
        {
            harvestFeedback = "Inventory full!";
            harvestFeedbackExpiryTime = Time.unscaledTime + 1.5f;
            return;
        }
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null) return;
        }

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        bool hasHit = Physics.SphereCast(ray, toolHeadRadius, out hit, shaveRange, shavableLayer, QueryTriggerInteraction.Ignore);
        if (!hasHit) hasHit = Physics.Raycast(ray, out hit, shaveRange, shavableLayer, QueryTriggerInteraction.Ignore);
        if (!hasHit) return;

        float cut = shaveSpeed * Time.deltaTime;

        IShaveable target = hit.collider.GetComponentInParent<IShaveable>();
        if (target != null)
        {
            AddHarvestedHair(target.ShaveNearPoint(hit.point, shaveRadius, cut));
            return;
        }

        HairStrand strand = hit.collider.GetComponent<HairStrand>();
        if (strand != null && strand.Shave(cut)) AddHarvestedHair(1);
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
        if (amount <= 0) return;

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