
using JetBrains.Annotations;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ModelImporterInfoPanel : UdonSharpBehaviour
{
    public TMPro.TextMeshProUGUI correctText;
    public TMPro.TextMeshProUGUI verticesText;
    public TMPro.TextMeshProUGUI normalsText;
    public TMPro.TextMeshProUGUI uvsText;
    public TMPro.TextMeshProUGUI indicesText;
    public TMPro.TextMeshProUGUI debugText;

    void DisplayText(TMPro.TextMeshProUGUI uiText, string content)
    {
        if ((uiText == null) | (content == null)) return;

        uiText.text = content;
    }

    public void ShowValues(
        bool isCorrect,
        int nVertices,
        int nNormals,
        int nUvs,
        int nIndices,
        string additionalContent)
    {
        DisplayText(correctText, isCorrect.ToString());
        DisplayText(verticesText, nVertices.ToString());
        DisplayText(normalsText, nNormals.ToString());
        DisplayText(uvsText, nUvs.ToString());
        DisplayText(indicesText, nIndices.ToString());
        DisplayText(debugText, additionalContent);
    }
}
