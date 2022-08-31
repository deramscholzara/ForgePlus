using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using ForgePlus.LevelManipulation.Utilities;
using UnityEngine;
using Weland;

public class PluginLoading_Texture : SingletonMonoBehaviour<PluginLoading_Texture>
{
    public Dictionary<ShapeDescriptor, PluginTextureSet> TextureLookup = new Dictionary<ShapeDescriptor, PluginTextureSet>();

    [SerializeField] private bool PluginSupportEnabled = true;

    // TODO: Make this load plugins data, but not the actual textures
    // TODO: Make plugin texture loading on-demand based on loaded plugin data,
    //       similar to ShapesLoading.Instance.GetShape
    private void Start()
    {
        if (!PluginSupportEnabled) return;

        var dataPathRoot = new DirectoryInfo(Path.Join(Application.dataPath, "MML_Plugins"));

        // Load all plugin XML data
        var xmlFiles = dataPathRoot.GetFiles("*.xml", SearchOption.AllDirectories);
        foreach (var xmlFile in xmlFiles)
        {
            var xmlSerializer = new XmlSerializer(typeof(Plugin));
            using (var xmlStream = new FileStream(xmlFile.FullName, FileMode.Open))
            {
                var plugin = xmlSerializer.Deserialize(xmlStream) as Plugin;
                if (plugin != null)
                {
                    Debug.Log($"Detected Plugin: {plugin.Name}" +
                              $"\nDescription: {plugin.Description}" +
                              $"\nMML Relative Path: {plugin.MmlInfo.File}" +
                              $"\nPlugin Path: {xmlFile.FullName}");

                    // Load Texture MML data
                    var mmlFile = new FileInfo(Path.Join(xmlFile.DirectoryName, plugin.MmlInfo.File));
                    var mmlSerializer = new XmlSerializer(typeof(MML));
                    using (var mmlStream = new FileStream(mmlFile.FullName, FileMode.Open))
                    {
                        var mml = mmlSerializer.Deserialize(mmlStream) as MML;

                        // Load all texture files
                        foreach (var texture in mml.Opengl.Textures)
                        {
                            // Debug.Log($"Detected Texture: Collection ({texture.Collection}) Bitmap ({texture.Bitmap})" +
                            //           $"\nNormal Image: {texture.NormalImage}" +
                            //           $"\nGlow Image: {texture.GlowImage}" +
                            //           $"\nGlow Bloom Scale: {texture.GlowBloomScale}" +
                            //           $"\nMinimum Glow Intensity: {texture.MinimumGlowIntensity}" +
                            //           $"\nOpacity Type: {texture.OpacityType}" +
                            //           $"\nType: {texture.Type}");

                            // Store mappings for loading
                            var textureSet = new PluginTextureSet();
                            if (!string.IsNullOrEmpty(texture.NormalImage))
                                textureSet.MainTexture = LoadTextureAtPath(Path.Join(mmlFile.DirectoryName, texture.NormalImage));

                            var shapeDescriptor = new ShapeDescriptor();
                            shapeDescriptor.Collection = (byte) texture.Collection;
                            shapeDescriptor.Bitmap = (byte) texture.Bitmap;
                            shapeDescriptor.CLUT = 0;
                            TextureLookup[shapeDescriptor] = textureSet;
                        }
                    }
                }
            }
        }
    }

