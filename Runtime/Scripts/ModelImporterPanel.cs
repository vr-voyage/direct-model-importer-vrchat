
using JetBrains.Annotations;
using TMPro;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

namespace VoyageVRSNS
{
    public class ModelImporterPanel : UdonSharpBehaviour
    {
        public InputField downloadedUrlText;
        public TMPro.TextMeshProUGUI correctText;
        public TMPro.TextMeshProUGUI verticesText;
        public TMPro.TextMeshProUGUI indicesText;
        public TMPro.TextMeshProUGUI statusText;

        public VoyageVRSNS.ModelsImporter modelImporter;
        public VRCUrlInputField modelUrlInput;

        public DMIPanelMaterial materialPanel;
        public Button[] materialsButtons;
        
        void ResetMaterialPanel()
        {
            materialPanel.CleanPanel();
        }

        void ResetButtons()
        {
            int nButtons = materialsButtons.Length;
            for (int button = 0; button < nButtons; button++)
            {
                var materialButton = materialsButtons[button];
                if (materialButton == null) continue;
                materialButton.gameObject.SetActive(false);
            }
        }

        public void RefreshMaterials()
        {
            ResetMaterialPanel();
            ResetButtons();
            int nMaterials = modelImporter.nMaterialsSet;
            int nButtons = materialsButtons.Length;
            int minUseableButtons = Mathf.Min(nButtons, nMaterials);
            
            for (int button = 0; button < minUseableButtons; button++)
            {
                var materialButton = materialsButtons[button];
                if (materialButton == null) continue;
                materialButton.gameObject.SetActive(true);
            }
            ShowMaterial(0);
        }

        public void ShowMaterial(int index)
        {
            ResetMaterialPanel();
            Debug.Log($"[ModelImporterPanel] (ShowMaterial) index : {index} - materialsSet : {modelImporter.nMaterialsSet}");
            if (index >= modelImporter.nMaterialsSet) return;
            materialPanel.SetupFor(modelImporter.materialSlots[index]);

        }

        public void ResetDisplay()
        {
            if (downloadedUrlText != null) downloadedUrlText.text = "";
            DisplayText(correctText,  "");
            DisplayText(verticesText, "");
            DisplayText(indicesText,  "");
            DisplayText(statusText,   "");
            ResetMaterialPanel();
        }

        void DisplayText(TMPro.TextMeshProUGUI uiText, string content)
        {
            if ((uiText == null) | (content == null)) return;

            uiText.text = content;
        }

        public void ShowDownloadedModel(VRCUrl downloadedModelUrl)
        {
            if ((downloadedUrlText == null) | (downloadedModelUrl == null)) return;
            downloadedUrlText.text = downloadedModelUrl.ToString();
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
            DisplayText(indicesText,  nIndices.ToString());
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

