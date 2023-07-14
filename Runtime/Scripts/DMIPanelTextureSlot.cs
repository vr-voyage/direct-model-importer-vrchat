
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

public class DMIPanelTextureSlot : UdonSharpBehaviour
{
    DMITextureDownloader downloader;

    public TMPro.TextMeshProUGUI propertyText;
    public TMPro.TextMeshProUGUI statusText;

    public VRCUrlInputField textureUrlInput;

    void SetText(TMPro.TextMeshProUGUI uiText, string text)
    {
        if ((uiText == null) | (text == null)) return;

        uiText.text = text;
    }

    public void SetupFor(DMITextureDownloader currentDownloader)
    {
        if (currentDownloader == null)
        {
            DisableSlot();
            return;
        }

        Debug.Log($"[{name}] SetupFor {currentDownloader.property}");
        downloader = currentDownloader;
        currentDownloader.SetPanel(this);
        SetText(propertyText, downloader.property);
        SetText(statusText, "");
        SetCurrentUrl(currentDownloader.url);
        gameObject.SetActive(true);
    }

    public void DisableSlot()
    {
        if (downloader != null)
        {
            downloader.ResetPanel();
        }
        downloader = null;
    }

    public void ShowStatus(string statusMessage) => SetText(statusText, statusMessage);
    public void SetCurrentUrl(VRCUrl url)
    {
        textureUrlInput.SetUrl(url);
    }

    public override void Interact()
    {
        if ((downloader == null) | (textureUrlInput == null)) return;

        VRCUrl textureUrl = textureUrlInput.GetUrl();
        if (textureUrl == null) return;

        downloader.DownloadTexture(textureUrl);
    }
}
