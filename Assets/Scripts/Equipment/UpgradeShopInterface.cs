using StarterAssets;
using UnityEngine;
using UnityEngine.UI;

public sealed class UpgradeShopInterface : MonoBehaviour
{
    [SerializeField] private ToolEquipmentManager equipmentManager;

    public bool IsOpen { get; private set; }

    private Canvas shopCanvas;
    private Text balanceLabel;
    private Text feedbackLabel;
    private RectTransform cardsRoot;
    private float previousTimeScale;
    private CursorLockMode previousCursorLockMode;
    private bool previousCursorVisibility;
    private bool previousControllerEnabled;
    private bool previousShavingToolEnabled;
    private bool previousCursorLocked;
    private bool previousCursorInputForLook;
    private FirstPersonController playerController;
    private StarterAssetsInputs playerInputs;
    private BasicShavingTool shavingTool;

    private static readonly Color OverlayColor = new Color(0.02f, 0.07f, 0.08f, 0.74f);
    private static readonly Color PanelColor = new Color(0.97f, 0.91f, 0.78f, 1f);
    private static readonly Color TealColor = new Color(0.04f, 0.28f, 0.33f, 1f);
    private static readonly Color CoralColor = new Color(0.83f, 0.22f, 0.18f, 1f);
    private static readonly Color GoldColor = new Color(0.94f, 0.62f, 0.12f, 1f);

    private void Awake()
    {
        if (equipmentManager == null)
        {
            equipmentManager = FindFirstObjectByType<ToolEquipmentManager>();
        }

        playerController = FindFirstObjectByType<FirstPersonController>();
        playerInputs = FindFirstObjectByType<StarterAssetsInputs>();
        shavingTool = FindFirstObjectByType<BasicShavingTool>();
        BuildInterface();
    }

    private void Update()
    {
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    private void OnDestroy()
    {
        if (IsOpen)
        {
            Close();
        }
    }

    public void Open()
    {
        if (IsOpen || equipmentManager == null)
        {
            return;
        }

        previousTimeScale = Time.timeScale;
        previousCursorLockMode = Cursor.lockState;
        previousCursorVisibility = Cursor.visible;
        Time.timeScale = 0f;

        if (playerController != null)
        {
            previousControllerEnabled = playerController.enabled;
            playerController.enabled = false;
        }

        if (shavingTool != null)
        {
            previousShavingToolEnabled = shavingTool.enabled;
            shavingTool.enabled = false;
        }

        if (playerInputs != null)
        {
            previousCursorLocked = playerInputs.cursorLocked;
            previousCursorInputForLook = playerInputs.cursorInputForLook;
            playerInputs.cursorLocked = false;
            playerInputs.cursorInputForLook = false;
            playerInputs.move = Vector2.zero;
            playerInputs.look = Vector2.zero;
            playerInputs.jump = false;
            playerInputs.sprint = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        feedbackLabel.text = "Choose a pair of scissors.";
        RefreshCards();
        shopCanvas.enabled = true;
        IsOpen = true;
    }

    public void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        shopCanvas.enabled = false;
        IsOpen = false;
        Time.timeScale = previousTimeScale;

        if (playerController != null)
        {
            playerController.enabled = previousControllerEnabled;
        }

        if (shavingTool != null)
        {
            shavingTool.enabled = previousShavingToolEnabled;
        }

        if (playerInputs != null)
        {
            playerInputs.move = Vector2.zero;
            playerInputs.look = Vector2.zero;
            playerInputs.jump = false;
            playerInputs.sprint = false;
            playerInputs.cursorInputForLook = previousCursorInputForLook;
            playerInputs.cursorLocked = previousCursorLocked;
        }

        Cursor.lockState = previousCursorLockMode;
        Cursor.visible = previousCursorVisibility;
    }

    private void BuildInterface()
    {
        GameObject canvasObject = new GameObject("Upgrade Shop Canvas", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        shopCanvas = canvasObject.GetComponent<Canvas>();
        shopCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        shopCanvas.sortingOrder = 50;
        shopCanvas.enabled = false;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform overlay = CreateRect("Overlay", canvasObject.transform, Vector2.zero,
            Vector2.one, Vector2.zero, Vector2.zero);
        CreateImage("Overlay Background", overlay, OverlayColor, Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero);

        RectTransform panel = CreateRect("Shop Panel", overlay, new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400f, 940f));
        CreateImage("Panel Background", panel, PanelColor, Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero);

        RectTransform header = CreateRect("Header", panel, new Vector2(0f, 1f),
            new Vector2(1f, 1f), new Vector2(0f, -60f), new Vector2(0f, 120f));
        CreateImage("Header Background", header, TealColor, Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero);

        CreateText("Title", header, "SCISSOR SHOP", 48, FontStyle.Bold, Color.white,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(28f, 0f),
            new Vector2(-250f, 0f), TextAnchor.MiddleLeft);
        CreateText("Subtitle", panel, "Upgrade your shaving game", 30, FontStyle.Normal, TealColor,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f),
            new Vector2(760f, 58f), TextAnchor.MiddleCenter);

        balanceLabel = CreateText("Coin Balance", panel, "Coins: 0", 32, FontStyle.Bold, TealColor,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-190f, -150f),
            new Vector2(320f, 58f), TextAnchor.MiddleRight);

