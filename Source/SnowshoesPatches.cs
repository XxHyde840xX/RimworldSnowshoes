using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using System.Reflection.Emit;
using HarmonyLib;

namespace Snowshoes
{
    [StaticConstructorOnStartup]
    public static class SnowshoesHarmonyPatcher
    {
        static SnowshoesHarmonyPatcher()
        {
            Harmony Harmony = new HarmonyLib.Harmony("udal.rimworld.snowshoes");
            Harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(Pawn_PathFollower), "CostToMoveIntoCell", new[] { typeof(Pawn), typeof(IntVec3) })]
    public static class Patch_PathFollower_CostToMoveIntoCell
    {
        /**
         * Use Transpiler on Pawn_PathFollower.CostToMoveIntoCell()
         * to replace:
         *   pawn.Map.pathGrid.CalculatedCostAt(c, false, pawn.Position)
         * with:
         *   Pawn_PathFollower_CalculatedCostAt.PathGrid_CalculatedCostAt(pawn, c)
         */
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> code = instructions.ToList();
            int foundInstruction = -1;
            for (int i = 0; i < code.Count; i++)
            {
                if (code[i].opcode == OpCodes.Callvirt &&
                    (code[i].operand as MethodInfo).Name == "CalculatedCostAt")
                {
                    // Found Verse.AI.PathGrid::CalculatedCostAt() call
                    foundInstruction = i;
                    break;
                }
            }

            // Clear unnecessaty instructions preceding the call (arguments)
            code.RemoveRange(foundInstruction - 7, 8);

            CodeInstruction[] collection = new CodeInstruction[] {
                // Load pawn into stack (argument 0)
                new CodeInstruction(OpCodes.Ldarg_0, null),
                // Load c into stack (argument 1)
                new CodeInstruction(OpCodes.Ldarg_1, null),
                // Call our own CalculatedCostAt (Patch_PathFollower_CostToMoveIntoCell.PathGrid_CalculatedCostAt())
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch_PathFollower_CostToMoveIntoCell), nameof(PathGrid_CalculatedCostAt)))
            };
            // Insert into IL
            code.InsertRange(foundInstruction - 7, collection);

            // Debug Result IL
            if (false)
            {
                Log.Message("--- SnowshoesHarmonyPatcher IL Result ---");
                string debug = "";
                for (int j = 0; j < code.Count; j++)
                {
                    debug += code[j].ToString() + "\r\n";
                }
                Log.Message(debug);
                Log.Message("--- ---");
            }

            return code.AsEnumerable();
        }

        // Modified PathGrid.CalculatedCostAt()
        public static int PathGrid_CalculatedCostAt(Pawn Pawn, IntVec3 c)
        {
            IntVec3 prevCell = Pawn.Position;

            bool flag = false;
            TerrainDef terrainDef = Pawn.Map.terrainGrid.TerrainAt(c);
            if (terrainDef == null || terrainDef.passability == Traversability.Impassable)
            {
                return 10000;
            }
            int num = terrainDef.pathCost;
            List<Thing> list = Pawn.Map.thingGrid.ThingsListAt(c);
            for (int i = 0; i < list.Count; i++)
            {
                Thing thing = list[i];
                if (thing.def.passability == Traversability.Impassable)
                {
                    return 10000;
                }
                if (!PathGrid_IsPathCostIgnoreRepeater(thing.def) || !prevCell.IsValid || !PathGrid_ContainsPathCostIgnoreRepeater(Pawn.Map, prevCell))
                {
                    int pathCost = thing.def.pathCost;
                    if (pathCost > num)
                    {
                        num = pathCost;
                    }
                }
                if (thing is Building_Door && prevCell.IsValid)
                {
                    Building edifice = prevCell.GetEdifice(Pawn.Map);
                    if (edifice != null && edifice is Building_Door)
                    {
                        flag = true;
                    }
                }
            }
            int num2 = SnowUtility.MovementTicksAddOn(Pawn.Map.snowGrid.GetCategory(c));
            if (num2 > num)
            {
                num = num2;
            }
            if (flag)
            {
                num += 45;
            }
            return num;
        }

        // Extracted private method PathGrid.IsPathCostIgnoreRepeater()
        public static bool PathGrid_IsPathCostIgnoreRepeater(ThingDef def)
        {
            return def.pathCost >= 25 && def.pathCostIgnoreRepeat;
        }

        // Extracted private method PathGrid.ContainsPathCostIgnoreRepeater()
        public static bool PathGrid_ContainsPathCostIgnoreRepeater(Map map, IntVec3 c)
        {
            List<Thing> list = map.thingGrid.ThingsListAt(c);
            for (int i = 0; i < list.Count; i++)
            {
                if (PathGrid_IsPathCostIgnoreRepeater(list[i].def))
                {
                    return true;
                }
            }
            return false;
        }
    }
}