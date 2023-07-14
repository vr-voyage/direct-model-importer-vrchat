
using UdonSharp;
using UnityEngine;
using VoyageVRSNS;
using VRC.SDKBase;
using VRC.Udon;

public class DMISelectMaterialButton : UdonSharpBehaviour
{
    public int materialIndex = 0;
    public ModelImporterPanel panel;

    public override void Interact()
    {
        Debug.Log($"[{name}] Interact !");
        if (panel == null) return;
        Debug.Log("Show !");
        panel.ShowMaterial(materialIndex);
    }
}
