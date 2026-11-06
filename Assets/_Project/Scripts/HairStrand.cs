using UnityEngine;

public class HairStrand : MonoBehaviour
{
    [Header("Hair Settings")]
    [SerializeField] private float initialLength = 1.0f;
    [SerializeField] private float currentLength = 1.0f;
    [SerializeField] private float minLength = 0.05f;

    private Vector3 originalScale;
    private Vector3 originalLocalPos;

    private void Awake()
    {
        originalScale = transform.localScale;
        originalLocalPos = transform.localPosition;
        currentLength = initialLength;
    }

    public bool Shave(float amount)
    {
        if (currentLength <= minLength || amount <= 0f) return false;

        currentLength -= amount;

        if (currentLength <= minLength)
        {
            currentLength = 0f;
            gameObject.SetActive(false);
            return true;
        }
        else
        {
            float ratio = currentLength / initialLength;
            float newScaleY = originalScale.y * ratio;
            transform.localScale = new Vector3(originalScale.x, newScaleY, originalScale.z);

            // Keep the base of the strand attached to the surface as it shrinks.
            float yOffset = (originalScale.y - newScaleY);
            transform.localPosition = originalLocalPos - new Vector3(0f, yOffset, 0f);
        }

        return false;
    }

    public bool IsShaved()
    {
        return currentLength <= minLength;
    }
}
