
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.StringLoading;
using VRC.SDK3.Data;
using VRC.Udon.Common.Interfaces;
using VRC.SDK3.Image;

namespace VoyageVRSNS
{
    enum EncodingMethod
    {
        INVALID,
        IMAGE
    };

    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class ModelsImporter : UdonSharpBehaviour
    {
        
        VRCUrl modelUrl;
        
        [HideInInspector]
        [UdonSynced]
        public VRCUrl syncedModelUrl;

        const float supportedVersionMax = 2;

        public MeshFilter outMeshFilter;

        IUdonEventReceiver receiver;

        public ModelImporterPanel panel;

        VRCImageDownloader downloader;
        TextureInfo textureInfo;

        [HideInInspector]
        public bool isDownloading = false;

        const float VOY = 0x00564f59;
        const float AGE = 0x00454741;

        public const int metadataSizeInFloat = 64;
        public const int minimumDataSize =
            metadataSizeInFloat
            + 3  // vertices
            + 3  // normals
            + 2  // uvs
            + 3; // indices

        public TexturesDownloader texturesDownloader;
        void OnEnable()
        {
            if (outMeshFilter == null)
            {
                Debug.Log("[VoyageVRSNS.ModelsImporter] Mesh Renderer not set !");
                gameObject.SetActive(false);
                return;
            }

            receiver = (IUdonEventReceiver)this;
            textureInfo = new TextureInfo();
            textureInfo.WrapModeU = TextureWrapMode.Clamp;
            textureInfo.WrapModeV = TextureWrapMode.Clamp;
            textureInfo.FilterMode = FilterMode.Point;
            if (texturesDownloader) texturesDownloader.ResetAndHide();

        }

        void DebugLog(string message)
        {
            Debug.Log(message);
        }

        void ShowError(string errorMessage)
        {
            Debug.LogError(errorMessage);
            if (panel != null) panel.ShowStatus(errorMessage);
        }



        void ResetDisplay()
        {

            outMeshFilter.sharedMesh = null;
            outMeshFilter.transform.localPosition = Vector3.zero;
            outMeshFilter.transform.localRotation = Quaternion.identity;

            if (panel != null)
            {
                panel.ResetDisplay();
            }

            if (texturesDownloader) texturesDownloader.ResetAndHide();

        }

        

        public void DownloadModel(VRCUrl providedModelUrl)
        {
            if (providedModelUrl == null) return;

            modelUrl = providedModelUrl;
            
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
            syncedModelUrl = modelUrl;
            RequestSerialization();
        }

        public override void OnDeserialization()
        {
            if ((syncedModelUrl == null) | (syncedModelUrl == modelUrl)) return;

            modelUrl = syncedModelUrl;
            Download();
        }

        void CancelDownload()
        {
            if (downloader != null)
            {
                downloader.Dispose();
            }
            downloader = null;
            isDownloading = false;
        }

        /* FIXME : Change this name */
        void Download()
        {
            ResetDisplay();
            if (modelUrl == null || modelUrl.ToString() == "") return;

            CancelDownload();
            isDownloading = true;
            
            if (panel != null)
            {
                panel.ShowDownloadedModel(modelUrl);
            }

            downloader = new VRCImageDownloader();
            downloader.DownloadImage(
                modelUrl,
                null,
                receiver,
                textureInfo);
        }

        public override void OnImageLoadSuccess(IVRCImageDownload result)
        {
            if (result.State == VRCImageDownloadState.Complete)
            {
                Mesh mesh = new Mesh();
                if (MeshFromColors(result.Result.GetPixels(), mesh))
                {
                    outMeshFilter.sharedMesh = mesh;
                    if (texturesDownloader != null) texturesDownloader.Show();
                }


                Debug.Log($"Panel is null ? {panel == null}");
                if (panel != null) { panel.ModelDownloaded(); }
                
            }
            /* Just in case VRChat snaps... */
            if (downloader != null) downloader.Dispose();
            isDownloading = false;
        }

        public override void OnImageLoadError(IVRCImageDownload result)
        {
            
            if (result.State == VRCImageDownloadState.Error)
            {
                ShowError(result.Error.ToString());
            }
            if (downloader != null) downloader.Dispose();
            isDownloading = false;
        }

        bool MeshFromColors(Color[] colors, Mesh mesh)
        {
            if (colors == null)
            {
                DebugLog("No color data...");
                return false;
            }

            if (colors.Length <= minimumDataSize)
            {
                DebugLog($"Not enough data ({colors.Length} bytes)");
                return false;
            }

            if ((colors[0].r != VOY) | (colors[0].g != AGE))
            {
                //Debug.Log($"{colors[0].r} != {VOY} ? {colors[0].r != VOY}");
                //Debug.Log($"{colors[0].g} != {AGE} ? {colors[0].g != AGE}");
                ShowError("Not an encoded model. Invalid metadata");
                return false;
            }

            var currentVersion = colors[1].r;
            if (currentVersion > supportedVersionMax)
            {
                Debug.Log("Unsupported format");
                return false;
            }
            var infoCol = colors[2];
            int nVertices = (int)infoCol.r;
            int nNormals = (int)infoCol.g;
            int nUVS = (int)infoCol.b;
            int nIndices = (int)infoCol.a;

            Vector3[] vertices = new Vector3[nVertices];
            Vector3[] normals = new Vector3[nNormals];
            Vector2[] uvs = new Vector2[nUVS];
            int[] indices = new int[nIndices];

            if (panel != null)
            {
                panel.ShowValues(true, nVertices, nNormals, nUVS, nIndices, "");
            }

            int start = metadataSizeInFloat / 4;
            int cursor = start;
            for (int v = 0; v < nVertices; v++, cursor++)
            {
                Color currentCol = colors[cursor];
                Vector3 vertex = new Vector3(
                    currentCol.r,
                    currentCol.g,
                    currentCol.b);
                vertices[v] = vertex;
            }

            for (int n = 0; n < nNormals; n++, cursor++)
            {
                Color currentCol = colors[cursor];
                Vector3 normal = new Vector3(
                    currentCol.r,
                    currentCol.g,
                    currentCol.b);
                normals[n] = normal;
            }

            for (int u = 0; u < nUVS; u++)
            {
                Color currentCol = colors[cursor];
                Vector2 uv = new Vector2(
                    currentCol.r,
                    currentCol.g);
                uvs[u] = uv;
                cursor++;
            }

            int alignedIndices = nIndices / 4 * 4;
            int currentIndex = 0;
            for (; currentIndex < alignedIndices; currentIndex += 4, cursor++)
            {

                Color currentCol = colors[cursor];
                indices[currentIndex + 0] = (int)currentCol.r;
                indices[currentIndex + 1] = (int)currentCol.g;
                indices[currentIndex + 2] = (int)currentCol.b;
                indices[currentIndex + 3] = (int)currentCol.a;
            }

            int remainingIndices = nIndices - alignedIndices;
            if (remainingIndices > 0)
            {
                Color currentCol = colors[cursor];
                if (remainingIndices >= 1)
                {
                    indices[currentIndex] = (int)currentCol.r;
                    currentIndex++;
                }
                if (remainingIndices >= 2)
                {
                    indices[currentIndex] = (int)currentCol.g;
                    currentIndex++;
                }
                if (remainingIndices == 3)
                {
                    indices[currentIndex] = (int)currentCol.b;
                    currentIndex++;
                }
            }


            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = indices;

            return true;
        }


    }

}

