using UnityEngine;

public class HairStrand : MonoBehaviour
{
    [Header("Hair Settings")]
    [SerializeField] private float initialLength = 1.0f;
    [SerializeField] private float currentLength = 1.0f;
    [SerializeField] private float minLength = 0.05f;

    [Header("Regrowth")]
    [SerializeField] private bool canRegrow = true;
    [SerializeField] private float regrowthDelay = 4.0f;
    [SerializeField] private float regrowthSpeed = 0.3f;

    [Header("Mesh Pivot")]
    [SerializeField] private bool pivotAtRoot = false;
    [SerializeField] private float meshHalfHeight = 1.0f;

    private Vector3 originalScale;
    private Vector3 originalLocalPos;
    private float timeSinceLastShaved;
    private bool fullyCut;

    public Vector3 RootPosition => pivotAtRoot ? transform.position
        : transform.position - transform.up * (meshHalfHeight * transform.lossyScale.y);

    private void Awake()
    {
        originalScale = transform.localScale;
        originalLocalPos = transform.localPosition;
        currentLength = initialLength;
        enabled = false;
    }

    private void Update()
    {
        if (fullyCut || !canRegrow || currentLength >= initialLength)
        {
            enabled = false;
            return;
        }

        timeSinceLastShaved += Time.deltaTime;
        if (timeSinceLastShaved < regrowthDelay) return;

        currentLength = Mathf.Min(initialLength, currentLength + regrowthSpeed * Time.deltaTime);
        ApplyScaleAndPosition();

        if (currentLength >= initialLength) enabled = false;
    }

    public bool Shave(float amount)
    {
        if (fullyCut) return false;

        timeSinceLastShaved = 0f;
        currentLength -= amount;

        if (currentLength <= minLength)
        {
            currentLength = 0f;
            fullyCut = true;
            enabled = false;
            gameObject.SetActive(false);
            return true;
        }

        ApplyScaleAndPosition();
        if (canRegrow) enabled = true;
        return false;
    }

    private void ApplyScaleAndPosition()
    {
        float ratio = currentLength / initialLength;
        float newScaleY = originalScale.y * ratio;
        transform.localScale = new Vector3(originalScale.x, newScaleY, originalScale.z);

        if (!pivotAtRoot)
        {
            float yOffset = (originalScale.y - newScaleY) * meshHalfHeight;
            transform.localPosition = originalLocalPos - transform.localRotation * Vector3.up * yOffset;
        }
    }

    public bool IsShaved() => fullyCut;
}