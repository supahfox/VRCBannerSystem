using UdonSharp;
using UnityEngine;
using VRC.SDK3.Image;
using VRC.SDKBase;
using VRC.Udon;

public class LoadingStaticImage : UdonSharpBehaviour
{

    VRCImageDownloader image;
    public VRCUrl URL;
    public Material mat;
    TextureInfo TexInf = new TextureInfo();
    
    public Texture defaultTex;

    UdonBehaviour garbo;

    public bool IsEmissive = true;

    void Start()
    {
        defaultTexure();
        garbo = gameObject.GetComponent<UdonBehaviour>();
        image = new VRCImageDownloader();
        TexInf.GenerateMipMaps = true; 
        image.DownloadImage(URL, mat, garbo, TexInf);
        
    }

    public override void OnImageLoadError(IVRCImageDownload result)
    {
        Debug.LogWarning("No se pudo cargar imagen");
        defaultTexure();
    }

    public void defaultTexure()
    {
        mat.SetTexture("_MainTex", defaultTex);
        if (IsEmissive)
        {
            mat.SetTexture("_EmissionMap", defaultTex);
        }
        
    }
    public override void OnImageLoadSuccess(IVRCImageDownload result)
    {

        Debug.Log("imagen cargada");
        mat.SetTexture("_MainTex", result.Result);
        if (IsEmissive)
        {
            mat.SetTexture("_EmissionMap", result.Result);
        }
    }
}