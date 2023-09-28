
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.StringLoading;
using VRC.SDK3.Data;
using VRC.Udon.Common.Interfaces;
using VRC.SDK3.Image;
using UnityEngine.Rendering;
using UnityEditor;
using System;

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

        const float supportedVersionMax = 3;

        public MeshFilter outMeshFilter;
        public MeshRenderer meshRenderer;

        IUdonEventReceiver receiver;

        public ModelImporterPanel panel;

        VRCImageDownloader downloader;
        TextureInfo textureInfo;

        public Material[] preloadedMaterials;

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

        //public TexturesDownloader texturesDownloader;
        public DMIMaterialSlot[] materialSlots;
        public DMIDatabase database;
        void OnEnable()
        {
            if (outMeshFilter == null)
            {
                Debug.Log("[VoyageVRSNS.ModelsImporter] Mesh Filter not set !");
                gameObject.SetActive(false);
                return;
            }

            if (meshRenderer == null)
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
            //if (texturesDownloader) texturesDownloader.ResetAndHide();

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

            //if (texturesDownloader) texturesDownloader.ResetAndHide();

        }

        

        public void DownloadModel(VRCUrl providedModelUrl)
        {
            if (providedModelUrl == null) return;

            modelUrl = providedModelUrl;

            // Since this is the user entry point
            // We disable the texture download HERE.
            // Basically, if a user starts a new download, reset
            // the texture.
            //if (texturesDownloader) texturesDownloader.ResetAndHide();

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

        public int nMaterialsSet = 0;

        void SetupMaterialSlots(MeshRenderer usedRenderer)
        {
            var materials = usedRenderer.sharedMaterials;
            int nSlots = materialSlots.Length;
            int nMaterials = materials.Length;
            Debug.Log($"[ModelsImporter] (SetupMaterialSlots) nSlots : {nSlots} - nMaterials : {nMaterials}");
            int maxSlots = Mathf.Min(nSlots, nMaterials);
            for (int slot = 0; slot < maxSlots; slot++)
            {
                var materialSlot = materialSlots[slot];
                var material = materials[slot];

                if ((materialSlot == null) | (material == null)) continue;

                materialSlot.SetupFor(material);
            }
            nMaterialsSet = maxSlots;
            panel.RefreshMaterials();
        }

        public bool SetMaterialShader(int materialIndex, int shaderIndex)
        {
            if ((materialIndex < 0) | (materialIndex >= meshRenderer.sharedMaterials.Length))
            {
                return false;
            }

            var shader = database.GetShader(shaderIndex);
            if (shader == null) return false;
            
            var material = meshRenderer.sharedMaterials[materialIndex];
            if (material == null) return false;

            material.shader = shader;
            return true;
        }

        public override void OnImageLoadSuccess(IVRCImageDownload result)
        {
            if (result.State == VRCImageDownloadState.Complete)
            {
                Mesh mesh = new Mesh();
                if (MeshFromColors(result.Result.GetPixels(), mesh))
                {
                    outMeshFilter.sharedMesh = mesh;
                    SetupMaterialSlots(meshRenderer);
                    //if (texturesDownloader != null) texturesDownloader.Show();
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

        void InstantiateMaterials(
            Material[] materials,
            int nMaterialsToInstantiate,
            MeshRenderer temporaryRenderer)
        {
            for (int m = 0; m < nMaterialsToInstantiate; m++)
            {
                /* This actually Instantiate a new material.
                 * For some reason, you can't do Instantiate(material)
                 * with Udon
                 */
                temporaryRenderer.material = materials[m];

                materials[m] = temporaryRenderer.material;
            }
        }

        void SetupMaterials(Mesh mesh)
        {
            if (meshRenderer == null)
            {
                return;
            }

            int materialsNeeded = mesh.subMeshCount;
            int materialsAvailable = preloadedMaterials.Length;
            Material[] newMaterials = new Material[materialsNeeded];

            int minMaterialsUseable = Mathf.Min(materialsNeeded, materialsAvailable);

            InstantiateMaterials(newMaterials, minMaterialsUseable, meshRenderer);
            //for (int m = 0; m < minMaterialsUseable; m++)
            //{
            //    /* This actually Instantiate a new material.
            //     * For some reason, you can't do Instantiate(material)
            //     * with Udon
            //     */
            //    meshRenderer.material = preloadedMaterials[m];

            //    newMaterials[m] = meshRenderer.material;
            //}
            meshRenderer.materials = newMaterials;

        }

        public MeshFilter cloningFilter;
        /* Because Udon is fucking dumb ! */
        Mesh InstantiateMesh(Mesh toClone)
        {
            cloningFilter.sharedMesh = toClone;
            Mesh newMesh = cloningFilter.mesh;
            cloningFilter.sharedMesh = null;
            return newMesh;
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
            Color infoCol2 = colors[3];
            int nVertices = (int)infoCol.r;
            int nNormals = (int)infoCol.g;
            int nUVS = (int)infoCol.b;
            int nIndices = (int)infoCol.a;
            int nSubmeshes = currentVersion > 2 ? (int)infoCol2.r : 0;

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
                cursor++;
            }

            
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = indices;

            if (nSubmeshes > 1)
            {
                CombineInstance[] combines = new CombineInstance[nSubmeshes];

                for (int submesh = 0; submesh < nSubmeshes; submesh++)
                {
                    Color currentColor = colors[cursor];
                    int indexStart = (int)currentColor.r;
                    int indexCount = (int)currentColor.g;
                    int[] subMeshIndices = new int[indexCount];
                    Array.Copy(indices, indexStart, subMeshIndices, 0, indexCount);

                    Mesh newMesh = InstantiateMesh(mesh);
                    newMesh.triangles = subMeshIndices;
                    newMesh.Optimize();

                    CombineInstance newInstance = new CombineInstance();
                    newInstance.mesh = newMesh;
                    combines[submesh] = newInstance;

                    cursor++;
                }

                mesh.Clear();
                mesh.CombineMeshes(combines, mergeSubMeshes: false, useMatrices: false);
                Debug.Log(mesh.vertices.Length);

                Debug.Log(mesh.triangles.Length);
                Debug.Log(mesh.subMeshCount);
            }

            SetupMaterials(mesh);

            return true;
        }


    }

}

