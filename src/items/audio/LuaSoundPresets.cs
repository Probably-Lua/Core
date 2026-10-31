using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    internal static class LuaSoundPresets
    {
        private static readonly Dictionary<string, Action<GameItem>> Presets = new(StringComparer.Ordinal)
        {
            ["pill"] = AudioHelper.InitPillAudio,
            ["injector"] = AudioHelper.InitInjectorAudio,
            ["pill_bottle"] = AudioHelper.InitPillBottleAudio,
            ["backpack"] = AudioHelper.InitBackpack,
            ["case"] = AudioHelper.InitCase,
            ["pouch"] = AudioHelper.InitPouch,
            ["edible"] = AudioHelper.InitEdibleAudio,
            ["food"] = AudioHelper.InitFoodAudio,
            ["drinkable"] = AudioHelper.InitDrinkableAudio,
            ["packaged"] = AudioHelper.InitPackagedAudio,
            ["plastic_bottle"] = AudioHelper.InitPlasticBottle,
            ["canned_food"] = AudioHelper.InitCannedFood,
            ["aluminum_can"] = AudioHelper.InitAluminumCan,
            ["rat"] = AudioHelper.InitRat,
            ["blunt_weapon"] = AudioHelper.InitBluntWeapon,
            ["sharp_weapon"] = AudioHelper.InitSharpWeapon,
            ["shotgun"] = AudioHelper.InitShotgun,
            ["smg"] = AudioHelper.InitSMG,
            ["ump"] = AudioHelper.InitUMP,
            ["pistol"] = AudioHelper.InitPistol,
            ["door"] = AudioHelper.InitDoor,
            ["dossier"] = AudioHelper.InitDossier,
            ["armour"] = AudioHelper.InitArmour,
            ["bandage"] = AudioHelper.InitBandage,
            ["grenade"] = AudioHelper.InitGrenade,
            ["equipment"] = AudioHelper.InitEquipment,
            ["battery"] = AudioHelper.InitBattery,
            ["blade"] = AudioHelper.InitBlade,
            ["book"] = AudioHelper.InitBook,
            ["carton_box"] = AudioHelper.InitCartonBox,
            ["carton_card"] = AudioHelper.InitCartonCard,
            ["glass"] = AudioHelper.InitGlass,
            ["metal_machine"] = AudioHelper.InitMetalMachine,
            ["module"] = AudioHelper.InitModule,
            ["paper_sheet"] = AudioHelper.InitPaperSheet,
            ["plastic_card"] = AudioHelper.InitPlasticCard,
            ["plastic_container"] = AudioHelper.InitPlasticContainer,
            ["plastic_vial"] = AudioHelper.InitPlasticVial,
            ["scanner"] = AudioHelper.InitScanner,
            ["solid_metal"] = AudioHelper.InitSolidMetal,
            ["cassette"] = AudioHelper.InitCassette,
            ["tiny"] = AudioHelper.InitTinyItem,
            ["tiny_metal"] = AudioHelper.InitTinyMetal,
            ["glass_vial"] = AudioHelper.InitGlassVial,
            ["small_machine"] = AudioHelper.InitSmallMachine,
            ["small_module"] = AudioHelper.InitSmallModule,
            ["leafy"] = AudioHelper.InitLeafy,
            ["produce"] = AudioHelper.InitProduce,
            ["small_gun"] = AudioHelper.InitSmallGun,
            ["gun"] = AudioHelper.InitGun,
            ["heavy_gun"] = AudioHelper.InitHeavyGun,
            ["meaty"] = AudioHelper.InitMeaty,
            ["gun_mod"] = AudioHelper.InitGunMod,
            ["plastic_machinery"] = AudioHelper.InitPlasticMachinery,
            ["soft_plastic"] = AudioHelper.InitSoftPlasticItem,
            ["scrap_metal"] = AudioHelper.InitScrapMetal,
            ["water_filter"] = AudioHelper.InitWaterFilter,
            ["empty_plastic_bottle"] = AudioHelper.InitEmptyPlasticBottle,
            ["full_plastic_bottle"] = AudioHelper.InitFullPlasticBottle,
            ["textile"] = AudioHelper.InitTextile,
            ["trashcan"] = AudioHelper.InitTrashcan,
        };

        internal static Action<GameItem>? Require(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            if (Presets.TryGetValue(name, out var apply))
                return apply;
                
            throw new ScriptRuntimeException($"Unknown sound preset '{name}'. Use SoundPreset.<name>.");
        }

        internal static void Bind(Script script)
        {
            var constants = new Table(script);
            constants.Set("Pill", DynValue.NewString("pill"));
            constants.Set("Injector", DynValue.NewString("injector"));
            constants.Set("PillBottle", DynValue.NewString("pill_bottle"));
            constants.Set("Backpack", DynValue.NewString("backpack"));
            constants.Set("Case", DynValue.NewString("case"));
            constants.Set("Pouch", DynValue.NewString("pouch"));
            constants.Set("Edible", DynValue.NewString("edible"));
            constants.Set("Food", DynValue.NewString("food"));
            constants.Set("Drinkable", DynValue.NewString("drinkable"));
            constants.Set("Packaged", DynValue.NewString("packaged"));
            constants.Set("PlasticBottle", DynValue.NewString("plastic_bottle"));
            constants.Set("CannedFood", DynValue.NewString("canned_food"));
            constants.Set("AluminumCan", DynValue.NewString("aluminum_can"));
            constants.Set("Rat", DynValue.NewString("rat"));
            constants.Set("BluntWeapon", DynValue.NewString("blunt_weapon"));
            constants.Set("SharpWeapon", DynValue.NewString("sharp_weapon"));
            constants.Set("Shotgun", DynValue.NewString("shotgun"));
            constants.Set("SMG", DynValue.NewString("smg"));
            constants.Set("UMP", DynValue.NewString("ump"));
            constants.Set("Pistol", DynValue.NewString("pistol"));
            constants.Set("Door", DynValue.NewString("door"));
            constants.Set("Dossier", DynValue.NewString("dossier"));
            constants.Set("Armour", DynValue.NewString("armour"));
            constants.Set("Bandage", DynValue.NewString("bandage"));
            constants.Set("Grenade", DynValue.NewString("grenade"));
            constants.Set("Equipment", DynValue.NewString("equipment"));
            constants.Set("Battery", DynValue.NewString("battery"));
            constants.Set("Blade", DynValue.NewString("blade"));
            constants.Set("Book", DynValue.NewString("book"));
            constants.Set("CartonBox", DynValue.NewString("carton_box"));
            constants.Set("CartonCard", DynValue.NewString("carton_card"));
            constants.Set("Glass", DynValue.NewString("glass"));
            constants.Set("MetalMachine", DynValue.NewString("metal_machine"));
            constants.Set("Module", DynValue.NewString("module"));
            constants.Set("PaperSheet", DynValue.NewString("paper_sheet"));
            constants.Set("PlasticCard", DynValue.NewString("plastic_card"));
            constants.Set("PlasticContainer", DynValue.NewString("plastic_container"));
            constants.Set("PlasticVial", DynValue.NewString("plastic_vial"));
            constants.Set("Scanner", DynValue.NewString("scanner"));
            constants.Set("SolidMetal", DynValue.NewString("solid_metal"));
            constants.Set("Cassette", DynValue.NewString("cassette"));
            constants.Set("Tiny", DynValue.NewString("tiny"));
            constants.Set("TinyMetal", DynValue.NewString("tiny_metal"));
            constants.Set("GlassVial", DynValue.NewString("glass_vial"));
            constants.Set("SmallMachine", DynValue.NewString("small_machine"));
            constants.Set("SmallModule", DynValue.NewString("small_module"));
            constants.Set("Leafy", DynValue.NewString("leafy"));
            constants.Set("Produce", DynValue.NewString("produce"));
            constants.Set("SmallGun", DynValue.NewString("small_gun"));
            constants.Set("Gun", DynValue.NewString("gun"));
            constants.Set("HeavyGun", DynValue.NewString("heavy_gun"));
            constants.Set("Meaty", DynValue.NewString("meaty"));
            constants.Set("GunMod", DynValue.NewString("gun_mod"));
            constants.Set("PlasticMachinery", DynValue.NewString("plastic_machinery"));
            constants.Set("SoftPlastic", DynValue.NewString("soft_plastic"));
            constants.Set("ScrapMetal", DynValue.NewString("scrap_metal"));
            constants.Set("WaterFilter", DynValue.NewString("water_filter"));
            constants.Set("EmptyPlasticBottle", DynValue.NewString("empty_plastic_bottle"));
            constants.Set("FullPlasticBottle", DynValue.NewString("full_plastic_bottle"));
            constants.Set("Textile", DynValue.NewString("textile"));
            constants.Set("Trashcan", DynValue.NewString("trashcan"));
            script.Globals.Set("SoundPreset", DynValue.NewTable(constants));
        }
    }
}
