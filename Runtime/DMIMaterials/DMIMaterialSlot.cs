
using System.Collections;
using UdonSharp;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.UIElements;
using VRC.SDK3.Image;
using VRC.SDKBase;
using VRC.Udon;

public class DMIMaterialSlot : UdonSharpBehaviour
{
    public DMIDatabase database;

    [UdonSynced]
    public int syncedShader;

    int currentShader = 0;

    public DMITextureDownloader[] downloaders;

    public Material material;

    bool cleanupUnusedSlots = true;

    public void Disable()
    {
        DisconnectTextureSlots();
        gameObject.SetActive(false);
        material = null;
    }

    public void SetupFor(Material newMaterial)
    {
        if (newMaterial == null)
        {
            Debug.LogWarning("[DMIMaterialSlot] new material is null !");
            Disable();
            return;
        }

        Debug.LogWarning("[DMIMaterialSlot] " + newMaterial.GetInstanceID());
        material = newMaterial;
        gameObject.SetActive(true);
        currentShader = database.GetShaderID(material.shader);
        currentShader = (currentShader > 0 ? currentShader : -1);
        RefreshTextureSlots();
        
    }

    public void Resync()
    {
        PrepareToSync();
    }
    void SetNewShader()
    {
        if (material == null) return;

        Shader newShader = database.GetShader(currentShader);
        if (newShader == null) return;

        material.shader = newShader;

        RefreshTextureSlots();
    }

    public void SetShader(int shaderID)
    {
        currentShader = shaderID;
        SetNewShader();
        PrepareToSync();
    }

    public void Cleanup()
    {
        DisconnectTextureSlots();
    }

    void DisconnectTextureSlots()
    {
        int nSlots = downloaders.Length;

        if (cleanupUnusedSlots)
        {
            for (int s = 0; s < nSlots; s++)
            {
                var downloader = downloaders[s];
                if (downloader == null) continue;
                downloader.Cleanup();
            }
        }

        for (int s = 0; s < nSlots; s++)
        {
            var downloader = downloaders[s];
            if (downloader == null) continue;
            downloader.gameObject.SetActive(false);
        }
    }

    void ConnectTextureSlots(params string[] properties)
    {
        int nProperties = properties.Length;
        int nTextureDownloaders = downloaders.Length;
        int minSetupProperties = Mathf.Min(nProperties, nTextureDownloaders);
        for (int p = 0; p < minSetupProperties; p++)
        {
            var downloader = downloaders[p];
            string property = properties[p];
            if ((downloader == null) | (property == null))
            {
                Debug.LogWarning($"[DMIMaterialSlot] Downloader {p} or property {p} was null !");
                continue;
            }
            downloader.material = material;
            downloader.property = property;
            downloader.gameObject.SetActive(true);
        }
    }

    void RefreshTextureSlots()
    {
        if (material == null) Disable();
        DisconnectTextureSlots();
        ConnectTextureSlots(material.GetTexturePropertyNames());
    }

    private void OnEnable()
    {
        currentShader = 0;
        SetNewShader();
    }

    private void OnDisable()
    {
        Destroy(material);
    }

    bool IsOwner()
    {
        return Networking.LocalPlayer == Networking.GetOwner(gameObject);
    }

    void GetOwnership()
    {
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        if (player == Networking.LocalPlayer) IGotOwnership();
    }

    void PrepareToSync()
    {
        if (IsOwner())
        {
            IGotOwnership();
        }
        else
        {
            GetOwnership();
        }
    }

    void IGotOwnership()
    {
        RequestSerialization();
    }

    public override void OnPreSerialization()
    {
        syncedShader = currentShader;
    }

    public override void OnDeserialization()
    {
        if (syncedShader == currentShader) return;

        currentShader = syncedShader;

        SetNewShader();
    }


}
