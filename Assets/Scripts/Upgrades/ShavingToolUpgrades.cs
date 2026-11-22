using UnityEngine;

public sealed class ShavingToolUpgrades : UpgradeListener
{
    private BasicShavingTool tool;
    private float baseRadius;
    private float baseSpeed;
    private float baseRange;

    private void Awake()
    {
        tool = GetComponent<BasicShavingTool>();
        baseRadius = tool.ShaveRadius;
        baseSpeed = tool.ShaveSpeed;
        baseRange = tool.ShaveRange;
    }

    public override void OnUpgradesChanged(UpgradeManager manager)
    {
        tool.ShaveRadius = manager.GetStatValue(UpgradeStat.ShaveRadius, baseRadius);
        tool.ShaveSpeed = manager.GetStatValue(UpgradeStat.ShaveSpeed, baseSpeed);
        tool.ShaveRange = manager.GetStatValue(UpgradeStat.ShaveRange, baseRange);
    }
}