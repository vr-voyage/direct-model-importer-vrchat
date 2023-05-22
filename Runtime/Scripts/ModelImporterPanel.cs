
using JetBrains.Annotations;
using TMPro;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

namespace VoyageVRSNS
{
    public class ModelImporterPanel : UdonSharpBehaviour
    {
        public TMPro.TextMeshProUGUI downloadedUrlText;
        public TMPro.TextMeshProUGUI correctText;
        public TMPro.TextMeshProUGUI verticesText;
        public TMPro.TextMeshProUGUI normalsText;
        public TMPro.TextMeshProUGUI uvsText;
        public TMPro.TextMeshProUGUI indicesText;
        public TMPro.TextMeshProUGUI debugText;

        public VoyageVRSNS.ModelsImporter modelImporter;
        public VRCUrlInputField modelUrlInput;
        public VRCUrlInputField textureUrlInput;

        [HideInInspector]
        public VRCUrl emptyUrl;
        
        public void ResetDisplay()
        {
            DisplayText(downloadedUrlText, "");
            DisplayText(correctText, "");
            DisplayText(verticesText, "");
            DisplayText(normalsText, "");
            DisplayText(uvsText, "");
            DisplayText(indicesText, "");
            DisplayText(debugText, "");
        }

        void DisplayText(TMPro.TextMeshProUGUI uiText, string content)
        {
            if ((uiText == null) | (content == null)) return;

            uiText.text = content;
        }

        public void ShowDownloadedModel(VRCUrl downloadedModelUrl)
        {
            DisplayText(downloadedUrlText, downloadedModelUrl.ToString());
        }

        public void ShowValues(
            bool isCorrect,
            int nVertices,
            int nNormals,
            int nUvs,
            int nIndices,
            string additionalContent)
        {
            DisplayText(correctText,  isCorrect.ToString());
            DisplayText(verticesText, nVertices.ToString());
            DisplayText(normalsText,  nNormals.ToString());
            DisplayText(uvsText,      nUvs.ToString());
            DisplayText(indicesText,  nIndices.ToString());
            DisplayText(debugText,    additionalContent);
        }

        public void ModelDownloaded()
        {
            if (modelUrlInput == null) return;
            modelUrlInput.SetUrl(emptyUrl);
        }

        public void DownloadButtonPushed()
        {
            if ((modelImporter == null) | (modelUrlInput == null)) return;
            if (modelImporter.isDownloading) return;

            VRCUrl modelUrl = modelUrlInput.GetUrl();
            VRCUrl textureUrl = null;
            if (textureUrlInput != null)
            {
                textureUrl = textureUrlInput.GetUrl();
            }

            Debug.Log($"Trying to download : {modelUrl}");

            modelImporter.DownloadModel(modelUrl, new VRCUrl[] { textureUrl });
        }
    }
}

