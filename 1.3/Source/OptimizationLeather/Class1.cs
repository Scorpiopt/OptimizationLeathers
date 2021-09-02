using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace OptimizationLeather
{
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

        private static HashSet<ThingDef> allDisallowedLeathers = new HashSet<ThingDef>();
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
                        allDisallowedLeathers.Add(thingDef.race.leatherDef);
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
