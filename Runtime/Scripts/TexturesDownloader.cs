
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Image;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace VoyageVRSNS
{
    /* FIXME : I hate duplicating the code of the download phase
     * to only manage the downloading of one texture.
     * However, in a near future, we'll get multiple materials
     * with multiple textures.
     * So this class might become way more complex.
     * We'll see though. If the code stays simple, I'll remerge
     * it with the ModelsImporter.
     */
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class TexturesDownloader : UdonSharpBehaviour
    {
        
        [HideInInspector]
        [UdonSynced]
        public VRCUrl syncedTextureUrl;
        VRCUrl textureUrl;

        public MeshRenderer targetRenderer;
        Material textureMaterial;

        IUdonEventReceiver receiver;

        TextureInfo textureInfo;

        public TexturesInfoPanel panel;

        VRCImageDownloader downloader;

        void OnEnable()
        {
            if (targetRenderer == null)
            {
                Debug.LogError("[Voyage Texture Downloader] No target renderer defined");
                gameObject.SetActive(false);
                return;
            }
            receiver = (IUdonEventReceiver)this;
            textureInfo = new TextureInfo();
            textureInfo.WrapModeU = TextureWrapMode.Repeat;
            textureInfo.WrapModeV = TextureWrapMode.Repeat;
            textureInfo.FilterMode = FilterMode.Bilinear;
        }

        void CancelDownload()
        {
            Debug.Log("<color=orange>[TexturesDownloader] Canceling download</color>");
            if (downloader != null)
            {
                downloader.Dispose();
            }
            downloader = null;
        }

        void ResetEverything()
        {
            CancelDownload();
            if (textureMaterial != null) textureMaterial.SetTexture("_MainTex", null);
            textureMaterial = null;
        }

        public void ResetAndHide()
        {
            Debug.Log("<color=orange>Called Reset and Hide !</color>");
            ResetEverything();
            if (panel != null) panel.Hide();
        }


        public void Show()
        {
            if (panel != null) panel.Show();
        }
        // TODO : See if this can be factorized into a common class

        public void DownloadTexture(VRCUrl newTextureUrl)
        {
            textureUrl = newTextureUrl;
            Sync();
            Download();
        }

        void Sync()
        {
            if (Networking.GetOwner(gameObject) != Networking.LocalPlayer)
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
            else
            {
                YouGotOwnership();
            }
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            if (player == Networking.LocalPlayer)
            {
                YouGotOwnership();
            }
        }

        void YouGotOwnership()
        {
            syncedTextureUrl = textureUrl;
            RequestSerialization();
        }

        public override void OnDeserialization()
        {
            Debug.Log($"<color=orange>[TexturesDownloader] OnDeserialization - {syncedTextureUrl} <-> {textureUrl}</color>");
            if ((syncedTextureUrl == null) | (syncedTextureUrl == textureUrl)) return;

            textureUrl = syncedTextureUrl;
            Download();
        }

        void Download()
        {
            Debug.Log($"<color=orange>[TexturesDownloader] Download {textureUrl}</color>");
            if (textureUrl == null) return;
            ResetEverything();

            if (targetRenderer.sharedMaterial == null)
            {
                ShowError("No material to apply a texture on (???)");
                return;
            }
            textureMaterial = targetRenderer.sharedMaterial;

            if (panel) panel.ShowDownloadURL(textureUrl);

            Debug.Log($"<color=orange>[TexturesDownloader] Starting to download {textureUrl}</color>");
            downloader = new VRCImageDownloader();
            downloader.DownloadImage(
                url: textureUrl,
                material: textureMaterial,
                udonBehaviour: receiver,
                textureInfo: textureInfo);
        }

        public override void OnImageLoadSuccess(IVRCImageDownload result)
        {
            /* Well, the image downloader will automatically assign the
             * texture to the material, so nothing to do here...
             */
            Debug.Log("<color=green>[TexturesDownloader] Download success !</color>");
            if (panel)
            {
                panel.TextureDownloaded();
            }
        }

        void ShowError(string errorMessage)
        {
            if (panel != null) panel.ShowStatus(errorMessage);
        }

        public override void OnImageLoadError(IVRCImageDownload result)
        {
            if (result.State == VRCImageDownloadState.Error)
            {
                ShowError(result.Error.ToString());
            }
            if (downloader != null) downloader.Dispose();
            if (panel) panel.TextureDownloaded();
        }
    }
}