    private Texture2D LoadTextureAtPath(string path)
    {
        var extension = Path.GetExtension(path);

        if (!extension.Equals(".dds", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
            return null;

        var fileBytes = File.ReadAllBytes(path);
        Texture2D loadedTexture = null;

        if (extension.Equals(".dds", StringComparison.OrdinalIgnoreCase))
        {
            loadedTexture = LoadTextureDXT(fileBytes, Path.GetFileNameWithoutExtension(path));

            var projectPath = path.Substring(path.IndexOf("\\Assets\\") + 1);
        }
        else if (extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
        {
            loadedTexture = new Texture2D(2, 2);
            loadedTexture.LoadImage(fileBytes);
        }

        return loadedTexture;
    }

    private Texture2D LoadTextureDXT(byte[] ddsBytes, string textureName)
    {
        TextureFormat srcTextureFormat;
        if (ddsBytes[84] == 0 && ddsBytes[85] == 0 && ddsBytes[86] == 0 && ddsBytes[87] == 0) // BGR8 format
        {
            // Unity doesn't functionally support reading BGR8 format,
            // so we read it as RGB24 and then fix it afterwards (below)
            srcTextureFormat = TextureFormat.RGB24;
        }
        else if (ddsBytes[84] == 68 && ddsBytes[85] == 88 && ddsBytes[86] == 84)
        {
            switch (ddsBytes[87])
            {
                case 49:
                    srcTextureFormat = TextureFormat.DXT1;
                    break;
                case 53:
                    srcTextureFormat = TextureFormat.DXT5;
                    break;
                default:
                    return null;
            }
        }
        else
            return null;

        byte ddsSizeCheck = ddsBytes[4];
        if (ddsSizeCheck != 124)
        {
            Debug.LogError("Invalid DDS DXTn texture. Unable to read"); //this header byte should be 124 for DDS image files
            return null;
        }

        int height = ddsBytes[13] * 256 + ddsBytes[12];
        int width = ddsBytes[17] * 256 + ddsBytes[16];

        int DDS_HEADER_SIZE = 128;
        byte[] dxtBytes = new byte[ddsBytes.Length - DDS_HEADER_SIZE];
        Buffer.BlockCopy(ddsBytes, DDS_HEADER_SIZE, dxtBytes, 0, ddsBytes.Length - DDS_HEADER_SIZE);

        Texture2D texture = new Texture2D(width, height, srcTextureFormat, false);
        texture.LoadRawTextureData(dxtBytes);
        texture.Apply();

        // DDS textures come in upside-down, for whatever reason,
        // so flip them here to fix the issue.
        var oldPixels = texture.GetPixels();
        var newPixels = new Color[oldPixels.Length];

        TextureFormat dstTextureFormat = TextureFormat.RGB24;
        if (srcTextureFormat == TextureFormat.DXT1)
        {
            dstTextureFormat = TextureFormat.RGB24;
        }
        else if (srcTextureFormat == TextureFormat.DXT5)
        {
            dstTextureFormat = TextureFormat.RGBA32;
        }

        var flippedTexture = new Texture2D(texture.width, texture.height, dstTextureFormat, mipChain: true, linear: false);
        for (var y = 0; y < texture.height; y++)
        {
            for (var x = 0; x < texture.width; x++)
            {
                var old_y = texture.height - 1 - y;
                var oldPixelIndex = old_y * texture.width + x;
                var newPixelIndex = y * texture.width + x;
                var oldColor = oldPixels[oldPixelIndex];

                // Unity doesn't support BGR8 format, so we have to swap the R and B color channels
                if (ddsBytes[84] == 0 && ddsBytes[85] == 0 && ddsBytes[86] == 0 && ddsBytes[87] == 0) // BGR8 format
                {
                    newPixels[newPixelIndex] = new Color(oldColor.b, oldColor.g, oldColor.r, oldColor.a);
                }
                else
                {
                    newPixels[newPixelIndex] = oldColor;
                }
            }
        }

        Destroy(texture);

        flippedTexture.SetPixels(newPixels);

        if (MathUtilities.IsPowerOfTwo(flippedTexture.width) &&
            MathUtilities.IsPowerOfTwo(flippedTexture.height))
        {
            flippedTexture.Apply(updateMipmaps: true, makeNoLongerReadable: false);
            flippedTexture.Compress(highQuality: true);
            flippedTexture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
        }
        else
        {
            flippedTexture.Apply(updateMipmaps: true, makeNoLongerReadable: true);
        }

        flippedTexture.filterMode = FilterMode.Trilinear;
        flippedTexture.name = textureName;

        return flippedTexture;
    }
}

public class PluginTextureSet
{
    public Texture2D MainTexture;
    public Texture2D EmissionTexture;
    public float EmissionIntensity;
    public float MinimumEmissionIntensity;
    public uint OpacityType;
    public uint Type;

    public void DestroyTextures()
    {
        if (MainTexture) UnityEngine.Object.Destroy(MainTexture);
        if (MainTexture) UnityEngine.Object.Destroy(EmissionTexture);
    }
}

[XmlRoot(ElementName = "plugin")]
public class Plugin
{
    [XmlElement(ElementName = "mml")] public MmlInfo MmlInfo { get; set; }
    [XmlAttribute(AttributeName = "name")] public string Name { get; set; }

    [XmlAttribute(AttributeName = "description")]
    public string Description { get; set; }
}

[XmlRoot(ElementName = "mml")]
public class MmlInfo
{
    [XmlAttribute(AttributeName = "file")] public string File { get; set; }
}

[XmlRoot(ElementName = "marathon")]
public class MML
{
    [XmlElement(ElementName = "opengl")] public Opengl Opengl { get; set; }
}

[XmlRoot(ElementName = "opengl")]
public class Opengl
{
    [XmlElement(ElementName = "texture")] public List<Texture> Textures { get; set; }
}

[XmlRoot(ElementName = "texture")]
public class Texture
{
    [XmlAttribute(AttributeName = "coll")] public uint Collection { get; set; }

    [XmlAttribute(AttributeName = "bitmap")]
    public uint Bitmap { get; set; }

    [XmlAttribute(AttributeName = "normal_image")]
    public string NormalImage { get; set; }

    [XmlAttribute(AttributeName = "glow_image")]
    public string GlowImage { get; set; }

    [XmlAttribute(AttributeName = "glow_bloom_scale")]
    public float GlowBloomScale { get; set; }

    [XmlAttribute(AttributeName = "minimum_glow_intensity")]
    public float MinimumGlowIntensity { get; set; }

    [XmlAttribute(AttributeName = "type")] public uint Type { get; set; }

    [XmlAttribute(AttributeName = "opac_type")]
    public uint OpacityType { get; set; }
}