using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        };

        public static HashSet<ThingDef> allDisallowedLeathers = new HashSet<ThingDef>();
        static Startup()
        {
            AssignLeathers();
            RemoveLeathers();
        }

        private static void AssignLeathers()
        {
            foreach (var thingDef in DefDatabase<ThingDef>.AllDefs)
            {
                if (thingDef.race != null)
                {
                    var leatherDef = thingDef.race?.leatherDef;
                    if (leatherDef != null && !allowedLeathers.Contains(leatherDef) && !leatherDef.UsedInRecipe())
                    {
                        allDisallowedLeathers.Add(leatherDef);
                        if (thingDef.race.baseBodySize >= 1f)
                        {
                            thingDef.race.leatherDef = OL_DefOf.Leather_Heavy;
                        }
                        else if (thingDef.race.baseBodySize >= 0.5f)
                        {
                            thingDef.race.leatherDef = OL_DefOf.Leather_Plain;
                        }
                        else
                        {
                            thingDef.race.leatherDef = OL_DefOf.Leather_Light;
                        }
                    }
                }
            }
        }

        private static void RemoveLeathers()
        {
            foreach (var thingDef in allDisallowedLeathers)
            {
                DefDatabase<ThingDef>.Remove(thingDef);
                ThingCategoryDefOf.Leathers.childThingDefs.Remove(thingDef);
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
    }
}
