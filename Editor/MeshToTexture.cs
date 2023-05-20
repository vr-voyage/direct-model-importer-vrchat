using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;

public class MeshToTexture : EditorWindow
{
    public Mesh mesh;
    const int currentVersion = 2;
    const int metadataVersion = 4;
    const int metadataVertices = 8;
    const int metadataNormals = 9;
    const int metadataUvs = 10;
    const int metadataIndices = 11;
    const int metadataSize = 64;
    const int float_size = 4;
    const int int_size = 4;
    const int nFloatsInColor = 4;

    const float VOY = 0x00564f59;
    const float AGE = 0x00454741;

    SerializedObject serialO;
    SerializedProperty meshSerialized;
    SerializedProperty pathSerialized;
    public UnityEngine.Object saveDir;
    private string assetsDir;
    public string saveFilePath;

    /* Note : You cannot move this before variables declaration */
    [MenuItem("Voyage / Mesh To Texture")]

    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(MeshToTexture), true);
    }

    private void OnEnable()
    {
        assetsDir = Application.dataPath;
        serialO = new SerializedObject(this);
        meshSerialized = serialO.FindProperty("mesh");
        pathSerialized = serialO.FindProperty("saveFilePath");

        saveDir = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets");
    }


    /*string TSVRecord(int index, int data)
    {
        return $"{index}\t{data}";
    }

    string TSVRecord(int index, float data)
    {
        return $"{index}\t{data}";
    }*/

    /*string ArrayToTSV(int[] array)
    {
        List<string> records = new List<string>(array.Length);
        foreach (int value in array)
        {
            records.Add(value.ToString());
        }
        return String.Join("\n", records);
    }

    string ArrayToTSV(float[] array)
    {
        List<string> records = new List<string>(array.Length);
        foreach (int value in array)
        {
            records.Add(value.ToString());
        }
        return String.Join("\n", records);
    }*/

    void DumpMetadata(float[] metadata)
    {
        Debug.Log($"{(int)metadata[0]:X} {(int)metadata[1]:X}");
        Debug.Log($"vertices : {metadata[metadataVertices]}");
        Debug.Log($"normals  : {metadata[metadataNormals]}");
        Debug.Log($"uvs      : {metadata[metadataUvs]}");
        Debug.Log($"indices  : {metadata[metadataIndices]}");
    }

    private void ZeroFill(float[] array)
    {
        int arraySize = array.Length;
        for (int i = 0; i < arraySize; i++)
        {
            array[i] = 0;
        }
    }



    Texture2D CreateEXRCompatibleTexture(int textureWidth, int textureHeight)
    {
        return new Texture2D(textureWidth, textureHeight, TextureFormat.RGBAFloat, false);
    }

    Texture2D EncodeMeshAsTexture(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        
        Vector3[] normals = mesh.normals;
        Vector2[] uvs = mesh.uv;
        int[] indices = mesh.triangles;

        Debug.Log("Starting with");
        Debug.Log($"Vertices : {vertices.Length}");
        Debug.Log($"Normals : {normals.Length}");
        Debug.Log($"Uvs : {uvs.Length}");
        Debug.Log($"indices : {indices.Length}");

        float[] float_indices = new float[indices.Length];
        for (int i = 0; i < indices.Length; i++)
        {
            
            float_indices[i] = indices[i];
            Debug.Log($"Before [{i}] = {(int)float_indices[i]}");
        }


        int n_pixels =
            vertices.Length * 3
            + uvs.Length * 2
            + normals.Length * 3
            + indices.Length;


        int pow2_size = 1;
        int pow2 = 1;
        while (pow2_size < n_pixels)
        {
            pow2++;
            pow2_size <<= 1;
        }

        int pow2_part = 1 << (pow2 / 2);
        Debug.Log($"{n_pixels} < {pow2_size} - {pow2_part}");

        int textureWidth = pow2_part;
        int textureHeight = pow2_part;

        Texture2D texture = CreateEXRCompatibleTexture(textureWidth, textureHeight);
        float[] meshData = new float[vertices.Length * 4 + normals.Length * 4 + uvs.Length * 4];
        int m = 0;
        for (int v = 0; v < vertices.Length; v++)
        {
            Vector3 vertex = vertices[v];
            meshData[m + 0] = vertex.x;
            meshData[m + 1] = vertex.y;
            meshData[m + 2] = vertex.z;
            m += 4;
        }
        Debug.Log($"cursor after vertices : {m}");
        for (int n = 0; n < normals.Length; n++)
        {
            Vector3 normal = normals[n];
            meshData[m + 0] = normal.x;
            meshData[m + 1] = normal.y;
            meshData[m + 2] = normal.z;
            m += 4;
        }
        Debug.Log($"cursor after normals : {m}");
        for (int u = 0; u < uvs.Length; u++)
        {
            Vector3 uv = uvs[u];
            meshData[m + 0] = uv.x;
            meshData[m + 1] = uv.y;
            m += 4;
        }
        Debug.Log($"cursor after uvs : {m}");
        float[] metadata = new float[metadataSize];
        ZeroFill(metadata);

        metadata[0] = VOY; // VOY
        metadata[1] = AGE; // AGE
        metadata[2] = float.PositiveInfinity;
        metadata[3] = float.NaN;
        metadata[metadataVersion] = 0; // Version

        metadata[metadataVertices] = vertices.Length;
        metadata[metadataNormals]  = normals.Length;
        metadata[metadataUvs]      = uvs.Length;
        metadata[metadataIndices]  = indices.Length;

        DumpMetadata(metadata);
        float[] pixels = new float[textureWidth * textureHeight * 4 /* floats per color */];

        Debug.Log($"meshData[0] = {meshData[0]}");
        Debug.Log($"meshData[0] = {((int)meshData[0]):X}");

        Buffer.BlockCopy(
            metadata, 0,
            pixels, 0,
            metadata.Length * float_size);
        Buffer.BlockCopy(
            meshData, 0,
            pixels, metadata.Length * float_size,
            meshData.Length * float_size);
        Buffer.BlockCopy(
            float_indices, 0, pixels,
            metadata.Length * float_size + meshData.Length * float_size,
            float_indices.Length * float_size);
        //File.WriteAllText($"Assets/zOutPixels.tsv", ArrayToTSV(pixels));

        texture.SetPixelData(pixels, 0, 0);

        //Debug.Log($"Pixels[32..35] = {BitConverter.ToSingle(pixels, (metadata.Length * int_size))}");

        return texture;
    }

    float[] ConvertColorsToFloat(Color[] values)
    {
        int nColors = values.Length;
        float[] floatValues = new float[nColors * 4];
        
        for (int color = 0, floatVal = 0; color < nColors; color++, floatVal += 4)
        {
            Color currentCol = values[color];
            floatValues[floatVal + 0] = currentCol.r;
            floatValues[floatVal + 1] = currentCol.g;
            floatValues[floatVal + 2] = currentCol.b;
            floatValues[floatVal + 3] = currentCol.a;
        }
        return floatValues;
    }


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

        //File.WriteAllText($"Assets/zOutColors.tsv", ArrayToTSV(ConvertColorsToFloat(colors)));

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
        int nNormals  = (int)infoCol.g;
        int nUVS      = (int)infoCol.b;
        int nIndices  = (int)infoCol.a;

        Vector3[] vertices = new Vector3[nVertices];
        Vector3[] normals  = new Vector3[nNormals];
        Vector2[] uvs      = new Vector2[nUVS];
        int[] indices      = new int[nIndices];

        Debug.Log($"vertices : {nVertices}");
        Debug.Log($"normals : {nNormals}");
        Debug.Log($"uvs : {nUVS}");
        Debug.Log($"indices : {nIndices}");

        int startIndex = metadataSize / nFloatsInColor;
        int cursor = startIndex;
        for (int v = 0; v < nVertices; v++, cursor++)
        {
            Color currentCol = colors[cursor];
            Vector3 vertex = new Vector3(
                currentCol.r,
                currentCol.g,
                currentCol.b);
            vertices[v] = vertex;
        }
        Debug.Log($"cursor after vertices : {cursor - startIndex} ({(cursor - startIndex) * 4})");
        for (int n = 0; n < nNormals; n++, cursor++)
        {
            Color currentCol = colors[cursor];
            Vector3 normal = new Vector3(
                currentCol.r,
                currentCol.g,
                currentCol.b);
            normals[n] = normal;
        }
        Debug.Log($"cursor after normals : {cursor - startIndex} ({(cursor - startIndex) * 4})");
        for (int u = 0; u < nUVS; u++)
        {
            Color currentCol = colors[cursor];
            Vector2 uv = new Vector2(
                currentCol.r,
                currentCol.g);
            uvs[u] = uv;
            cursor++;
        }
        Debug.Log($"cursor after uvs : {cursor - startIndex} ({(cursor - startIndex) * 4})");

        int alignedIndices = nIndices / 4 * 4;
        int currentIndex = 0;
        for (; currentIndex < alignedIndices; currentIndex += 4, cursor++)
        {
            
            Color currentCol = colors[cursor];
            Debug.Log($"Index : {currentIndex} ({cursor*4}) - Indices : {(int)currentCol.r},{(int)currentCol.g},{(int)currentCol.b},{(int)currentCol.a}");
            indices[currentIndex+0] = (int)currentCol.r;
            indices[currentIndex+1] = (int)currentCol.g;
            indices[currentIndex+2] = (int)currentCol.b;
            indices[currentIndex+3] = (int)currentCol.a;
        }
        Debug.Log($"cursor after indices : {cursor - startIndex} ({(cursor - startIndex) * 4})");
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

    private void OnGUI()
    {
        serialO.Update();

        EditorGUILayout.PropertyField(meshSerialized, true);
        EditorGUILayout.PropertyField(pathSerialized);
        serialO.ApplyModifiedProperties();

        if ((mesh == null) | (saveFilePath == null)) return;

        if (GUILayout.Button("Generate texture"))
        {
            Texture2D texture = EncodeMeshAsTexture(mesh);

            if (texture == null) return;

            byte[] exrData = texture.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat | Texture2D.EXRFlags.CompressZIP);
            File.WriteAllBytes($"Assets/{saveFilePath}.exr", exrData);

            AssetDatabase.Refresh();
        }

        /*Mesh resultMesh = new Mesh();
        MeshFromColors(texture.GetPixels(), resultMesh);
        AssetDatabase.CreateAsset(resultMesh, "Assets/zResultMesh.mesh");*/
    }
}
