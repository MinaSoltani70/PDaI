using UnityEngine;

public class HairStrand : MonoBehaviour
{
    [Header("Hair Settings")]
    [SerializeField] private float initialLength = 1.0f;
    [SerializeField] private float currentLength = 1.0f;
    [SerializeField] private float minLength = 0.05f; // Batas minimal sebelum dianggap habis

    [Header("Regrowth Settings")]
    [SerializeField] private bool canRegrow = true;         // Apakah bulu bisa tumbuh kembali?
    [SerializeField] private float regrowthDelay = 4.0f;    // Jeda waktu (detik) sebelum mulai tumbuh
    [SerializeField] private float regrowthSpeed = 0.3f;    // Kecepatan memanjang per detik

    private Vector3 originalScale;
    private Vector3 originalLocalPos;
    private float timeSinceLastShaved = 0f;

    private void Awake()
    {
        originalScale = transform.localScale;
        originalLocalPos = transform.localPosition;
        currentLength = initialLength;
    }

    private void Update()
    {
        // Jika bulu belum mencapai panjang maksimal, jalankan proses tumbuh kembali
        if (canRegrow && currentLength < initialLength)
        {
            timeSinceLastShaved += Time.deltaTime;

            // Tunggu sampai jeda delay terlewati
            if (timeSinceLastShaved >= regrowthDelay)
            {
                GrowHair(regrowthSpeed * Time.deltaTime);
            }
        }
    }

    // Mengembalikan TRUE hanya saat tebasan ini membuat bulu putus/habis (dipanen)
    public bool Shave(float amount)
    {
        // Kalau sudah gundul, tidak bisa dipanen lagi
        if (currentLength <= minLength) return false;

        timeSinceLastShaved = 0f; // Reset timer jeda tiap kali kena cukur
        currentLength -= amount;

        if (currentLength <= minLength)
        {
            currentLength = 0f;
            SetHairVisible(false);
            return true; // Sukses dipanen! (Ini yang ditunggu HairPatch & ShavingTool)
        }
        else
        {
            ApplyScaleAndPosition();
            return false; // Masih dalam proses memendek
        }
    }

    // Fungsi untuk menumbuhkan kembali bulu perlahan-lahan
    private void GrowHair(float amount)
    {
        // Jika visualnya sempat mati, aktifkan kembali
        if (currentLength <= 0f)
        {
            SetHairVisible(true);
        }

        currentLength += amount;
        if (currentLength > initialLength)
        {
            currentLength = initialLength;
        }

        ApplyScaleAndPosition();
    }

    private void ApplyScaleAndPosition()
    {
        float ratio = currentLength / initialLength;
        float newScaleY = originalScale.y * ratio;
        transform.localScale = new Vector3(originalScale.x, newScaleY, originalScale.z);

        // Menjaga pangkal rambut tetap menempel di permukaan saat memendek/memanjang
        float yOffset = (originalScale.y - newScaleY);
        transform.localPosition = originalLocalPos - new Vector3(0f, yOffset, 0f);
    }

    private void SetHairVisible(bool visible)
    {
        var meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null) meshRenderer.enabled = visible;

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = visible;
    }

    public bool IsShaved()
    {
        return currentLength <= minLength;
    }
}