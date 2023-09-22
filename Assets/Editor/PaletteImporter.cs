using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

[ScriptedImporter(1, "pal")]
public class PaletteImporter : ScriptedImporter {
    public Vector3Int mapResolution = Vector3Int.one * 16;

    private Color SRGBToLinear(Color color) {
        return new Color(Mathf.Pow(color.r, 1f / 2.2f), Mathf.Pow(color.g, 1f / 2.2f), Mathf.Pow(color.b, 1f / 2.2f));
    }

    private Color LinearToSRGB(Color color) {
        return new Color(Mathf.Pow(color.r, 2.2f), Mathf.Pow(color.g, 2.2f), Mathf.Pow(color.b, 2.2f));
    }

    public override void OnImportAsset(AssetImportContext ctx) {
        byte[] bytes = File.ReadAllBytes(assetPath);

        Texture2D texture = new Texture2D(0, 0);
        texture.LoadImage(bytes);
        texture.filterMode = FilterMode.Point;

        // set description and icon
        TextAsset description = new TextAsset("A color palette for cel shading. Only colors in this texture is used for cel shading. ");
        ctx.AddObjectToAsset("Palette File", description, texture);

        // create texture map
        Color[] pixels = texture.GetPixels();

        HashSet<Vector3> colors = new HashSet<Vector3>();
        foreach (Color pixel in pixels) {
            colors.Add(new Vector3(pixel.r, pixel.g, pixel.b));
        }

        Texture3D map = new Texture3D(mapResolution.x, mapResolution.y, mapResolution.z, TextureFormat.RGBA32, false);
        for (int z = 0; z < mapResolution.z; z++) {
            for (int y = 0; y < mapResolution.y; y++) {
                for (int x = 0; x < mapResolution.x; x++) {
                    Vector3 coordinateColor = new Vector3((float)x / (mapResolution.x - 1), (float)y / (mapResolution.x - 1), (float)z / (mapResolution.z - 1));
                    float minimumDistance = Mathf.Infinity;
                    Vector3 minimumColor = Vector3.zero;
                    foreach (Vector3 color in colors) {
                        float distance = Vector3.Distance(color, coordinateColor);
                        if (distance < minimumDistance) {
                            minimumDistance = distance;
                            minimumColor = color;
                        }
                    }
                    map.SetPixel(x, y, z, LinearToSRGB(new Color(minimumColor.x, minimumColor.y, minimumColor.z)));
                }
            }
        }
        map.name = Path.GetFileNameWithoutExtension(assetPath) + "Map";
        map.wrapMode = TextureWrapMode.Clamp;
        map.filterMode = FilterMode.Point;

        ctx.AddObjectToAsset("Calculated Texture", map);
    }
}
