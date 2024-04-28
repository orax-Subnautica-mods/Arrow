using BepInEx.Configuration;
using Nautilus.Options;
using UnityEngine;
using System.Text.RegularExpressions;

namespace Arrow;

public class ArrowModOptions : ModOptions
{
    private const string prefixSectionArrow = "Arrow_";
    private const string restartMsg = "Restart the game to take effect.";
    private const string advancedMsg = "(advanced)";
    private const string notModifyExistingMsg = "This does not modify the existing ones.";
    private const string restartToModifyExistingMsg = "Require a restart to modify the existing ones.";

    public int NumberOfArrows { get; private set; }
    public TechGroup TechGroup { get; set; }
    public TechCategory TechCategory { get; set; }
    public bool AlignObjectBottomToGroundLevel { get; set; }
    public LargeWorldEntity.CellLevel CellLevel { get; set; }

    // The base ModOptions class takes a string name as an argument
    public ArrowModOptions() : base(PluginInfo.PLUGIN_NAME)
    {
        LoadGeneralOptions();
    }

    private void LoadGeneralOptions()
    {
        const string section = "General";

        // number of arrows
        ConfigEntry<int> numberOfArrows = Plugin.Instance.ConfigBind(
            // The section under which the option is shown
            section,
            // The key of the configuration option in the configuration file
            "Number of arrows",
            // The default value
            1,
            // Description of the option to show in the config file
            $"Number of different arrows you want. {restartMsg}");
        ModSliderOption numberOfArrowsOption = numberOfArrows.ToModSliderOption(1, 10, 1);
        AddItem(numberOfArrowsOption);
        NumberOfArrows = numberOfArrows.Value;

        // tech group (advenced)
        ConfigEntry<TechGroup> techGroup = Plugin.Instance.ConfigBind(
            section, "Tech group", TechGroup.BasePieces,
            $"{advancedMsg} Tech group. Group tab in the builder tool. {restartMsg}");
        TechGroup = techGroup.Value;

        // tech category (advanced)
        ConfigEntry<TechCategory> techcategory = Plugin.Instance.ConfigBind(
            section, "Tech category", TechCategory.BasePiece,
            $"{advancedMsg} Tech category. Category tab in the builder tool. {restartMsg}");
        TechCategory = techcategory.Value;

        //  LargeWorldEntity.CellLevel (advanced)
        ConfigEntry<LargeWorldEntity.CellLevel> cellLevel = Plugin.Instance.ConfigBind(
            section, "Cell level", LargeWorldEntity.CellLevel.Medium,
            $"{advancedMsg} Level of distance this prefab can stay visible before unloading. (LargeWorldEntity.CellLevel)");
        CellLevel = cellLevel.Value;
    }

    public void LoadArrowOptions(Arrow arrow)
    {
        string section = $"{prefixSectionArrow}{arrow.Id}";

        // name
        ConfigEntry<string> configEntryName = Plugin.Instance.ConfigBind(
            section, "Name", $"Arrow {arrow.Id}", "Arrow name");
        arrow.Cfg.Name = configEntryName.Value;

        // description
        ConfigEntry<string> configEntryDescription = Plugin.Instance.ConfigBind(
            section, "Description", "Directional arrow.", "Arrow description");
        arrow.Cfg.Description = configEntryDescription.Value;

        // color
        ConfigEntry<Color> configEntryColor = Plugin.Instance.ConfigBind(
            section, $"Color (arrow id: {arrow.Id})", Color.green,
            $"Color value for arrow with id: {arrow.Id}");
        arrow.Cfg.Color = configEntryColor.Value;
        Color color = configEntryColor.Value;
        ModColorOption modColorOption = configEntryColor.ToModColorOption(false);
        modColorOption.OnChanged += Color_OnChanged;
        AddItem(modColorOption);

        // intensity
        ConfigEntry<float> configEntryIntensity = Plugin.Instance.ConfigBind(
            section, "Intensity", 0f,
            $"Intensity of the emission color.");
        arrow.Cfg.Intensity = configEntryIntensity.Value;
        float intensity = configEntryIntensity.Value;
        ModSliderOption intensitySliderOption = configEntryIntensity.ToModSliderOption(-4f, 4f, 0.1f);
        intensitySliderOption.OnChanged += Intensity_OnChanged;
        AddItem(intensitySliderOption);

        // scale
        ConfigEntry<Vector3> configEntryScale = Plugin.Instance.ConfigBind(
            section, "Scale", Vector3.one,
            $"Scale x,y,z. {notModifyExistingMsg}");
        arrow.Cfg.Scale = configEntryScale.Value;
        var scaleSliderOption = configEntryScale.ToModSliderOptions(0f, 10f, 0.1f);
        for (int i = 0; i < scaleSliderOption.Count; i++)
        {
            scaleSliderOption[i].OnChanged += Scale_OnChanged;
            AddItem(scaleSliderOption[i]);
        }

        // place default distance
        ConfigEntry<float> placeDefaultDistance = Plugin.Instance.ConfigBind(
            section, "Place default distance", 3f, "Place default distance.");
        arrow.Cfg.PlaceDefaultDistance = placeDefaultDistance.Value;
        ModSliderOption placeDefaultDistanceOption = placeDefaultDistance.ToModSliderOption(0f, 10f, 1f);
        placeDefaultDistanceOption.OnChanged += PlaceDefaultDistance_OnChanged;
        AddItem(placeDefaultDistanceOption);

        // place max distance
        ConfigEntry<float> placeMaxDistance = Plugin.Instance.ConfigBind(
            section, "Place max distance", 5f, "Place max distance.");
        arrow.Cfg.PlaceMaxDistance = placeMaxDistance.Value;
        ModSliderOption placeMaxDistanceOption = placeMaxDistance.ToModSliderOption(0f, 30, 1f);
        placeMaxDistanceOption.OnChanged += PlaceMaxDistance_OnChanged;
        AddItem(placeMaxDistanceOption);

        // force upright
        ConfigEntry<bool> forceUpright = Plugin.Instance.ConfigBind(
            section, "Force upright", false, $"Force upright when placing the object. {notModifyExistingMsg}");
        arrow.Cfg.ForceUpright = forceUpright.Value;
        ModToggleOption forceUprightOption = forceUpright.ToModToggleOption();
        forceUprightOption.OnChanged += ForceUpright_OnChanged;
        AddItem(forceUprightOption);

        // hologram
        ConfigEntry<bool> configEntryIsHologram = Plugin.Instance.ConfigBind(
            section, "Is hologram", false,
            $"Set true if you want to be able to traverse the object. {restartToModifyExistingMsg}");
        arrow.Cfg.IsHologram = configEntryIsHologram.Value;
        ModToggleOption isHologramOption = configEntryIsHologram.ToModToggleOption();
        isHologramOption.OnChanged += IsHologramOption_OnChanged;
        AddItem(isHologramOption);

        // image file that will be displayed as icon in PDA (advanced)
        ConfigEntry<string> imageIconInPDA = Plugin.Instance.ConfigBind(
            section, "Filename image in PDA", "arrow.png",
            $"{advancedMsg} Image filename of the image that will be displayed " +
            $"as icon in your PDA. {restartMsg}");
        arrow.Cfg.ImageIconFile = imageIconInPDA.Value;

        // true for change image icon color (advanced)
        ConfigEntry<bool> changeIconColorInPDA = Plugin.Instance.ConfigBind(
            section, "Change icon color in PDA", true,
            $"{advancedMsg} The color of the 2D object icon that will be displayed " +
            $"in your PDA will be the same as the 3D model. {restartMsg}");
        arrow.Cfg.ChangeIconColorInPDA = changeIconColorInPDA.Value;

        // transparency (alpha) of the image icon (advanced)
        ConfigEntry<float> alphaIconInPDA = Plugin.Instance.ConfigBind(
            section, "Alpha icon in PDA", 0.8f,
            $"{advancedMsg} Transparency (alpha) of the image icon in your PDA. " +
            $"Range: 0 (transparent) to 1 (opaque).");
        arrow.Cfg.AlphaIconInPDA = alphaIconInPDA.Value;
    }

