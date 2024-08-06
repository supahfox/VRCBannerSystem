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

    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
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

        [SerializeField]
        private VRCUrlInputField InputField;

        [UdonSynced, FieldChangeCallback(nameof(SyncedImage))]
        private bool _syncedToggle;

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
        public bool SyncedImage
        {
            //Si alguien le da click al botón, se sincroniza la imagen
            set
            {
                if (value)
                {
                    LoadImage();
                    _syncedToggle = false;
                }
            }
        }

        public void ReSync()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            _syncedToggle = true;
            RequestSerialization();
            Debug.Log("Imagen sincronizada");
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (Networking.IsMaster)
            {
                SendCustomNetworkEvent(NetworkEventTarget.All, "ReSync");
            }
        }

        public GameObject removeButton;
        public void RemoveImage()
        {
            //Eliminar todas las imagenes y volver a cargar el array anterior por defecto
            imageUrls = new VRCUrl[0];
            _downloadedTextures = new Texture2D[imageUrls.Length];
            _imageDownloader = new VRCImageDownloader();
            _udonEventReceiver = (IUdonEventReceiver)this;
            LoadNextRecursive();
            Debug.Log("Imagen eliminada");
        }

        //Función para obtener la URL del InputField
        public void OnURLInput()
        {
            setURL(InputField.GetUrl());
            InputField.SetUrl(VRCUrl.Empty);
        }

        public void setURL(VRCUrl url)
        {
            // Borrar todo el array y remplazarlo por la imagen cargada
            imageUrls = new VRCUrl[1];
            imageUrls[0] = url;
            _downloadedTextures = new Texture2D[imageUrls.Length];
            _imageDownloader = new VRCImageDownloader();
            _udonEventReceiver = (IUdonEventReceiver)this;
            LoadNextRecursive();
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