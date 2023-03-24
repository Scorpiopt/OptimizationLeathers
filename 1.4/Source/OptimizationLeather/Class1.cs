using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace OptimizationLeather
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            Harmony harmony = new Harmony("OptimizationLeather.Mod");
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(Thing), "SetStuffDirect")]
    public static class Patch_SetStuffDirect
    {
        public static void Prefix(Thing __instance, ref ThingDef newStuff)
        {
            if (newStuff != null && Startup.allDisallowedLeathers.Contains(newStuff))
            {
                ThingDef chosenLeather = Startup.allowedLeathers.RandomElement();
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
        public static bool debug = false;
        public static HashSet<ThingDef> allowedLeathers = new HashSet<ThingDef>()
        {
             OL_DefOf.Leather_Bird,
             OL_DefOf.Leather_Light,
             OL_DefOf.Leather_Plain,
             OL_DefOf.Leather_Lizard,
             OL_DefOf.Leather_Heavy,
             OL_DefOf.Leather_Human,
             OL_DefOf.Leather_Legend,
             OL_DefOf.Leather_Chitin,
             OL_DefOf.Leather_DragonScale,
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
        }

        public static Dictionary<string, ThingDef> leathersToConvert = new Dictionary<string, ThingDef>
        {
            {"VFEV_Leather_Fenrir", OL_DefOf.Leather_Legend },
            {"VFEV_Leather_Lothurr", OL_DefOf.Leather_Legend },
            {"VFEV_Leather_Njorun", OL_DefOf.Leather_Legend },
        };

        public static void Message(string message)
        {
            if (debug)
            {
                Log.Message(message);
            }
        }
        private static void AssignLeathers()
        {
            foreach (ThingDef animal in DefDatabase<ThingDef>.AllDefs)
            {
                if (animal.race != null && animal.race.Humanlike is false && animal.race.IsFlesh)
                {
                    if (LeathersOptimizationMod.settings.disallowedAnimals.Contains(animal) is false)
                    {
                        ThingDef leatherDef = animal.race.leatherDef;
                        Message(animal + " - existing leather: " + animal.race.leatherDef + " - default: " + GetDefaultLeather(animal));
                        if (LeathersOptimizationMod.settings.animalsByLeathers.TryGetValue(animal, out ThingDef leatherDef2))
                        {
                            if (leatherDef2 != null && leatherDef2 != leatherDef)
                            {
                                SwapLeathers(animal, leatherDef2);
                            }
                        }
                        else
                        {
                            if (leatherDef is null)
                            {
                                if (animal.race.Insect)
                                {
                                    SwapLeathers(animal, OL_DefOf.Leather_Chitin);
                                    if (animal.butcherProducts != null)
                                    {
                                        animal.butcherProducts.RemoveAll(x => x.thingDef.defName == "VFEI_Chitin");
                                    }
                                }
                            }
                            else
                            {
                                var defaultLeather = GetDefaultLeather(animal);
                                if (defaultLeather != leatherDef)
                                {
                                    SwapLeathers(animal, defaultLeather);
                                    if (animal.race.Insect)
                                    {
                                        if (animal.butcherProducts != null)
                                        {
                                            animal.butcherProducts.RemoveAll(x => x.thingDef.defName == "VFEI_Chitin");
                                        }
                                    }
                                }
                            }
                        }

                        if (LeathersOptimizationMod.settings.animalsByLeathers.TryGetValue(animal, out var newLeather) is false)
                        {
                            LeathersOptimizationMod.settings.animalsByLeathers[animal] = animal.race.leatherDef;
                        }
                    }
                    else
                    {
                        LeathersOptimizationMod.settings.animalsByLeathers[animal] = animal.race.leatherDef;
                    }
                }
            }

            if (DefDatabase<ThingDef>.AllDefs.Any(x => x.race?.leatherDef == OL_DefOf.Leather_DragonScale) is false)
            {
                allDisallowedLeathers.Add(OL_DefOf.Leather_DragonScale);
            }
        }

        public static List<string> birdBodies = new List<string>
        {
            "Bird"
        };
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
            if (LeathersOptimizationMod.settings.disallowedAnimals.Contains(thingDef))
            {
                return thingDef.race.leatherDef;
            }
            else if (birdBodies.Contains(thingDef.race.body.defName))
            {
                return OL_DefOf.Leather_Bird;
            }
            else if (thingDef.race.Insect)
            {
                return OL_DefOf.Leather_Chitin;
            }
            else if (thingDef.race.Humanlike || thingDef.race.IsFlesh is false || thingDef.race.leatherDef is null 
                || thingDef.race.leatherDef.IsLeather is false)
            {
                return thingDef.race.leatherDef;
            }
            else if (thingDef.race.leatherDef != null && leathersToConvert.TryGetValue(thingDef.race.leatherDef.defName, out ThingDef newLeather))
            {
                return newLeather;
            }
            else if (thingDef.IsDragon())
            {
                return OL_DefOf.Leather_DragonScale;
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
            ThingDef oldLeather = animal.race.leatherDef;
            Message(animal + " swapped from " + oldLeather + " to " + newLeather);
            animal.race.leatherDef = newLeather;
            if (newLeather != null && animal.GetStatValueAbstract(StatDefOf.LeatherAmount) <= 0)
            {
                animal.SetStatBaseValue(StatDefOf.LeatherAmount, 40);
            }
            else if (newLeather is null)
            {
                animal.SetStatBaseValue(StatDefOf.LeatherAmount, 0);
            }
            LeathersOptimizationMod.settings.animalsByLeathers[animal] = animal.race.leatherDef;
            CompProperties_Shearable compShearable = animal.GetCompProperties<CompProperties_Shearable>();
            if (oldLeather != null && compShearable != null && compShearable.woolDef == oldLeather)
            {
                compShearable.woolDef = newLeather;
            }
            CompProperties compScaleable = animal.comps.FirstOrDefault(x => x.compClass.Name == "CompScaleable");
            if (compScaleable != null)
            {
                Traverse.Create(compScaleable).Field("scaleDef").SetValue(newLeather);
            }

            if (oldLeather != null && !allDisallowedLeathers.Contains(oldLeather) && !allowedLeathers.Contains(oldLeather))
            {
                if (oldLeather.IsLeather)
                {
                    Message("Disallowed: " + oldLeather);
                    allDisallowedLeathers.Add(oldLeather);
                }
            }
        }

        private static void RemoveLeathers()
        {
            foreach (ThingDef thingDef in allDisallowedLeathers)
            {
                if (thingDef != null)
                {
                    DefDatabase<ThingDef>.Remove(thingDef);
                    if (thingDef.thingCategories != null)
                    {
                        foreach (ThingCategoryDef category in thingDef.thingCategories)
                        {
                            category.childThingDefs.Remove(thingDef);
                        }
                    }
                }
            }
            PawnApparelGenerator.allApparelPairs.RemoveAll(x => x.stuff != null && allDisallowedLeathers.Contains(x.stuff));
        }
        private static bool UsedInRecipe(this ThingDef leatherDef)
        {
            foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefs)
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
            foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefs)
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
            return Content.Name;
        }

        public override void WriteSettings()
        {
            Startup.ApplySettings();
            base.WriteSettings();
        }
    }

    public class LeathersOptimizationSettings : ModSettings
    {
        public HashSet<ThingDef> disallowedAnimals = new HashSet<ThingDef>();
        public Dictionary<ThingDef, ThingDef> animalsByLeathers = new Dictionary<ThingDef, ThingDef>();
        public override void ExposeData()
        {
            base.ExposeData();
            if (animalsByLeathers != null)
                animalsByLeathers.RemoveAll(x => x.Key is null || x.Value is null);

            Scribe_Collections.Look(ref animalsByLeathers, "animalsByLeathers", LookMode.Def, LookMode.Def);
            Scribe_Collections.Look(ref disallowedAnimals, "disallowedAnimals", LookMode.Def);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (animalsByLeathers is null)
                {
                    animalsByLeathers = new Dictionary<ThingDef, ThingDef>();
                }
                if (disallowedAnimals is null)
                {
                    disallowedAnimals = new HashSet<ThingDef>();
                }
            }
        }
        private Vector2 scrollPosition;
        private int scrollHeightCount = 0;
        public void DoSettingsWindowContents(Rect inRect)
        {
            List<ThingDef> defs = DefDatabase<ThingDef>.AllDefs.Where(x => x.race != null && x.race.Humanlike is false 
            && x.race.Dryad is false && x.race.IsFlesh).ToList();
            Rect outRect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height - 50);
            Rect viewRect = new Rect(outRect.x, outRect.y, outRect.width - 30, scrollHeightCount);
            scrollHeightCount = 0;
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            float pos = outRect.y;
            for (int num = 0; num < defs.Count; num++)
            {
                ThingDef thingDef = defs[num];
                scrollHeightCount += 24;
                Rect labelRect = new Rect(outRect.x, pos + (num * 24), inRect.width - 150 - 30 - 30, 24);
                Widgets.Label(labelRect, thingDef.LabelCap);
                Rect selectorRect = new Rect(labelRect.xMax, labelRect.y, 150, 24);
                var leatherLabel = animalsByLeathers.ContainsKey(thingDef) ? 
                    animalsByLeathers[thingDef]?.LabelCap ?? "None".Translate() 
                    : thingDef.race.leatherDef != null ? thingDef.race.leatherDef.LabelCap : "None".Translate();
                if (Widgets.ButtonText(selectorRect, leatherLabel))
                {
                    List<FloatMenuOption> floatMenuOptions = new List<FloatMenuOption>();
                    floatMenuOptions.Add(new FloatMenuOption("None".Translate(), delegate
                    {
                        animalsByLeathers[thingDef] = null;
                    }));
                    foreach (ThingDef leather in Startup.allowedLeathers)
                    {
                        floatMenuOptions.Add(new FloatMenuOption(leather.LabelCap, delegate
                        {
                            animalsByLeathers[thingDef] = leather;
                        }));
                    }
                    Find.WindowStack.Add(new FloatMenu(floatMenuOptions));
                }
                bool enabled = disallowedAnimals.Contains(thingDef) is false;
                Widgets.Checkbox(new Vector2(selectorRect.xMax + 6, selectorRect.y), ref enabled);
                if (enabled)
                {
                    disallowedAnimals.Remove(thingDef);
                }
                else
                {
                    disallowedAnimals.Add(thingDef);
                }
            }

            Widgets.EndScrollView();

            Rect resetButton = new Rect((outRect.width / 2f) - (Window.CloseButSize.x / 2f), outRect.yMax + 15, Window.CloseButSize.x, Window.CloseButSize.y);
            if (Widgets.ButtonText(resetButton, "Reset".Translate()))
            {
                disallowedAnimals.Clear();
                animalsByLeathers.Clear();
                foreach (ThingDef def in defs)
                {
                    var leather = Startup.GetDefaultLeather(def);
                    if (leather != null)
                    {
                        animalsByLeathers[def] = leather;
                    }
                }
            }
        }
    }
}
