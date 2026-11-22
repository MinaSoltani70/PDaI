using UnityEngine;

public interface IUpgradeListener
{
    void OnUpgradesChanged(UpgradeManager manager);
}