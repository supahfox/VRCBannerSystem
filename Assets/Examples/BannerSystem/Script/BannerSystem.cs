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
using VRC.Udon.Common.Interfaces;

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
    
    private void Start()
    {
        // Imágenes descargadas a un array de texturas
        _downloadedTextures = new Texture2D[imageUrls.Length];
        
        // VRCImageDownloader a una variable así no recolecto tanta basura xd
        _imageDownloader = new VRCImageDownloader();
        
        // Para recibir eventos de carga
        _udonEventReceiver = (IUdonEventReceiver)this;
        
        // Captions are downloaded once. On success, OnImageLoadSuccess() will be called. IGNORAR 1ER ARGUMENTO
        VRCStringDownloader.LoadUrl(stringUrl, _udonEventReceiver);
        
        // Cargar siguiente imagen indefinidamente
        LoadNextRecursive();
    }

    public void LoadNextRecursive()
    {
        LoadNext();
        SendCustomEventDelayedSeconds(nameof(LoadNextRecursive), slideDurationSeconds);
    }
    
    private void LoadNext()
    {
        // Esto sirve para sincronizar la imagen cargada.
        _loadedIndex = (int)(Networking.GetServerTimeInMilliseconds() / 1000f / slideDurationSeconds) % imageUrls.Length;

        var nextTexture = _downloadedTextures[_loadedIndex];
        
        if (nextTexture != null)
        {
            // Si la imagen ya está descargada, no descargarla de nuevo
            renderer.sharedMaterial.mainTexture = nextTexture;
        }
        else
        {
            var rgbInfo = new TextureInfo();
            rgbInfo.GenerateMipMaps = true;
            _imageDownloader.DownloadImage(imageUrls[_loadedIndex], renderer.material, _udonEventReceiver, rgbInfo);
        }
    }

    // CARGAR IMAGEN
    public GameObject imageButton;
    public void LoadImage()
    {
        //Si el botón es presionado, se carga la imagen
        // Esto sirve para sincronizar la imagen cargada.

        var nextTexture = _downloadedTextures[0];
        
        if (nextTexture != null)
        {
            // Si la imagen ya está descargada, no descargarla de nuevo
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

    //RESYNC
    public GameObject reSyncButton;

    public void ReSync()
    {
        //Enviar a todos los miembros de la instancia el evento de reSync
        SendCustomNetworkEvent(NetworkEventTarget.All, "ToggleReSync");
    }

    public void ToggleReSync()
    {
        //Si el botón es presionado, se carga la imagen
        // Esto sirve para sincronizar la imagen cargada.

        var nextTexture = _downloadedTextures[0];
        
        if (nextTexture != null)
        {
            // Si la imagen ya está descargada, no descargarla de nuevo
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
            //Si el jugador que se une es el master, activar el botón de reSync
                SendCustomNetworkEvent(NetworkEventTarget.All, "ToggleReSyncOnMasterJoin");
        }
    }

    public void ToggleReSyncOnMasterJoin()
    {
        var nextTexture = _downloadedTextures[0];
        
        if (nextTexture != null)
        {
            // Si la imagen ya está descargada, no descargarla de nuevo
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

    //REMOVE IMAGEN
    public GameObject removeButton;

    public void RemoveImage()
    {
        LoadNext();
        Debug.Log("Imagen removida");
    }

    public override void OnImageLoadSuccess(IVRCImageDownload result)
    {
        Debug.Log($"Imagen cargada: {result.SizeInMemoryBytes} bytes.");
        
        _downloadedTextures[_loadedIndex] = result.Result;
    }

    public override void OnImageLoadError(IVRCImageDownload result)
    {
        Debug.Log($"Imagen no cargada: {result.Error.ToString()}: {result.ErrorMessage}.");
    }

    private void OnDestroy()
    {
        _imageDownloader.Dispose();
    }
}