    private string GetArrowId(string eventId)
    {
        // get the corresponding Arrow instance
        return Regex.Match(eventId, @"\d+").Value;
    }

    private void PlaceDefaultDistance_OnChanged(object sender, SliderChangedEventArgs e)
    {
        Plugin.ArrowsList.TryGetValue(GetArrowId(e.Id), out Arrow arrow);
        arrow.Cfg.PlaceDefaultDistance = e.Value;
        if (arrow.GameObject != null)
        {
            arrow.GameObject.GetComponent<Constructable>().placeDefaultDistance = e.Value;
        }
    }

    private void PlaceMaxDistance_OnChanged(object sender, SliderChangedEventArgs e)
    {
        Plugin.ArrowsList.TryGetValue(GetArrowId(e.Id), out Arrow arrow);
        arrow.Cfg.PlaceMaxDistance = e.Value;
        if (arrow.GameObject != null)
        {
            arrow.GameObject.GetComponent<Constructable>().placeMaxDistance = e.Value;
        }
    }

    private void ForceUpright_OnChanged(object sender, ToggleChangedEventArgs e)
    {
        Plugin.ArrowsList.TryGetValue(GetArrowId(e.Id), out Arrow arrow);
        arrow.Cfg.ForceUpright = e.Value;
        if (arrow.GameObject != null)
        {
            arrow.GameObject.GetComponent<Constructable>().forceUpright = e.Value;
        }
    }

    private void Scale_OnChanged(object sender, SliderChangedEventArgs e)
    {
        Plugin.ArrowsList.TryGetValue(GetArrowId(e.Id), out Arrow arrow);

        char axis = e.Id[^1]; // get last character

        switch (axis)
        {
            case 'X':
                arrow.Cfg.Scale = new Vector3(e.Value, arrow.Cfg.Scale.y, arrow.Cfg.Scale.z);
                break;
            case 'Y':
                arrow.Cfg.Scale = new Vector3(arrow.Cfg.Scale.x, e.Value, arrow.Cfg.Scale.z);
                break;
            case 'Z':
                arrow.Cfg.Scale = new Vector3(arrow.Cfg.Scale.x, arrow.Cfg.Scale.y, e.Value);
                break;
            default:
                Plugin.Logger.LogError("Unable to modify scale value.");
                break;
        }

        if (arrow.GameObject != null)
        {
            arrow.GameObject.transform.localScale = arrow.Cfg.Scale;
        }
    }

    private void IsHologramOption_OnChanged(object sender, ToggleChangedEventArgs e)
    {
        Plugin.ArrowsList.TryGetValue(GetArrowId(e.Id), out Arrow arrow);
        arrow.Cfg.IsHologram = e.Value;
        arrow.ApplyHologramOption();
    }

    private void Color_OnChanged(object sender, ColorChangedEventArgs e)
    {
        Plugin.ArrowsList.TryGetValue(GetArrowId(e.Id), out Arrow arrow);
        arrow.Cfg.Color = e.Value;
        arrow.ApplyColorOption();
    }

    private void Intensity_OnChanged(object sender, SliderChangedEventArgs e)
    {
        Plugin.ArrowsList.TryGetValue(GetArrowId(e.Id), out Arrow arrow);
        arrow.Cfg.Intensity = e.Value;
        arrow.ApplyColorOption();
    }
}
