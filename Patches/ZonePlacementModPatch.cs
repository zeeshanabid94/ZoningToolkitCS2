using Game.Net;
using Game.Zones;
using HarmonyLib;
using Unity.Collections;
using Unity.Entities;

using Game.Prefabs;
using Game.Tools;
using Game.Zones;
using HarmonyLib;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using ZonePlacementMod.Systems;
using System;
using System.Reflection;
using System.Linq;
using Game.Common;
using Unity.Jobs;
using Game.UI.Widgets;

namespace ZonePlacementModPatches
{
    [HarmonyPatch]
    class Patches
    {
    
        // [HarmonyPatch(typeof(BlockSystem), "OnUpdate")]
        // [HarmonyPrefix]
        // static void SetEnablezone(BlockSystem __instance) {
        //     ComponentLookup<RoadComposition> roadCompositionLookup = __instance.GetComponentLookup<RoadComposition>(false);
        //     ComponentLookup<Composition> compositionLookup = __instance.GetComponentLookup<Composition>(true);
        //     ComponentLookup<NetCompositionData> netCompositionDataLookup = __instance.GetComponentLookup<NetCompositionData>(false);
        //     ComponentLookup<OriginalNetCompositionData> originalNetCompositionDataLookup = __instance.GetComponentLookup<OriginalNetCompositionData>(false);

        //     roadCompositionLookup.Update(ref __instance.CheckedStateRef);
        //     compositionLookup.Update(ref __instance.CheckedStateRef);
        //     netCompositionDataLookup.Update(ref __instance.CheckedStateRef);
        //     originalNetCompositionDataLookup.Update(ref __instance.CheckedStateRef);

        //     EntityQueryBuilder query = new EntityQueryBuilder((AllocatorManager.AllocatorHandle) Allocator.Temp)
        //         .WithAll<Composition>()
        //         .WithAny<Updated>()
        //         .WithAny<Deleted>()
        //         .WithNone<Temp>();
        //     NativeArray<Entity> queriedEntities = __instance.GetEntityQuery(
        //         query
        //     ).ToEntityArray((AllocatorManager.AllocatorHandle) Allocator.Temp);


        //     for (int i = 0; i < queriedEntities.Length; i++) {
        //         Console.WriteLine($"Entity index ${i}");
        //         Entity entity = queriedEntities[i];

        //         if (compositionLookup.HasComponent(entity)) {
        //             Console.WriteLine($"Entity has composition data");

        //             Composition composition = compositionLookup[entity];

        //             if (roadCompositionLookup.HasComponent(composition.m_Edge)) {
        //                 Console.WriteLine($"composition.m_Edge has road composition.");

        //                 RoadComposition roadComposition = roadCompositionLookup[composition.m_Edge];

        //                 roadComposition.m_Flags |= Game.Prefabs.RoadFlags.EnableZoning;

        //                 __instance.EntityManager.SetComponentData(composition.m_Edge, roadComposition);
        //             }
        //         }
        //     }

        //     queriedEntities.Dispose();
        //     query.Dispose();
        // }
    }

}