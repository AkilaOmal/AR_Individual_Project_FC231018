using UnityEngine;

public abstract class BaseModule : MonoBehaviour
{
    public string moduleName;
    [TextArea] public string explanation;
    public abstract void ResetModule();   // return to initial state
    public virtual void OnShown() { ResetModule(); }
    public virtual void OnHidden() { }
}