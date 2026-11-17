using UnityEngine;
using TMPro; // Wajib diimpor untuk mengakses komponen TextMeshPro

public class GameplayHUD : MonoBehaviour
{
    [Header("UI Text References")]
    [SerializeField] private TextMeshProUGUI hairText;
    [SerializeField] private TextMeshProUGUI coinsText;

    [Header("Display Format")]
    [SerializeField] private string hairPrefix = "Hair: ";
    [SerializeField] private string coinsPrefix = "Coins: ";

    private HairInventoryManager hairInventory;
    private EconomyManager economyManager;

    private void Start()
    {
        // Cari pengelola inventori dan ekonomi yang ada di scene
        hairInventory = FindFirstObjectByType<HairInventoryManager>();
        economyManager = FindFirstObjectByType<EconomyManager>();

        // Hubungkan (subscribe) ke event perubahan data
        if (hairInventory != null)
        {
            hairInventory.HairUnitsChanged += UpdateHairDisplay;
            // Tampilkan angka awal yang sudah tersimpan
            UpdateHairDisplay(hairInventory.HairUnits);
        }

        if (economyManager != null)
        {
            economyManager.CoinsChanged += UpdateCoinsDisplay;
            // Tampilkan angka awal koin yang sudah tersimpan
            UpdateCoinsDisplay(economyManager.Coins);
        }
    }

    private void OnDestroy()
    {
        // Selalu lepas langganan (unsubscribe) saat objek dihancurkan agar tidak bocor memori
        if (hairInventory != null)
        {
            hairInventory.HairUnitsChanged -= UpdateHairDisplay;
        }

        if (economyManager != null)
        {
            economyManager.CoinsChanged -= UpdateCoinsDisplay;
        }
    }

    // Fungsi ini otomatis dipanggil saat jumlah bulu bertambah atau berkurang
    private void UpdateHairDisplay(int currentHair)
    {
        if (hairText != null)
        {
            hairText.text = hairPrefix + currentHair;
        }
    }

    // Fungsi ini otomatis dipanggil saat koin bertambah atau berkurang
    private void UpdateCoinsDisplay(int currentCoins)
    {
        if (coinsText != null)
        {
            coinsText.text = coinsPrefix + currentCoins;
        }
    }

    private void Update()
    {
        // Tombol tes debug: tekan H untuk tambah bulu, C untuk tambah koin
        if (Input.GetKeyDown(KeyCode.H) && hairInventory != null)
        {
            hairInventory.AddHair(1);
        }

        if (Input.GetKeyDown(KeyCode.C) && economyManager != null)
        {
            economyManager.AddCoins(10);
        }
    }

}

