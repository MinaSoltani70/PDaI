using UnityEngine;

public class BasicShavingTool : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float shaveRange = 5.0f;       // max range shaver
    [SerializeField] private float shaveRadius = 0.6f;      // areas
    [SerializeField] private LayerMask shavableLayer = ~0;

    [Header("Speed Upgrade Settings (#20)")]
    [SerializeField] private float baseShaveSpeed = 1.2f;       // lv 1 speed
    [SerializeField] private float speedIncreasePerLevel = 0.6f; 
    [SerializeField] private int currentSpeedLevel = 1;         
    [SerializeField] private int maxSpeedLevel = 5;             // max lv

    [Header("Crosshair UI")]
    [SerializeField] private bool showCrosshair = false; 
    [SerializeField] private Color crosshairColor = Color.white;

    [Header("Cutting Area Visual Ring")]
    [SerializeField] private LineRenderer visualRing;
    [SerializeField] private int ringSegments = 40;
    [SerializeField] private float ringLineWidth = 0.03f;
    [SerializeField] private Color ringIdleColor = Color.white;
    [SerializeField] private Color ringCuttingColor = new Color(0.4f, 1f, 0.5f, 1f);

    private HairInventoryManager hairInventory;
    private bool missingInventoryLogged;
    private string feedbackText;
    private float feedbackExpiryTime;

    // read recent speed (gameplay & shop)
    public float CurrentShaveSpeed => baseShaveSpeed + ((currentSpeedLevel - 1) * speedIncreasePerLevel);
    public int CurrentSpeedLevel => currentSpeedLevel;
    public int MaxSpeedLevel => maxSpeedLevel;

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
                }
            }
            if (mat != null) visualRing.material = mat;
        }

        visualRing.enabled = false;
    }

    private void Update()
    {
        // pres u for more speed
        if (Input.GetKeyDown(KeyCode.U))
        {
            UpgradeShavingSpeed();
        }

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
            Color activeColor = isCutting ? ringCuttingColor : ringIdleColor;
            visualRing.startColor = activeColor;
            visualRing.endColor = activeColor;

            DrawCircleOnSurface(hit.point, hit.normal, shaveRadius);
        }
        else
        {
            visualRing.enabled = false;
        }
    }

    private void DrawCircleOnSurface(Vector3 center, Vector3 normal, float radius)
    {
        Vector3 surfaceCenter = center + normal * 0.008f;
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

    // accuration shaver Function using upgrading speed
    private void PerformShave()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null) return;
        }

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, shaveRange, shavableLayer))
        {
            Collider[] collidersInRange = Physics.OverlapSphere(hit.point, shaveRadius, shavableLayer);
            int totalHarvested = 0;
            float currentSpeed = CurrentShaveSpeed; // recent upgrade speed 

            foreach (var col in collidersInRange)
            {
                HairStrand strand = col.GetComponent<HairStrand>();
                if (strand != null && !strand.IsShaved())
                {
                    if (strand.Shave(currentSpeed * Time.deltaTime))
                    {
                        totalHarvested++;
                    }
                }
            }

            if (totalHarvested > 0)
            {
                AddHarvestedHair(totalHarvested);
            }
        }
    }

    // public function to lv up speed (from Shop)
    public bool UpgradeShavingSpeed()
    {
        if (currentSpeedLevel >= maxSpeedLevel)
        {
            ShowFeedback($"Speed Maxed! (Lv.{currentSpeedLevel})");
            return false;
        }

        currentSpeedLevel++;
        ShowFeedback($"Shaving Speed Upgraded! Lv.{currentSpeedLevel} ({CurrentShaveSpeed:F1}x)");
        return true;
    }

    // speed function (ex: load data game)
    public void SetSpeedLevel(int level)
    {
        currentSpeedLevel = Mathf.Clamp(level, 1, maxSpeedLevel);
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

        ShowFeedback(amount == 1 ? "+1 Hair" : $"+{amount} Hair");
    }

    private void ShowFeedback(string message)
    {
        feedbackText = message;
        feedbackExpiryTime = Time.unscaledTime + 1.8f;
    }

    private void OnGUI()
    {
        // pop up notification
        if (!string.IsNullOrEmpty(feedbackText) && Time.unscaledTime < feedbackExpiryTime)
        {
            GUIStyle feedbackStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.yellow }
            };

            float panelWidth = 400f;
            float panelHeight = 40f;
            float x = (Screen.width - panelWidth) * 0.5f;
            float y = Screen.height * 0.5f + 40f;

            GUI.Label(new Rect(x, y, panelWidth, panelHeight), feedbackText, feedbackStyle);
        }
    }
}