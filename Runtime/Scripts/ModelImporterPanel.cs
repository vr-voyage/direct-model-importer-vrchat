
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
        public TMPro.TextMeshProUGUI statusText;

        public VoyageVRSNS.ModelsImporter modelImporter;
        public VRCUrlInputField modelUrlInput;

        public void ResetDisplay()
        {
            DisplayText(downloadedUrlText, "");
            DisplayText(correctText, "");
            DisplayText(verticesText, "");
            DisplayText(normalsText, "");
            DisplayText(uvsText, "");
            DisplayText(indicesText, "");
            DisplayText(debugText, "");
            DisplayText(statusText, "");
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

        public void ShowStatus(string statusMessage)
        {
            DisplayText(statusText, statusMessage);
        }

        public void ModelDownloaded()
        {
            Debug.Log("Model Downloaded !");
            if (modelUrlInput == null) return;
            modelUrlInput.SetUrl(VRCUrl.Empty);
        }

        public void DownloadButtonPushed()
        {
            if ((modelImporter == null) | (modelUrlInput == null)) return;
            if (modelImporter.isDownloading) return;

            VRCUrl modelUrl = modelUrlInput.GetUrl();

            Debug.Log($"Trying to download : {modelUrl}");

            modelImporter.DownloadModel(modelUrl);
        }
    }
}

