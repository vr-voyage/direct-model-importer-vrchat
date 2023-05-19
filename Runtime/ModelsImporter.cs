
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.StringLoading;
using VRC.SDK3.Data;
using VRC.Udon.Common.Interfaces;
using VRC.SDK3.Image;

enum EncodingMethod
{
    INVALID,
    IMAGE
};

public class ModelsImporter : UdonSharpBehaviour
{
    public VRCUrl modelUrl;
    public VRCUrl textureUrl;

    public Material textureMaterial;

    const float supportedVersionMax = 1;
    const string expectedDataType = "VoyageModelEncoder";

    public MeshFilter outMeshFilter;

    IUdonEventReceiver receiver;

    public ModelImporterInfoPanel debugPanel;

    VRCImageDownloader downloader;
    TextureInfo textureInfo;
    void OnEnable()
    {
        receiver = (IUdonEventReceiver)this;
        textureInfo = new TextureInfo();
        textureInfo.WrapModeU = TextureWrapMode.Clamp;
        textureInfo.WrapModeV = TextureWrapMode.Clamp;
        textureInfo.FilterMode = FilterMode.Point;
        NextPhase();
        //VRCStringDownloader.LoadUrl(modelUrl, receiver);
        //VRCStringDownloader
    }

    void DebugLog(string message)
    {
        //if (debugOutput == null) return;

        //debugOutput.text += $"[{System.DateTime.Now.ToString()}] {message}\n";
        Debug.Log(message);
    }

    int downloadStage = 0;

    void NextPhase()
    {
        VRCUrl downloadUrl;
        Material usedMaterial = null;
        switch(downloadStage)
        {
            case 0:
                downloadUrl = modelUrl;
            break;
            case 1:
                downloadUrl = textureUrl;
                usedMaterial = textureMaterial;
            break;
            default:
                return;
        }

        if (downloadUrl == null || downloadUrl.ToString() == "") return;
        downloader = new VRCImageDownloader();
        downloader.DownloadImage(downloadUrl, usedMaterial, receiver, textureInfo);
    }

    void PhaseSucceeded(Texture2D result)
    {
        bool success = false;
        switch(downloadStage)
        {
            case 0:
                Mesh mesh = new Mesh();
                success = MeshFromColors(result.GetPixels(), mesh);
                if (success)
                {
                    outMeshFilter.sharedMesh = mesh;
                }
                downloader.Dispose();
            break;
            case 1:
                success = true;
                return;
        }
        if (success)
        {
            downloadStage++;
            NextPhase();
        }
    }

    public override void OnImageLoadSuccess(IVRCImageDownload result)
    {
        if (result.State == VRCImageDownloadState.Complete)
        {  
            PhaseSucceeded(result.Result);
        }
    }

    public override void OnImageLoadError(IVRCImageDownload result)
    {
        if (result.State == VRCImageDownloadState.Error)
        {
            DebugLog(result.Error.ToString());
        }
    }


    bool DictionaryContainsKeys(DataDictionary dictionary, params string[] keys)
    {
        bool keysPresent = true;
        foreach (string key in keys)
        {
            keysPresent &= (dictionary.ContainsKey(key));
        }
        return keysPresent;
    }

    public override void OnStringLoadSuccess(IVRCStringDownload result)
    {
        string resultString = result.Result;
        DataToken resultToken;
        if (VRCJson.TryDeserializeFromJson(resultString, out resultToken) == false)
        {
            DebugLog(resultToken.ToString());
            return;
        }

        if (resultToken.TokenType != TokenType.DataDictionary)
        {
            DebugLog($"Invalid token type. Expected DataDictionary, got {resultToken.TokenType}");
            return;
        }

        DataDictionary resultDictionary = (DataDictionary)resultToken;
        if (DictionaryContainsKeys(resultDictionary, "version", "type", "values"))
        {
            DebugLog("Some keys (version, type) are missing in the provided JSON !");
            return;
        }

        var dataVersion = resultDictionary["version"];
        var dataType = resultDictionary["type"];
        var dataValues = resultDictionary["values"];

        if (dataVersion.TokenType != TokenType.Double)
        {
            DebugLog($"Invalid version number. Expected a Double, got a {dataVersion.TokenType}.");
            return;
        }

        if (dataType.TokenType != TokenType.String)
        {
            DebugLog($"Invalid 'type' field. Expected a Double, got a {dataType.TokenType}.");
            return;
        }

        if (dataValues.TokenType != TokenType.DataList)
        {
            DebugLog($"Invalid 'values' field. Expected a DataList, got a {dataValues.TokenType}.");
            return;
        }

        if ((string)dataType != expectedDataType)
        {
            DebugLog($"Invalid datatype. Expected {expectedDataType}");
            return;
        }

        if ((double)dataVersion > supportedVersionMax)
        {
            DebugLog($"Unsupported version {dataVersion}. Only version {supportedVersionMax} and lower are supported.");
            return;
        }

        
    }



