
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DMIDatabase : UdonSharpBehaviour
{
    public Material[] materials;

    public Shader GetShader(int shaderID)
    {
        Material shaderRepresentation = GetMaterial(shaderID);
        if (shaderRepresentation == null) return null;
        
        return shaderRepresentation.shader;
    }

    Material GetMaterial(int shaderID)
    {
        if ((shaderID < 0) | (shaderID > materials.Length)) return null;
        return materials[shaderID];
    }

    public int GetShaderID(Shader shader)
    {
        int nMaterials = materials.Length;
        for (int shaderID = 0; shaderID < nMaterials; shaderID++)
        {
            if (materials[shaderID].shader == shader) return shaderID;
        }
        return -1;
    }
}
