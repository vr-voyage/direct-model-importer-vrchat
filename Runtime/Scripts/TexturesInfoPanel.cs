
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

namespace VoyageVRSNS
{
    public class TexturesInfoPanel : UdonSharpBehaviour
    {
        public TexturesDownloader downloader;
        public VRCUrlInputField textureUrlInput;
        public TMPro.TextMeshProUGUI urlText;
        public TMPro.TextMeshProUGUI statusText;

        void DisplayText(TMPro.TextMeshProUGUI uiText, string text)
        {
            if (uiText == null) return;
            uiText.text = (text != null ? text : "");
        }

        void ClearText(TMPro.TextMeshProUGUI uiText)
        {
            DisplayText(uiText, "");
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            ClearText(urlText);
            ClearText(statusText);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void ShowDownloadURL(VRCUrl url)
        {
            if (urlText == null) return;
            urlText.text = url.ToString();
        }

        public void TextureDownloaded()
        {
            if (textureUrlInput == null) return;
            textureUrlInput.SetUrl(VRCUrl.Empty);
            ShowStatus("Success !");
        }
        public void DownloadButtonPushed()
        {
            if ((downloader == null) | (textureUrlInput == null)) return;
            downloader.DownloadTexture(textureUrlInput.GetUrl());
        }
        public void ShowStatus(string statusMessage)
        {
            DisplayText(statusText, statusMessage);
        }
    }
}

