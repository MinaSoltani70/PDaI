using UnityEngine;

[CreateAssetMenu(fileName = "ScissorDefinition", menuName = "A Hairy Business/Scissor Definition")]
public sealed class ScissorDefinition : ScriptableObject
{
    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [SerializeField, Min(0)] private int price;
    [SerializeField, Min(0.1f)] private float shaveRange = 3.5f;
    [SerializeField, Min(0f)] private float toolHeadRadius = 0.35f;
    [SerializeField, Min(0.01f)] private float cuttingRadius = 0.4f;
    [SerializeField, Min(0.01f)] private float shaveSpeed = 0.8f;

    public string Id => id;
    public string DisplayName => displayName;
    public int Price => price;
    public float ShaveRange => shaveRange;
    public float ToolHeadRadius => toolHeadRadius;
    public float CuttingRadius => cuttingRadius;
    public float ShaveSpeed => shaveSpeed;

    private void OnValidate()
    {
        price = Mathf.Max(0, price);
        shaveRange = Mathf.Max(0.1f, shaveRange);
        toolHeadRadius = Mathf.Max(0f, toolHeadRadius);
        cuttingRadius = Mathf.Max(0.01f, cuttingRadius);
        shaveSpeed = Mathf.Max(0.01f, shaveSpeed);
    }
}
