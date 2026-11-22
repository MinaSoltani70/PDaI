using UnityEngine;
using System;

[Serializable]
public class UpgradePrerequisite
{
    public UpgradeDefinition upgrade;
    public int minLevel = 1;
}