
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDK3.Image;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class DMITextureDownloader : UdonSharpBehaviour
{
    public Material material;

    public VRCUrl url;
    public VRCUrl previousUrl;

    [UdonSynced]
    public string property;
    [UdonSynced]
    public VRCUrl syncedUrl;

    VRCImageDownloader downloader;
    IVRCImageDownload downloadState;

    DMIPanelTextureSlot panel;

    bool IsOwner()
    {
        return Networking.LocalPlayer == Networking.GetOwner(gameObject);
    }

    void ApplyTexture()
    {
        if ((material == null) | (property == null) | (downloadState == null)) return;
        if ((property.Trim() == "") | (downloadState.Result == null)) return;

        material.SetTexture(property, downloadState.Result);
        

    }

    void OnEnable()
    {
        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        if (localPlayer == null)
        {
            Debug.LogError("[DMITextureDownloader] Broken VRChat !");
            gameObject.SetActive(false);
            return;
        }

        ApplyTexture();

        if (!IsOwner())
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "Resync");
        }
    }

    public void Resync()
    {
        PrepareToSync();
    }

    public void Cleanup()
    {
        if ((property != null) & (material != null))
        {
            material.SetTexture(property, null);
        }

        downloadState = null;
        if (downloader != null)
        {
            downloader.Dispose();
        }
        
        previousUrl = url;
        ShowStatus("Nothing loaded");
    }

    public void DownloadTexture(VRCUrl textureUrl)
    {

        if (textureUrl == null) return;

        url = textureUrl;

        if (url == VRCUrl.Empty)
        {
            Cleanup();
            return;
        }

        if (url == previousUrl) return;

        StartDownload();
        PrepareToSync();
    }

    public void SetPanel(DMIPanelTextureSlot newPanel)
    {
        panel = newPanel;
    }

    public void ResetPanel()
    {
        panel = null;
    }

    void ShowStatus(string message)
    {
        if (panel != null)
        {
            panel.ShowStatus(message);
        }
    }

    void StartDownload()
    {
        Cleanup();
        downloader = new VRCImageDownloader();
        downloadState = downloader.DownloadImage(
            url: url,
            material: null,
            udonBehaviour: (VRC.Udon.Common.Interfaces.IUdonEventReceiver)this);
        ShowStatus("Download queued");
    }

    public override void OnImageLoadSuccess(IVRCImageDownload result)
    {
        if ((material == null) | (property == null))
        {
            Cleanup();
            return;
        }

        ApplyTexture();
        ShowStatus("Downloaded");
    }

    public override void OnImageLoadError(IVRCImageDownload result)
    {
        Debug.LogError($"[DMITextureDownloader] Could not load {result.Url} : {result.ErrorMessage} ");
        ShowStatus("Error : " + result.ErrorMessage);
        Cleanup();
        return;
    }

    void PrepareToSync()
    {
        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        /* Play mode without Client Sim or quitting the instance */
        if (localPlayer == null) return;

        if (Networking.GetOwner(gameObject) != localPlayer)
        {
            Networking.SetOwner(localPlayer, gameObject);
            return;
        }

        IGotOwnership();
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        if (player == Networking.LocalPlayer)
        {
            IGotOwnership();
        }
    }

    public override void OnPreSerialization()
    {
        syncedUrl = url;
    }

    void IGotOwnership()
    {
        RequestSerialization();
    }

    public override void OnDeserialization()
    {
        if (syncedUrl == url) return;
        url = syncedUrl;

        if (syncedUrl == VRCUrl.Empty)
        {
            Cleanup();
            return;
        }

        StartDownload();
    }
}