    const float VOY = 0x00564f59;
    const float AGE = 0x00454741;

    public const int metadataSize = 64;
    public const int minimumDataSize =
        metadataSize
        + 3  // vertices
        + 3  // normals
        + 2  // uvs
        + 3; // indices
    


    bool MeshFromColors(Color[] colors, Mesh mesh)
    {
        

        if (colors == null)
        {
            Debug.Log("No color data...");
            return false;
        }

        if (colors.Length <= minimumDataSize)
        {
            Debug.Log($"Not enough data ({colors.Length} bytes)");
            return false;
        }

        /* Structure is :
         * [0..15] : Metadata
         * [...] :
         *   metadata[8]  * vertices * 3
         *   metadata[9]  * normals  * 3
         *   metadata[10] * uvs      * 2
         *   metadata[11] * indices
         */

        //DumpMetadata(colors);

        if ((colors[0].r != VOY) | (colors[0].g != AGE))
        {
            Debug.Log($"{colors[0].r} != {VOY} ? {colors[0].r != VOY}");
            Debug.Log($"{colors[0].g} != {AGE} ? {colors[0].g != AGE}");
            Debug.LogError("No voyage here !");
            //Debug.LogError($"{(int)colors[0].r:X} - {(int)colors[1].r:X}");
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

        if (debugPanel != null)
        {
            debugPanel.ShowValues(true, nVertices, nNormals, nUVS, nIndices, "Meow");
        }

        /*Debug.Log($"vertices : {nVertices}");
        Debug.Log($"normals : {nNormals}");
        Debug.Log($"uvs : {nUVS}");
        Debug.Log($"indices : {nIndices}");*/

        int start = metadataSize / 4;
        int cursor = start;
        for (int v = 0; v < nVertices; v++, cursor++)
        {
            Color currentCol = colors[cursor];
            Vector3 vertex = new Vector3(
                currentCol.r,
                currentCol.g,
                currentCol.b);
            //Debug.Log($"Vertex {v} : {vertex.x},{vertex.y},{vertex.z}");
            vertices[v] = vertex;
        }
        /*Debug.Log($"cursor after vertices : {cursor - start} ({(cursor - start) * 4})");*/
        for (int n = 0; n < nNormals; n++, cursor++)
        {
            Color currentCol = colors[cursor];
            Vector3 normal = new Vector3(
                currentCol.r,
                currentCol.g,
                currentCol.b);
            //Debug.Log($"Normal {n} : {normal.x},{normal.y},{normal.z}");
            normals[n] = normal;
        }
        /*Debug.Log($"cursor after normals : {cursor - start} ({(cursor - start) * 4})");*/
        for (int u = 0; u < nUVS; u++)
        {
            Color currentCol = colors[cursor];
            Vector2 uv = new Vector2(
                currentCol.r,
                currentCol.g);
            uvs[u] = uv;
            cursor++;
        }
        /*Debug.Log($"cursor after uvs : {cursor - start} ({(cursor - start) * 4})");*/

        int alignedIndices = nIndices / 4 * 4;
        int currentIndex = 0;
        for (; currentIndex < alignedIndices; currentIndex += 4, cursor++)
        {
            
            Color currentCol = colors[cursor];
            /*Debug.Log($"Index : {currentIndex} ({cursor*4}) - Indices : {(int)currentCol.r},{(int)currentCol.g},{(int)currentCol.b},{(int)currentCol.a}");*/
            indices[currentIndex+0] = (int)currentCol.r;
            indices[currentIndex+1] = (int)currentCol.g;
            indices[currentIndex+2] = (int)currentCol.b;
            indices[currentIndex+3] = (int)currentCol.a;
        }
        /*Debug.Log($"cursor after indices : {cursor - start} ({(cursor - start) * 4})");*/
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

    public override void OnStringLoadError(IVRCStringDownload result)
    {
        DebugLog(result.ToString());
    }
}
