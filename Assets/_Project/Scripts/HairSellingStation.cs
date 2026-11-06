using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public sealed class HairSellingStation : MonoBehaviour
{
    [Header("Sale Settings")]
    [SerializeField, Min(1)] private int coinsPerHairUnit = 1;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;

    [Header("Interaction")]
    [SerializeField] private Vector3 interactionZoneSize = new Vector3(5f, 3f, 5f);
    [SerializeField] private float feedbackDuration = 3f;

    private HairInventoryManager hairInventory;
    private EconomyManager economyManager;
    private bool playerInRange;
    private string feedbackMessage;
    private float feedbackExpiryTime;

    private void Awake()
    {
        BoxCollider interactionZone = GetComponent<BoxCollider>();
        interactionZone.isTrigger = true;
        interactionZone.size = interactionZoneSize;
        interactionZone.center = new Vector3(0f, interactionZoneSize.y * 0.5f, 0f);

        hairInventory = FindFirstObjectByType<HairInventoryManager>();
        economyManager = FindFirstObjectByType<EconomyManager>();
        CreateStationVisuals();
    }

    private void OnValidate()
    {
        coinsPerHairUnit = Mathf.Max(1, coinsPerHairUnit);
        interactionZoneSize.x = Mathf.Max(0.5f, interactionZoneSize.x);
        interactionZoneSize.y = Mathf.Max(0.5f, interactionZoneSize.y);
        interactionZoneSize.z = Mathf.Max(0.5f, interactionZoneSize.z);
        feedbackDuration = Mathf.Max(0.1f, feedbackDuration);
    }

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(interactionKey))
        {
            SellAllHair();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other))
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other))
        {
            playerInRange = false;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (IsPlayer(other))
        {
            playerInRange = true;
        }
    }

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player") || other.transform.root.CompareTag("Player");
    }

    private void SellAllHair()
    {
        if (hairInventory == null || economyManager == null)
        {
            ShowFeedback("The selling station is not connected to the game managers.");
            return;
        }

        int hairUnits = hairInventory.HairUnits;
        if (hairUnits <= 0)
        {
            ShowFeedback("You do not have any hair to sell yet.");
            return;
        }

        long payout = (long)hairUnits * coinsPerHairUnit;
        if (payout > int.MaxValue || economyManager.Coins > int.MaxValue - payout)
        {
            ShowFeedback("The sale would exceed the supported coin balance.");
            return;
        }

        if (!hairInventory.RemoveHair(hairUnits))
        {
            ShowFeedback("The hair inventory changed before the sale could complete.");
            return;
        }

        if (!economyManager.AddCoins((int)payout))
        {
            hairInventory.AddHair(hairUnits);
            ShowFeedback("The sale could not be completed. Your hair was returned.");
            return;
        }

        ShowFeedback($"Sold {hairUnits} hair for {payout} coins.");
    }

    private void ShowFeedback(string message)
    {
        feedbackMessage = message;
        feedbackExpiryTime = Time.unscaledTime + feedbackDuration;
    }

    private void OnGUI()
    {
        if (!playerInRange)
        {
            return;
        }

        int hairUnits = hairInventory != null ? hairInventory.HairUnits : 0;
        int coins = economyManager != null ? economyManager.Coins : 0;
        long payout = (long)hairUnits * coinsPerHairUnit;
        string prompt = $"Press {interactionKey} to sell all hair";
        string inventorySummary = hairUnits > 0
            ? $"Inventory: {hairUnits} hair    Sale value: {payout} coins    Balance: {coins} coins"
            : $"Inventory: 0 hair    Balance: {coins} coins";

        const float panelWidth = 460f;
        const float panelHeight = 86f;
        float panelX = (Screen.width - panelWidth) * 0.5f;
        float panelY = Screen.height - panelHeight - 32f;

        GUI.Box(new Rect(panelX, panelY, panelWidth, panelHeight), "Hair Selling Station");
        GUI.Label(new Rect(panelX + 12f, panelY + 28f, panelWidth - 24f, 22f), prompt);
        GUI.Label(new Rect(panelX + 12f, panelY + 52f, panelWidth - 24f, 20f), inventorySummary);

        if (!string.IsNullOrEmpty(feedbackMessage) && Time.unscaledTime < feedbackExpiryTime)
        {
            GUI.Label(new Rect(panelX, panelY - 28f, panelWidth, 22f), feedbackMessage);
        }
    }

    private void CreateStationVisuals()
    {
        Material creamMaterial = CreateMaterial(new Color(1f, 0.84f, 0.57f));
        Material tealMaterial = CreateMaterial(new Color(0.08f, 0.48f, 0.52f));
        Material coralMaterial = CreateMaterial(new Color(0.91f, 0.25f, 0.22f));
        Material goldMaterial = CreateMaterial(new Color(1f, 0.67f, 0.12f));
        Material darkMaterial = CreateMaterial(new Color(0.22f, 0.12f, 0.08f));

        CreateVisualCube("Cream Counter", new Vector3(0f, 0.48f, 0f), new Vector3(1.8f, 0.96f, 0.95f), creamMaterial);
        CreateVisualCube("Teal Counter Front", new Vector3(0f, 0.48f, 0.49f), new Vector3(1.62f, 0.7f, 0.06f), tealMaterial);
        CreateVisualCube("Sign Border", new Vector3(0f, 1.48f, 0.09f), new Vector3(1.78f, 0.56f, 0.035f), goldMaterial);
        CreateVisualCube("Sign Face", new Vector3(0f, 1.48f, 0.112f), new Vector3(1.68f, 0.46f, 0.03f), coralMaterial);

        CreateVisualCube("Left Support", new Vector3(-0.82f, 1.35f, 0f), new Vector3(0.12f, 1.12f, 0.12f), creamMaterial);
        CreateVisualCube("Right Support", new Vector3(0.82f, 1.35f, 0f), new Vector3(0.12f, 1.12f, 0.12f), creamMaterial);
        CreateVisualCube("Awning Top", new Vector3(0f, 1.95f, 0f), new Vector3(2.08f, 0.18f, 1.12f), creamMaterial);

        const int stripeCount = 7;
        float stripeWidth = 2f / stripeCount;
        for (int stripe = 0; stripe < stripeCount; stripe++)
        {
            float x = -1f + stripeWidth * (stripe + 0.5f);
            Material stripeMaterial = stripe % 2 == 0 ? coralMaterial : creamMaterial;
            CreateVisualCube($"Awning Stripe {stripe + 1}", new Vector3(x, 1.81f, 0f),
                new Vector3(stripeWidth, 0.22f, 1.12f), stripeMaterial);
        }

        CreateVisualCylinder("Sign Coin Emblem", new Vector3(0f, 1.48f, 0.15f),
            new Vector3(0.24f, 0.045f, 0.24f), Quaternion.Euler(90f, 0f, 0f), goldMaterial);
        CreateVisualSphere("Sign Hair Curl Left", new Vector3(-0.27f, 1.56f, 0.15f),
            new Vector3(0.16f, 0.12f, 0.1f), darkMaterial);
        CreateVisualSphere("Sign Hair Curl Right", new Vector3(0.27f, 1.56f, 0.15f),
            new Vector3(0.16f, 0.12f, 0.1f), darkMaterial);

        CreateVisualCylinder("Golden Coin Emblem", new Vector3(0f, 0.48f, 0.54f),
            new Vector3(0.28f, 0.06f, 0.28f), Quaternion.Euler(90f, 0f, 0f), goldMaterial);
        CreateVisualSphere("Hair Bundle Display", new Vector3(0f, 1.05f, 0f),
            new Vector3(0.32f, 0.18f, 0.24f), darkMaterial);
        CreateVisualSphere("Hair Curl Left", new Vector3(-0.18f, 1.08f, 0.02f),
            new Vector3(0.18f, 0.17f, 0.18f), darkMaterial);
        CreateVisualSphere("Hair Curl Right", new Vector3(0.18f, 1.08f, 0.02f),
            new Vector3(0.18f, 0.17f, 0.18f), darkMaterial);
    }

    private void CreateVisualCube(string objectName, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = objectName;
        part.transform.SetParent(transform, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;

        Collider partCollider = part.GetComponent<Collider>();
        Destroy(partCollider);

        MeshRenderer renderer = part.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
    }

    private void CreateVisualCylinder(string objectName, Vector3 localPosition, Vector3 localScale,
        Quaternion localRotation, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        part.name = objectName;
        part.transform.SetParent(transform, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.transform.localRotation = localRotation;
        Destroy(part.GetComponent<Collider>());
        part.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    private void CreateVisualSphere(string objectName, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        part.name = objectName;
        part.transform.SetParent(transform, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        Destroy(part.GetComponent<Collider>());
        part.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    private Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("HDRP/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (shader == null)
        {
            return null;
        }

        Material material = new Material(shader)
        {
            color = color
        };

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        return material;
    }
}
