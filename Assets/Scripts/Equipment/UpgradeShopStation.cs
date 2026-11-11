using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public sealed class UpgradeShopStation : MonoBehaviour
{
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private Vector3 interactionZoneSize = new Vector3(4f, 3f, 4f);
    [SerializeField] private UpgradeShopInterface shopInterface;

    private bool playerInRange;

    private void Awake()
    {
        BoxCollider interactionZone = GetComponent<BoxCollider>();
        interactionZone.isTrigger = true;
        interactionZone.size = interactionZoneSize;
        interactionZone.center = new Vector3(0f, interactionZoneSize.y * 0.5f, 0f);

        if (shopInterface == null)
        {
            shopInterface = GetComponent<UpgradeShopInterface>();
        }

        CreateStandVisuals();
    }

    private void OnValidate()
    {
        interactionZoneSize.x = Mathf.Max(0.5f, interactionZoneSize.x);
        interactionZoneSize.y = Mathf.Max(0.5f, interactionZoneSize.y);
        interactionZoneSize.z = Mathf.Max(0.5f, interactionZoneSize.z);
    }

    private void Update()
    {
        if (!playerInRange || shopInterface == null || shopInterface.IsOpen)
        {
            return;
        }

        if (Input.GetKeyDown(interactionKey))
        {
            shopInterface.Open();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other))
        {
            playerInRange = true;
        }
    }

    private void OnTriggerStay(Collider other)
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

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player") || other.transform.root.CompareTag("Player");
    }

    private void OnGUI()
    {
        if (!playerInRange || shopInterface == null || shopInterface.IsOpen)
        {
            return;
        }

        const float promptWidth = 340f;
        const float promptHeight = 42f;
        float x = (Screen.width - promptWidth) * 0.5f;
        float y = Screen.height - promptHeight - 110f;
        GUI.Box(new Rect(x, y, promptWidth, promptHeight), $"Press {interactionKey} to open the scissor shop");
    }

    private void CreateStandVisuals()
    {
        Material cream = CreateMaterial(new Color(1f, 0.84f, 0.57f));
        Material teal = CreateMaterial(new Color(0.08f, 0.48f, 0.52f));
        Material coral = CreateMaterial(new Color(0.91f, 0.25f, 0.22f));
        Material gold = CreateMaterial(new Color(1f, 0.67f, 0.12f));

        CreateCube("Shop Counter", new Vector3(0f, 0.48f, 0f), new Vector3(1.8f, 0.96f, 0.95f), cream);
        CreateCube("Counter Front", new Vector3(0f, 0.48f, 0.49f), new Vector3(1.62f, 0.7f, 0.06f), teal);
        CreateCube("Shop Sign Border", new Vector3(0f, 1.47f, 0.09f), new Vector3(1.8f, 0.56f, 0.04f), gold);
        CreateCube("Shop Sign", new Vector3(0f, 1.47f, 0.12f), new Vector3(1.68f, 0.46f, 0.03f), coral);
        CreateCube("Left Support", new Vector3(-0.82f, 1.35f, 0f), new Vector3(0.12f, 1.12f, 0.12f), cream);
        CreateCube("Right Support", new Vector3(0.82f, 1.35f, 0f), new Vector3(0.12f, 1.12f, 0.12f), cream);
        CreateCube("Awning", new Vector3(0f, 1.95f, 0f), new Vector3(2.08f, 0.18f, 1.12f), cream);

        const int stripeCount = 7;
        float stripeWidth = 2f / stripeCount;
        for (int stripe = 0; stripe < stripeCount; stripe++)
        {
            float x = -1f + stripeWidth * (stripe + 0.5f);
            CreateCube($"Awning Stripe {stripe + 1}", new Vector3(x, 1.81f, 0f),
                new Vector3(stripeWidth, 0.22f, 1.12f), stripe % 2 == 0 ? coral : cream);
        }

        CreateCube("Scissor Handle Left", new Vector3(-0.2f, 1.46f, 0.16f),
            new Vector3(0.12f, 0.18f, 0.04f), gold);
        CreateCube("Scissor Handle Right", new Vector3(0.2f, 1.46f, 0.16f),
            new Vector3(0.12f, 0.18f, 0.04f), gold);
        CreateCube("Scissor Blades", new Vector3(0f, 1.58f, 0.16f),
            new Vector3(0.38f, 0.035f, 0.035f), cream);
    }

    private void CreateCube(string objectName, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
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

        Material material = new Material(shader);
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        return material;
    }
}