        CreateButton("Close Button", header, "CLOSE", 24, CoralColor,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-90f, 0f),
            new Vector2(150f, 70f), Close);

        RectTransform viewport = CreateRect("Scissor Card Viewport", panel,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f),
            new Vector2(1320f, 570f));
        viewport.gameObject.AddComponent<RectMask2D>();
        ScrollRect scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
        scrollRect.viewport = viewport;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        cardsRoot = CreateRect("Scissor Cards", viewport, new Vector2(0f, 1f),
            new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        cardsRoot.pivot = new Vector2(0.5f, 1f);
        GridLayoutGroup cardLayout = cardsRoot.gameObject.AddComponent<GridLayoutGroup>();
        cardLayout.padding = new RectOffset(16, 16, 10, 10);
        cardLayout.cellSize = new Vector2(560f, 500f);
        cardLayout.spacing = new Vector2(32f, 24f);
        cardLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        cardLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        cardLayout.childAlignment = TextAnchor.UpperCenter;
        cardLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        cardLayout.constraintCount = 2;
        ContentSizeFitter contentSize = cardsRoot.gameObject.AddComponent<ContentSizeFitter>();
        contentSize.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentSize.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = cardsRoot;
        feedbackLabel = CreateText("Feedback", panel, "", 27, FontStyle.Bold, TealColor,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(26f, 56f),
            new Vector2(-52f, 56f), TextAnchor.MiddleCenter);

        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            Debug.LogError("Upgrade shop requires an EventSystem with an input module in the scene.", this);
        }
    }

    private void RefreshCards()
    {
        if (equipmentManager == null || cardsRoot == null)
        {
            return;
        }

        for (int i = cardsRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(cardsRoot.GetChild(i).gameObject);
        }

        balanceLabel.text = $"Coins: {equipmentManager.Coins}";
        ScissorDefinition[] catalog = equipmentManager.ScissorCatalog;
        int count = catalog != null ? catalog.Length : 0;
        const float cardWidth = 560f;

        for (int i = 0; i < count; i++)
        {
            ScissorDefinition scissor = catalog[i];
            if (scissor == null)
            {
                continue;
            }

            CreateScissorCard(scissor, cardWidth);
        }
    }

    private void CreateScissorCard(ScissorDefinition scissor, float width)
    {
        RectTransform card = CreateRect(scissor.Id + " Card", cardsRoot,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(width, 500f));
        CreateImage("Card Background", card, new Color(1f, 0.98f, 0.92f, 1f), Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero);

        CreateText("Item Name", card, scissor.DisplayName, 38, FontStyle.Bold, TealColor,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -58f),
            new Vector2(-36f, 76f), TextAnchor.MiddleCenter);

        ScissorDefinition starter = equipmentManager.StarterScissor != null
            ? equipmentManager.StarterScissor
            : scissor;
        float speedChange = GetPercentChange(scissor.ShaveSpeed, starter.ShaveSpeed);
        float areaChange = GetPercentChange(scissor.CuttingRadius * scissor.CuttingRadius,
            starter.CuttingRadius * starter.CuttingRadius);
        string stats = $"Shave speed: {scissor.ShaveSpeed:0.0}  ({speedChange:+0;-0;0}%)\n" +
                       $"Cutting area: {areaChange:+0;-0;0}%\n" +
                       $"Range: {scissor.ShaveRange:0.0}";
        CreateText("Scissor Stats", card, stats, 30, FontStyle.Normal, TealColor,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(32f, -180f),
            new Vector2(-64f, 190f), TextAnchor.MiddleLeft);

        bool owned = equipmentManager.IsOwned(scissor);
        bool equipped = equipmentManager.EquippedScissor == scissor;
        bool canAfford = equipmentManager.Coins >= scissor.Price;
        string actionLabel = equipped
            ? "EQUIPPED"
            : owned
                ? "EQUIP"
                : canAfford
                    ? $"BUY  |  {scissor.Price} COINS"
                    : $"NEED  {scissor.Price} COINS";

        Color buttonColor = equipped ? GoldColor : CoralColor;
        Button actionButton = CreateButton("Scissor Action", card, actionLabel, 28, buttonColor,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 66f),
            new Vector2(width - 64f, 84f), () => PurchaseOrEquip(scissor));
        actionButton.interactable = !equipped && (owned || canAfford);
    }

    private void PurchaseOrEquip(ScissorDefinition scissor)
    {
        if (equipmentManager.TryPurchaseOrEquip(scissor, out string message))
        {
            feedbackLabel.color = TealColor;
        }
        else
        {
            feedbackLabel.color = CoralColor;
        }

        feedbackLabel.text = message;
        RefreshCards();
    }

    private static float GetPercentChange(float value, float baseline)
    {
        return baseline <= 0f ? 0f : (value / baseline - 1f) * 100f;
    }

    private static RectTransform CreateRect(string objectName, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static Image CreateImage(string objectName, Transform parent, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        RectTransform rect = CreateRect(objectName, parent, anchorMin, anchorMax, position, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static Text CreateText(string objectName, Transform parent, string value, int fontSize,
        FontStyle fontStyle, Color color, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 position, Vector2 size, TextAnchor alignment)
    {
        RectTransform rect = CreateRect(objectName, parent, anchorMin, anchorMax, position, size);
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static Button CreateButton(string objectName, Transform parent, string label, int fontSize,
        Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size,
        UnityEngine.Events.UnityAction onClick)
    {
        RectTransform rect = CreateRect(objectName, parent, anchorMin, anchorMax, position, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        Text buttonText = CreateText("Label", rect, label, fontSize, FontStyle.Bold, Color.white,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
        buttonText.raycastTarget = false;
        return button;
    }
}
