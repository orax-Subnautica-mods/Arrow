using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Utility;
using Nautilus.Assets.PrefabTemplates;
using UnityEngine;
using System.IO;

namespace Arrow;

public class Arrow
{
    public const string AssetGameObject = "Arrow";
    public const string AssetModel = "model";
    public const string CustomPrefabClassId = "CustomArrowPrefab";
    public const string RecipeFile = "recipe.json";
    public const string ObjectClassId = "Arrow";

    private static PrefabInfo PrefabInfo { get; set; }

    public string Id { get; private set; }
    public GameObject GameObject { get; private set; }
    public Material Material { get; private set; }

    public struct Config
    {
        public string Name;
        public string Description;
        public Color Color;
        public float Intensity;
        public Vector3 Scale;
        public bool IsHologram;
        public bool ChangeIconColorInPDA;
        public string ImageIconFile;
        public Color IconColorInPDA;
        public float AlphaIconInPDA;
        public bool ForceUpright;
        public float PlaceDefaultDistance;
        public float PlaceMaxDistance;
    }

    public Config Cfg;

    public Arrow(string id)
    {
        Id = id;
        Plugin.ArrowsList.Add(id, this);
        Register();
    }

    public void ApplyHologramOption()
    {
        if (GameObject == null)
        {
            return;
        }

        BoxCollider boxCollider = GameObject.GetComponentInChildren<BoxCollider>();
        if (boxCollider != null)
        {
            if (Cfg.IsHologram == true)
            {

                boxCollider.isTrigger = true;
                GameObject.layer = LayerID.Useable;
            }
            else
            {
                boxCollider.isTrigger = false;
                GameObject.layer = LayerID.Default;
            }
        }
    }

    public void ApplyColorOption()
    {
        if (Material != null)
        {
            Color color = Cfg.Color;
            float intensity = Cfg.Intensity;

            Material.SetColor("_Color", color);
            Material.SetColor("_EmissionColor", color * Mathf.Pow(2, intensity));
        }
    }

    public static void LoadAssets()
    {
        PrefabInfo prefabInfo = PrefabInfo.WithTechType(CustomPrefabClassId);

        // Cache the tech type for use in other places
        PrefabInfo = prefabInfo;

        var prefab = new CustomPrefab(prefabInfo);

        prefab.SetGameObject(GetAssetBundlePrefab());

        // Register the prefab to the Nautilus prefab database
        prefab.Register();
    }

    private static GameObject GetAssetBundlePrefab()
    {
        GameObject prefab = Plugin.AssetBundle.LoadAsset<GameObject>(AssetGameObject);

        PrefabUtils.AddBasicComponents(
            prefab,
            PrefabInfo.ClassID,
            PrefabInfo.TechType,
            Plugin.ModOptions.CellLevel);

        // Return the GameObject with all the components added
        return prefab;
    }

    public void Register()
    {
        Plugin.ModOptions.LoadArrowOptions(this);

        // load the image file that will be displayed as icon in the PDA
        Atlas.Sprite sprite = null;
        if (Cfg.ChangeIconColorInPDA)
        {
            // load the image file and change its color

            Texture2D texture2D = ImageUtils.LoadTextureFromFile(
                Path.Combine(Plugin.AssetsFolder, Cfg.ImageIconFile));

            // change the color of each non-transparent pixel
            Color[] colors = texture2D.GetPixels();
            for (int i = 0; i < colors.Length; i++)
                if (colors[i].a != 0f)
                {
                    // apply a "multiply" blend mode
                    colors[i].r = colors[i].r * Cfg.Color.r;
                    colors[i].g = colors[i].g * Cfg.Color.g;
                    colors[i].b = colors[i].b * Cfg.Color.b;
                    colors[i].a = colors[i].a * Cfg.AlphaIconInPDA;
                }
            texture2D.SetPixels(colors);
            texture2D.Apply();

            // convert the modified texture in Atlas.Sprite
            sprite = ImageUtils.LoadSpriteFromTexture(texture2D);
        }
        else
        {
            // else load unmodified image
            sprite = ImageUtils.LoadSpriteFromFile(Path.Combine(Plugin.AssetsFolder, Cfg.ImageIconFile));
        }

        var info = PrefabInfo
            .WithTechType(ObjectClassId + Id, Cfg.Name, Cfg.Description)
            .WithIcon(sprite);

        var prefab = new CustomPrefab(info);
        var clone = new CloneTemplate(info, CustomPrefabClassId);

        clone.ModifyPrefab += obj =>
        {
            GameObject = obj;

            GameObject model = obj.transform.Find(AssetModel).gameObject;
            Material = model.GetComponent<Renderer>().material;

            // set all flags
            ConstructableFlags constructableFlags =
                        ConstructableFlags.Ground |
                        ConstructableFlags.Wall |
                        ConstructableFlags.Ceiling |
                        ConstructableFlags.Base |
                        ConstructableFlags.Submarine |
                        ConstructableFlags.Outside |
                        ConstructableFlags.AllowedOnConstructable |
                        ConstructableFlags.Rotatable;

            Constructable cstr = PrefabUtils.AddConstructable(
                        obj,
                        PrefabInfo.TechType,
                        constructableFlags,
                        model);

            // place distance            
            cstr.placeDefaultDistance = Cfg.PlaceDefaultDistance;
            cstr.placeMaxDistance = Cfg.PlaceMaxDistance;

            // force upright
            cstr.forceUpright = Cfg.ForceUpright;

            // hologram
            ApplyHologramOption();

            // color, intensity
            ApplyColorOption();

            // scale
            obj.transform.localScale = Cfg.Scale;
        };

        prefab.SetGameObject(clone);

        // assign it to the correct tab in the builder tool
        prefab.SetPdaGroupCategory(Plugin.ModOptions.TechGroup, Plugin.ModOptions.TechCategory);

        prefab.SetRecipeFromJson(Path.Combine(Plugin.ModPath, RecipeFile));

        prefab.Register();
    }
}
