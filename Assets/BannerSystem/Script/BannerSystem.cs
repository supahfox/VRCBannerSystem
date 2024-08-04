﻿/*
VRCBannerSystem
Author: @supahfox
Description: Un sistema de banners que descarga imágenes y texto de una URL y las muestra en un plano.
*/

using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Image;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;
using VRC.SDK3.Components;
using VRC.Udon.Common.Interfaces;

namespace BannerSystem
{
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class BannerSystem : UdonSharpBehaviour
{
    [SerializeField, Tooltip("URLs de imágenes a cargar")]
    private VRCUrl[] imageUrls;
    
    [SerializeField, Tooltip("URL of text file containing captions for images, one caption per line.")]
    private VRCUrl stringUrl;
    
    [SerializeField, Tooltip("Renderer donde mostrar imágenes descargadas.")]
    private new Renderer renderer;
    
    [SerializeField, Tooltip("Text field for captions.")]
    private Text field;
    
    [SerializeField, Tooltip("Duración en segundos de cada imagen.")]
    private float slideDurationSeconds = 10f;
    
    private int _loadedIndex = -1;
    private VRCImageDownloader _imageDownloader;
    private IUdonEventReceiver _udonEventReceiver;
    private string[] _captions = new string[0];
    private Texture2D[] _downloadedTextures;
    private VRCUrlInputField urlField;

    private void Start()
    {
        _downloadedTextures = new Texture2D[imageUrls.Length];
        _imageDownloader = new VRCImageDownloader();
        _udonEventReceiver = (IUdonEventReceiver)this;
        VRCStringDownloader.LoadUrl(stringUrl, _udonEventReceiver);
        LoadNextRecursive();
    }

    public void LoadNextRecursive()
    {
        LoadNext();
        SendCustomEventDelayedSeconds(nameof(LoadNextRecursive), slideDurationSeconds);
    }
    
    private void LoadNext()
    {
        _loadedIndex = (int)(Networking.GetServerTimeInMilliseconds() / 1000f / slideDurationSeconds) % imageUrls.Length;
        var nextTexture = _downloadedTextures[_loadedIndex];
        
        if (nextTexture != null)
        {
            renderer.sharedMaterial.mainTexture = nextTexture;
        }
        else
        {
            var rgbInfo = new TextureInfo();
            rgbInfo.GenerateMipMaps = true;
            _imageDownloader.DownloadImage(imageUrls[_loadedIndex], renderer.material, _udonEventReceiver, rgbInfo);
        }
    }

    public GameObject imageButton;
    public void LoadImage()
    {
        var nextTexture = _downloadedTextures[0];
        
        if (nextTexture != null)
        {
            renderer.sharedMaterial.mainTexture = nextTexture;
        }
        else
        {
            var rgbInfo = new TextureInfo();
            rgbInfo.GenerateMipMaps = true;
            _imageDownloader.DownloadImage(imageUrls[_loadedIndex], renderer.material, _udonEventReceiver, rgbInfo);
        }
        Debug.Log("Imagen cargada");
    }

    public GameObject reSyncButton;
    public void ReSync()
    {
        SendCustomNetworkEvent(NetworkEventTarget.All, "ToggleReSync");
    }

    public void ToggleReSync()
    {
        var nextTexture = _downloadedTextures[0];
        
        if (nextTexture != null)
        {
            renderer.sharedMaterial.mainTexture = nextTexture;
        }
        else
        {
            var rgbInfo = new TextureInfo();
            rgbInfo.GenerateMipMaps = true;
            _imageDownloader.DownloadImage(imageUrls[_loadedIndex], renderer.material, _udonEventReceiver, rgbInfo);
        }
        Debug.Log("ReSync activado");
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        if (Networking.IsMaster)
        {
            SendCustomNetworkEvent(NetworkEventTarget.All, "ToggleReSyncOnMasterJoin");
        }
    }

    public void ToggleReSyncOnMasterJoin()
    {
        var nextTexture = _downloadedTextures[0];
        
        if (nextTexture != null)
        {
            renderer.sharedMaterial.mainTexture = nextTexture;
        }
        else
        {
            var rgbInfo = new TextureInfo();
            rgbInfo.GenerateMipMaps = true;
            _imageDownloader.DownloadImage(imageUrls[_loadedIndex], renderer.material, _udonEventReceiver, rgbInfo);
        }
        Debug.Log("ReSync activado");
    }

    public GameObject removeButton;
    public void RemoveImage()
    {
        //Cuando el botón se presione, pasar a la siguiente imagen
        _loadedIndex = (_loadedIndex + 1) % imageUrls.Length;
        
        var nextTexture = _downloadedTextures[_loadedIndex];

        if (nextTexture != null)
        {
            renderer.sharedMaterial.mainTexture = nextTexture;
        }
        else
        {
            var rgbInfo = new TextureInfo();
            rgbInfo.GenerateMipMaps = true;
            _imageDownloader.DownloadImage(imageUrls[_loadedIndex], renderer.material, _udonEventReceiver, rgbInfo);
        }

        Debug.Log("Imagen eliminada");
    }

    public void OnURLInput()
    {
        //Función para obtener la URL del InputField del banner
        var url = urlField.GetUrl();
        ShowURL(url);
        Debug.Log("URL ingresada"); 
    }

    public void ShowURL(VRCUrl url)
    {
        var rgbInfo = new TextureInfo();
        rgbInfo.GenerateMipMaps = true;
        _imageDownloader.DownloadImage(url, renderer.material, _udonEventReceiver, rgbInfo);
        Debug.Log($"Descargando imagen de: {url}");
    }

    public override void OnImageLoadSuccess(IVRCImageDownload result)
    {
        Debug.Log($"Imagen cargada: {result.SizeInMemoryBytes} bytes.");
        
        _downloadedTextures[_loadedIndex] = result.Result;
        renderer.sharedMaterial.mainTexture = result.Result;
    }

    public override void OnImageLoadError(IVRCImageDownload result)
    {
        Debug.Log($"Imagen no cargada: {result.Error.ToString()}: {result.ErrorMessage}.");
    }

    private void OnDestroy()
    {
        _imageDownloader.Dispose();
        Debug.Log("BannerSystem destruido.");
    }
}
}