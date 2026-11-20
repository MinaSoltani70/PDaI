using StarterAssets;
using UnityEngine;

public sealed class PlayerSpeedUpgrades : UpgradeListener
{
    private FirstPersonController controller;
    private float baseMoveSpeed;
    private float baseSprintSpeed;

    private void Awake()
    {
        controller = GetComponent<FirstPersonController>();
        baseMoveSpeed = controller.MoveSpeed;
        baseSprintSpeed = controller.SprintSpeed;
    }

    public override void OnUpgradesChanged(UpgradeManager manager)
    {
        controller.MoveSpeed = manager.GetStatValue(UpgradeStat.MoveSpeed, baseMoveSpeed);
        controller.SprintSpeed = manager.GetStatValue(UpgradeStat.MoveSpeed, baseSprintSpeed);
    }
}