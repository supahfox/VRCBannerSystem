﻿/*
VRCBannerSystem
Author: @supahfox
Description: Un sistema de banners que descarga imágenes y texto de una URL y las muestra en un plano.
https://github.com/supahfox/VRCBannerSystem
*/

using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDK3.Image;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class BannerSystem : UdonSharpBehaviour
{
    [SerializeField, Tooltip("URL de los links puestos en el InputField.")]
    private VRCUrl[] arrayURL;

    [SerializeField, Tooltip("URL por default de la imagen.")]
    private VRCUrl defaultURL;

    [SerializeField, Tooltip("URL of text file containing captions for images, one caption per line.")]
    private VRCUrl stringUrl;
    
    [SerializeField, Tooltip("Material donde mostrar imágenes descargadas.")]
    private Material material;
    
    [SerializeField, Tooltip("Text field for captions.")]
    private Text field;
    
    [SerializeField, Tooltip("Duración en segundos de cada imagen.")]
    private float slideDurationSeconds = 10f;
    
    private int _loadedIndex = -1;
    private VRCImageDownloader _imageDownloader;
    private IUdonEventReceiver _udonEventReceiver;
    private string[] _captions = new string[0];
    private Texture2D[] _downloadedTextures;

    [SerializeField]
    private VRCUrlInputField InputField;

    [UdonSynced, FieldChangeCallback(nameof(SyncedImageURL))]
    private VRCUrl _syncedImageURL;

    private void Start()
    {
        InitializeArrayURL();
        _downloadedTextures = new Texture2D[arrayURL.Length];
        _imageDownloader = new VRCImageDownloader();
        _udonEventReceiver = (IUdonEventReceiver)this;
        VRCStringDownloader.LoadUrl(stringUrl, _udonEventReceiver);
        LoadNextRecursive();
    }

    private void InitializeArrayURL()
    {
        arrayURL = new VRCUrl[1];
        arrayURL[0] = defaultURL;
        _syncedImageURL = defaultURL;
    }

    public void LoadNextRecursive()
    {
        LoadNext();
        SendCustomEventDelayedSeconds(nameof(LoadNextRecursive), slideDurationSeconds);
    }
    
    private void LoadNext()
    {
        _loadedIndex = (int)(Networking.GetServerTimeInMilliseconds() / 1000f / slideDurationSeconds) % arrayURL.Length;
        var nextTexture = _downloadedTextures[_loadedIndex];
        
        if (nextTexture != null)
        {
            ApplyTextureToMaterial(nextTexture);
        }
        else
        {
            var rgbInfo = new TextureInfo();
            rgbInfo.GenerateMipMaps = true;
            _imageDownloader.DownloadImage(arrayURL[_loadedIndex], material, _udonEventReceiver, rgbInfo);
        }
    }

    private void ApplyTextureToMaterial(Texture2D texture)
    {
        if (material != null)
        {
            material.SetTexture("_MainTex", texture);
            material.SetTexture("_EmissionMap", texture);
        }
        else
        {
            Debug.LogError("Material no asignado en el inspector.");
        }
    }

    public void LoadImage()
    {
        // Cuando el botón es presionado, cargar la imagen del InputField
        _downloadedTextures = new Texture2D[arrayURL.Length];
        _imageDownloader = new VRCImageDownloader();
        _udonEventReceiver = (IUdonEventReceiver)this;
        LoadNextRecursive();
    }

    public GameObject reSyncButton;
    public VRCUrl SyncedImageURL
    {
        set
        {
            if (_syncedImageURL != value)
            {
                _syncedImageURL = value;
                arrayURL[0] = _syncedImageURL;
                LoadImage();
            }
        }
    }

    public void ReSync()
    {
        Debug.Log("ReSync button clickeado. Sincronizando imagen...");
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        _syncedImageURL = arrayURL[0];
        RequestSerialization();
        Debug.Log("Imagen sincronizada");
    }

    public override void OnDeserialization()
    {
        Debug.Log("OnDeserialization called. Syncing image if needed.");
        SyncedImageURL = _syncedImageURL;
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        if (Networking.IsMaster)
        {
            Debug.Log("Master player detected. Sending resync event to all.");
            SendCustomNetworkEvent(NetworkEventTarget.All, "ReSync");
        }
    }

    public GameObject removeButton;
    public void RemoveImage()
    {
        // Mostrar imagen por default
        _downloadedTextures = new Texture2D[arrayURL.Length];
        _imageDownloader = new VRCImageDownloader();
        _udonEventReceiver = (IUdonEventReceiver)this;
        // Cuando el botón es presionado se llamará a InitializeArrayURL() para setear la URL por default
        InitializeArrayURL();
        LoadNextRecursive();
    }

    // Función para obtener la URL del InputField
    public void OnURLInput()
    {
        setURL(InputField.GetUrl());
        InputField.SetUrl(VRCUrl.Empty);
    }

    public void setURL(VRCUrl url)
    {
        // Settear la imagen del inputfield y cargarla
        arrayURL = new VRCUrl[1];
        arrayURL[0] = url;
        _syncedImageURL = url;
        _downloadedTextures = new Texture2D[arrayURL.Length];
        _imageDownloader = new VRCImageDownloader();
        _udonEventReceiver = (IUdonEventReceiver)this;
        RequestSerialization();
        LoadNextRecursive();
        Debug.Log("URL seteada");
    }

    public override void OnImageLoadSuccess(IVRCImageDownload result)
    {
        Debug.Log($"Imagen cargada: {result.SizeInMemoryBytes} bytes.");
        
        _downloadedTextures[_loadedIndex] = result.Result;
        ApplyTextureToMaterial(result.Result);
    }

    public override void OnImageLoadError(IVRCImageDownload result)
    {
        Debug.Log($"Imagen no cargada: {result.Error.ToString()}: {result.ErrorMessage}.");
    }

    private void OnDestroy()
    {
        ClearDownloadedTextures();
        DisposeImageDownloader();
        Debug.Log("BannerSystem destruido.");
    }

    private void ClearDownloadedTextures()
    {
        for (int i = 0; i < _downloadedTextures.Length; i++)
        {
            if (_downloadedTextures[i] != null)
            {
                Destroy(_downloadedTextures[i]);
                _downloadedTextures[i] = null;
            }
        }
    }

    private void DisposeImageDownloader()
    {
        if (_imageDownloader != null)
        {
            _imageDownloader.Dispose();
            _imageDownloader = null;
        }
    }
}