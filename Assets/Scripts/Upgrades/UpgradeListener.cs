using UnityEngine;

public abstract class UpgradeListener : MonoBehaviour, IUpgradeListener
{
    private bool started;

    protected virtual void Start()
    {
        started = true;
        Register();
    }

    protected virtual void OnEnable()
    {
        if (started) Register();
    }

    protected virtual void OnDisable() => RegisterListener(false);

    private void Register() => RegisterListener(true);

    public abstract void OnUpgradesChanged(UpgradeManager manager);

    private void RegisterListener(bool register)
    {
        if (UpgradeManager.Instance == null) return;
        if (register) UpgradeManager.Instance.Subscribe(this); else UpgradeManager.Instance.Unsubscribe(this);
    }
}