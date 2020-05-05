using System;
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
            bool debug = false;
            if (debug) Log.Message("Patch_PathFollower_CostToMoveIntoCell - Transpiler");

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

            if(foundInstruction != -1)
            {
                if (debug) Log.Message("Patch_PathFollower_CostToMoveIntoCell - Found Instruction");

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
                if (debug)
                {
                    Log.Message("--- SnowshoesHarmonyPatcher IL Result ---");
                    string debugILMsg = "";
                    for (int j = 0; j < code.Count; j++)
                    {
                        debugILMsg += code[j].ToString() + "\r\n";
                    }
                    Log.Message(debugILMsg);
                    Log.Message("--- ---");
                }
            }

            return code.AsEnumerable();
        }

        // Modified PathGrid.CalculatedCostAt()
        public static int PathGrid_CalculatedCostAt(Pawn Pawn, IntVec3 c)
        {
            IntVec3 prevCell = Pawn.Position;

            // Modified beahaviour:
            // - Apply CountersIcePenalty and CountersSnowPenalty stats
            // - When snow it's medium or thicker, terrain does not apply anymore.

            TerrainDef terrainDef = Pawn.Map.terrainGrid.TerrainAt(c);
            if (terrainDef == null || terrainDef.passability == Traversability.Impassable)
            {
                return 10000;
            }

            // Get terrain path cost
            int pcTerrain = terrainDef.pathCost;
            if (terrainDef == TerrainDefOf.Ice)
            {
                // Apply counter ice penalty
                pcTerrain = (int)Math.Ceiling((double)((float)pcTerrain * (1f - Pawn.GetStatValue(StatDef.Named("CountersIcePenalty"), true))));
            }
            // Get snow path cost
            int pcSnow = SnowUtility.MovementTicksAddOn(Pawn.Map.snowGrid.GetCategory(c));
            // Apply counter snow penalty
            pcSnow = (int)Math.Ceiling((double)((float)pcSnow * (1f - Pawn.GetStatValue(StatDef.Named("CountersSnowPenalty"), true))));

            int pc = 0;
            if (Pawn.Map.snowGrid.GetCategory(c) >= SnowCategory.Medium)
            {
                // Snow is thick, we don't consider terrain path cost
                pc = pcSnow;   
            }
            else
            {
                // Snow is thin, we apply the highest path cost
                pc = pcTerrain > pcSnow ? pcTerrain : pcSnow;
            }

            bool flagDoor = false;
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
                    int pcThing = thing.def.pathCost;
                    if (pcThing > pc)
                    {
                        pc = pcThing;
                    }
                }
                if (thing is Building_Door && prevCell.IsValid)
                {
                    Building edifice = prevCell.GetEdifice(Pawn.Map);
                    if (edifice != null && edifice is Building_Door)
                    {
                        flagDoor = true;
                    }
                }
            }

            if (flagDoor)
            {
                pc += 45;
            }
            return pc;
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