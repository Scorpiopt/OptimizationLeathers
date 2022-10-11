using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace OptimizationLeather
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("OptimizationLeather.Mod");
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(Thing), "SetStuffDirect")]
    public static class Patch_SetStuffDirect
    {
        public static void Prefix(Thing __instance, ref ThingDef newStuff)
        {
            if (Startup.allDisallowedLeathers.Contains(newStuff))
            {
                var chosenLeather = Startup.allowedLeathers.RandomElement();
                Log.Message("[Optimization: Leather] " + __instance + " has a forbidden stuff assigned, changing " + newStuff + " to " + chosenLeather);
                newStuff = chosenLeather;
            }
        }
    }

    [DefOf]
    public static class OL_DefOf
    {
        public static ThingDef Leather_Bird;
        public static ThingDef Leather_Light;
        public static ThingDef Leather_Plain;
        public static ThingDef Leather_Lizard;
        public static ThingDef Leather_Heavy;
        public static ThingDef Leather_Human;
        public static ThingDef Leather_Legend;
        public static ThingDef Leather_Thrumbo;
        public static ThingDef Leather_Chitin;
        public static ThingDef Leather_DragonScale;
        public static StuffCategoryDef Chitin;
    }
    [StaticConstructorOnStartup]
    public static class Startup
    {
        public static HashSet<ThingDef> allowedLeathers = new HashSet<ThingDef>()
        {
             OL_DefOf.Leather_Bird,
             OL_DefOf.Leather_Light,
             OL_DefOf.Leather_Plain,
             OL_DefOf.Leather_Lizard,
             OL_DefOf.Leather_Heavy,
             OL_DefOf.Leather_Human,
             OL_DefOf.Leather_Legend,
             OL_DefOf.Leather_Chitin
        };

        public static HashSet<ThingDef> allDisallowedLeathers = new HashSet<ThingDef>();
        static Startup()
        {
            LeathersOptimizationMod.settings = LoadedModManager.GetMod<LeathersOptimizationMod>().GetSettings<LeathersOptimizationSettings>();
            ApplySettings();
            LoadedModManager.GetMod<LeathersOptimizationMod>().WriteSettings();
        }
        public static void ApplySettings()
        {
            AssignLeathers();
            RemoveLeathers();
            AssignStuff();
            foreach (var animal in LeathersOptimizationMod.settings.animalsByLeathers.Keys.ToList())
            {
                if (animal != null)
                {
                    LeathersOptimizationMod.settings.animalsByLeathers[animal] = animal.race.leatherDef;
                }
            }
        }

        public static Dictionary<string, ThingDef> leathersToConvert = new Dictionary<string, ThingDef>
        {
            {"VFEV_Leather_Fenrir", OL_DefOf.Leather_Legend },
            {"VFEV_Leather_Lothurr", OL_DefOf.Leather_Legend },
            {"VFEV_Leather_Njorun", OL_DefOf.Leather_Legend },
        };

        private static void AssignLeathers()
        {
            bool assignedDragonLeather = false;
            foreach (var thingDef in DefDatabase<ThingDef>.AllDefs)
            {
                if (thingDef.race != null && thingDef.race.Humanlike is false && 
                    (LeathersOptimizationMod.settings.disallowedAnimals.ContainsKey(thingDef) is false 
                    || LeathersOptimizationMod.settings.disallowedAnimals[thingDef] is false))
                {
                    var leatherDef = thingDef.race.leatherDef;
                    if (LeathersOptimizationMod.settings.animalsByLeathers.TryGetValue(thingDef, out var leatherDef2) 
                        && leatherDef2 != null && leatherDef2 != leatherDef)
                    {
                        SwapLeathers(thingDef, leatherDef2);
                        if (leatherDef is null && thingDef.race.Insect)
                        {
                            thingDef.SetStatBaseValue(StatDefOf.LeatherAmount, 40);
                            if (thingDef.butcherProducts != null)
                            {
                                thingDef.butcherProducts.RemoveAll(x => x.thingDef.defName == "VFEI_Chitin");
                            }
                        }
                    }
                    else
                    {
                        if (leatherDef is null)
                        {
                            if (thingDef.race.Insect)
                            {
                                SwapLeathers(thingDef, OL_DefOf.Leather_Chitin);
                                thingDef.SetStatBaseValue(StatDefOf.LeatherAmount, 40);
                                if (thingDef.butcherProducts != null)
                                {
                                    thingDef.butcherProducts.RemoveAll(x => x.thingDef.defName == "VFEI_Chitin");
                                }
                            }
                        }
                        else if (leatherDef != null && (!allowedLeathers.Contains(leatherDef) 
                            || leatherDef != GetDefaultLeather(thingDef)))
                        {
                            if (!allowedLeathers.Contains(leatherDef))
                            {
                                allDisallowedLeathers.Add(leatherDef);
                            }
                            if (thingDef.race.leatherDef != null 
                                && leathersToConvert.TryGetValue(thingDef.race.leatherDef.defName, out var newLeather))
                            {
                                SwapLeathers(thingDef, newLeather);
                            }
                            else if (thingDef.IsDragon())
                            {
                                SwapLeathers(thingDef, OL_DefOf.Leather_DragonScale);
                                assignedDragonLeather = true;
                            }
                            else if (thingDef.race.Insect)
                            {
                                SwapLeathers(thingDef, OL_DefOf.Leather_Chitin);
                                if (thingDef.butcherProducts != null)
                                {
                                    thingDef.butcherProducts.RemoveAll(x => x.thingDef.defName == "VFEI_Chitin");
                                }
                            }
                            else if (thingDef.race.leatherDef == OL_DefOf.Leather_Thrumbo)
                            {
                                SwapLeathers(thingDef, OL_DefOf.Leather_Legend);
                            }
                            else if (thingDef.race.baseBodySize >= 1f)
                            {
                                SwapLeathers(thingDef, OL_DefOf.Leather_Heavy);
                            }
                            else if (thingDef.race.baseBodySize >= 0.5f)
                            {
                                SwapLeathers(thingDef, OL_DefOf.Leather_Plain);
                            }
                            else
                            {
                                SwapLeathers(thingDef, OL_DefOf.Leather_Light);
                            }
                        }
                    }
                }
            }
            if (!assignedDragonLeather)
            {
                allDisallowedLeathers.Add(OL_DefOf.Leather_DragonScale);
            }
        }

        public static List<string> moddedDragonLeathers = new List<string>
        {
            "Dragon_Leather",
            "Rare_Dragon_Leather",
            "True_Dragon_Leather",
            "Leather_DragonScale"
        };

        public static List<string> moddedDragonBodies = new List<string>
        {
            "GS_Dragon",
            "GS_Dragon_Triple_Stryke",
            "RttR_WingedQuadrupedAnimalWithPawsAndTail",
            "RttR_QuadWingedQuadrupedAnimalWithPawsAndTail",
            "RttR_WingedQuadrupedAnimalThreeTails"
        };
        public static bool IsDragon(this ThingDef thingDef)
        {
            if (thingDef.race.leatherDef != null && moddedDragonLeathers.Contains(thingDef.race.leatherDef.defName))
            {
                return true;
            }
            else if (thingDef.race.body != null && moddedDragonBodies.Contains(thingDef.race.body.defName))
            {
                return true;
            }
            return false;
        }
        public static ThingDef GetDefaultLeather(ThingDef thingDef)
        {
            if (thingDef.race.Humanlike)
            {
                return thingDef.race.leatherDef;
            }
            else if (LeathersOptimizationMod.settings.disallowedAnimals.ContainsKey(thingDef) 
                && LeathersOptimizationMod.settings.disallowedAnimals[thingDef])
            {
                return thingDef.race.leatherDef;
            }
            else if (thingDef.race.leatherDef != null && leathersToConvert.TryGetValue(thingDef.race.leatherDef.defName, out var newLeather))
            {
                return newLeather;
            }
            else if (thingDef.IsDragon())
            {
                return OL_DefOf.Leather_DragonScale;
            }
            else if (thingDef.race.Insect)
            {
                return OL_DefOf.Leather_Chitin;
            }
            else if (thingDef.race.leatherDef == OL_DefOf.Leather_Thrumbo || thingDef.race.leatherDef == OL_DefOf.Leather_Legend)
            {
                return OL_DefOf.Leather_Legend;
            }
            else if (thingDef.race.baseBodySize >= 1f)
            {
                return OL_DefOf.Leather_Heavy;
            }
            else if (thingDef.race.baseBodySize >= 0.5f)
            {
                return OL_DefOf.Leather_Plain;
            }
            else
            {
                return OL_DefOf.Leather_Light;
            }
        }

        private static void SwapLeathers(ThingDef animal, ThingDef newLeather)
        {
            var oldLeather = animal.race.leatherDef;
            animal.race.leatherDef = newLeather;
            var compShearable = animal.GetCompProperties<CompProperties_Shearable>();
            if (compShearable != null && compShearable.woolDef == oldLeather)
            {
                compShearable.woolDef = newLeather;
            }
            var compScaleable = animal.comps.FirstOrDefault(x => x.compClass.Name == "CompScaleable");
            if (compScaleable != null)
            {
                Traverse.Create(compScaleable).Field("scaleDef").SetValue(newLeather);
            }

            if (!LeathersOptimizationMod.settings.animalsByLeathers.ContainsKey(animal))
            {
                LeathersOptimizationMod.settings.animalsByLeathers[animal] = newLeather;
            }
        }

        private static void RemoveLeathers()
        {
            foreach (var thingDef in allDisallowedLeathers)
            {
                DefDatabase<ThingDef>.Remove(thingDef);
                if (thingDef.thingCategories != null)
                {
                    foreach (var category in thingDef.thingCategories)
                    {
                        category.childThingDefs.Remove(thingDef);
                    }
                }
            }
            PawnApparelGenerator.allApparelPairs.RemoveAll(x => allDisallowedLeathers.Contains(x.stuff));
        }
        private static bool UsedInRecipe(this ThingDef leatherDef)
        {
            foreach (var recipe in DefDatabase<RecipeDef>.AllDefs)
            {
                if (recipe.ingredients?.Any(x => x.filter.thingDefs?.Contains(leatherDef) ?? false) ?? false)
                {
                    return true;
                }
                if (recipe.products != null && recipe.products.Any(x => x.thingDef == leatherDef))
                {
                    return true;
                }
            }
            return false;
        }

        public static void AssignStuff()
        {
            foreach (var thingDef in DefDatabase<ThingDef>.AllDefs)
            {
                if (thingDef.stuffCategories != null && thingDef.stuffCategories.Where(x => x == StuffCategoryDefOf.Metallic 
                || x == StuffCategoryDefOf.Woody 
                || x == StuffCategoryDefOf.Stony).Count() == 3)
                {
                    thingDef.stuffCategories.Add(OL_DefOf.Chitin);
                }
            }
        }
    }

    public class LeathersOptimizationMod : Mod
    {
        public static LeathersOptimizationSettings settings;
        public LeathersOptimizationMod(ModContentPack pack) : base(pack)
        {

        }
        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            settings.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return this.Content.Name;
        }

        public override void WriteSettings()
        {
            Startup.ApplySettings();
            base.WriteSettings();
        }
    }

    public class LeathersOptimizationSettings : ModSettings
    {
        public Dictionary<ThingDef, bool> disallowedAnimals = new Dictionary<ThingDef, bool>();
        public Dictionary<ThingDef, ThingDef> animalsByLeathers = new Dictionary<ThingDef, ThingDef>();
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref animalsByLeathers, "animalsByLeathers", LookMode.Def, LookMode.Def);
            Scribe_Collections.Look(ref disallowedAnimals, "disallowedAnimals", LookMode.Def, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (animalsByLeathers is null)
                {
                    animalsByLeathers = new Dictionary<ThingDef, ThingDef>();
                }
                if (disallowedAnimals is null)
                {
                    disallowedAnimals = new Dictionary<ThingDef, bool>();
                }
            }
        }
        private Vector2 scrollPosition;
        int scrollHeightCount = 0;
        public void DoSettingsWindowContents(Rect inRect)
        {
            var defs = DefDatabase<ThingDef>.AllDefs.Where(x => x.race != null && x.race.Humanlike is false 
                && x.race.leatherDef != null && animalsByLeathers.ContainsKey(x)).ToList();
            var outRect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height - 50);
            var viewRect = new Rect(outRect.x, outRect.y, outRect.width - 30, scrollHeightCount);
            scrollHeightCount = 0;
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            var pos = outRect.y;
            for (var num = 0; num < defs.Count; num++)
            {
                var thingDef = defs[num];
                scrollHeightCount += 24;
                var labelRect = new Rect(outRect.x, pos + (num * 24), inRect.width - 150 - 30 - 30, 24);
                Widgets.Label(labelRect, thingDef.LabelCap);
                var selectorRect = new Rect(labelRect.xMax, labelRect.y, 150, 24);
                if (Widgets.ButtonText(selectorRect, animalsByLeathers[thingDef].LabelCap))
                {
                    List<FloatMenuOption> floatMenuOptions = new List<FloatMenuOption>();
                    foreach (var leather in Startup.allowedLeathers)
                    {
                        floatMenuOptions.Add(new FloatMenuOption(leather.LabelCap, delegate
                        {
                            animalsByLeathers[thingDef] = leather;
                        }));
                    }
                    Find.WindowStack.Add(new FloatMenu(floatMenuOptions));
                }
                var enabled = disallowedAnimals.ContainsKey(thingDef) is false || disallowedAnimals[thingDef] is false;
                Widgets.Checkbox(new Vector2(selectorRect.xMax + 6, selectorRect.y), ref enabled);
                if (enabled)
                {
                    disallowedAnimals[thingDef] = false;
                }
                else
                {
                    disallowedAnimals[thingDef] = true;
                }
            }

            Widgets.EndScrollView();

            var resetButton = new Rect(outRect.width / 2f - Window.CloseButSize.x / 2f, outRect.yMax + 15, Window.CloseButSize.x, Window.CloseButSize.y);
            if (Widgets.ButtonText(resetButton, "Reset".Translate()))
            {
                animalsByLeathers.Clear();
                foreach (var def in defs)
                {
                    animalsByLeathers[def] = Startup.GetDefaultLeather(def);
                }
            }
        }
    }
}
