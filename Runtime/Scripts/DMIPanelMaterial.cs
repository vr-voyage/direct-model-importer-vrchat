
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DMIPanelMaterial : UdonSharpBehaviour
{
    public DMIPanelTextureSlot[] textureSlots;

    public DMIMaterialSlot currentSlot;

    public void CleanPanel()
    {
        int nSlots = textureSlots.Length;
        for (int slot = 0; slot < nSlots; slot++)
        {
            textureSlots[slot].DisableSlot();
        }
        currentSlot = null;
    }

    public void SetupFor(DMIMaterialSlot materialSlot)
    {
        
        CleanPanel();
        if (materialSlot == null) return;
        currentSlot = materialSlot;

        var downloaders = materialSlot.downloaders;
        int nDownloaders = materialSlot.downloaders.Length;
        int nTextureSlots = textureSlots.Length;
        int minUseableSlots = Mathf.Min(nDownloaders, nTextureSlots);

        Debug.Log($"[{name}] minUseableSlots : {minUseableSlots}");
        for (int slot = 0; slot < minUseableSlots; slot++)
        {
            var textureSlot = textureSlots[slot];
            var downloader = downloaders[slot];

            if ((downloader == null) | (textureSlot == null)) continue;

            textureSlot.SetupFor(downloader);
        }
    }


}
