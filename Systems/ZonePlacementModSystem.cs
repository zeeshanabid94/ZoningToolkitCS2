using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Game;
using Game.Common;
using Game.Net;
using Game.Tools;
using Game.Zones;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using ZonePlacementMod.Components;

namespace ZonePlacementMod.Systems
{
    [CompilerGenerated]
    public class EnableZoneSystem: GameSystemBase {

        private EntityQuery updatedEntityQuery;
        private ComponentTypeHandle<Block> blockComponentTypeHandle;
        private EntityTypeHandle entityTypeHandle;
        private ComponentLookup<Owner> ownerComponentLookup;
        [ReadOnly]
        private ComponentLookup<Curve> curveComponentLookup;
        private ComponentLookup<ZoningInfo> zoningInfoComponentLookup;
        private ModificationBarrier4B modificationBarrier4B;
        private NetToolSystem netToolSystem;
        private ToolRaycastSystem raycastSystem;
        public ZoningMode zoningMode;

        protected override void OnCreate() {
            base.OnCreate();

            this.updatedEntityQuery = this.GetEntityQuery(new EntityQueryDesc() {
                All = [
                    ComponentType.ReadWrite<Block>(),
                    ComponentType.ReadWrite<Owner>()
                ],
                Any = [
                    ComponentType.ReadOnly<Updated>(),
                    ComponentType.ReadOnly<Applied>(),
                    ComponentType.ReadOnly<Created>()
                ]
            });
            
            this.blockComponentTypeHandle = this.GetComponentTypeHandle<Block>();
            this.ownerComponentLookup = this.GetComponentLookup<Owner>();
            this.curveComponentLookup = this.GetComponentLookup<Curve>(true);
            this.zoningInfoComponentLookup = this.GetComponentLookup<ZoningInfo>();
            this.modificationBarrier4B = World.GetExistingSystemManaged<ModificationBarrier4B>();
            this.netToolSystem = World.GetExistingSystemManaged<NetToolSystem>();
            this.raycastSystem = World.GetExistingSystemManaged<ToolRaycastSystem>();
            this.entityTypeHandle = this.GetEntityTypeHandle();

            setZoningMode("Default");
            this.RequireForUpdate(this.updatedEntityQuery);
        }
        
        [UnityEngine.Scripting.Preserve]
        protected override void OnUpdate()
        {
            EntityCommandBuffer entityCommandBuffer = this.modificationBarrier4B.CreateCommandBuffer();

            if (netToolSystem.actualMode != NetToolSystem.Mode.Replace) {
                JobHandle jobHandle = new UpdateZoneData() {
                    blockComponentTypeHandle = this.blockComponentTypeHandle,
                    curveComponentLookup = this.curveComponentLookup,
                    entityCommandBuffer = entityCommandBuffer.AsParallelWriter(),
                    entityTypeHandle = this.entityTypeHandle,
                    ownerComponentLookup = this.ownerComponentLookup,
                    zoningMode = this.zoningMode,
                    zoningInfoComponentLookup = this.zoningInfoComponentLookup,
                    upgradedEntity = null
                }.ScheduleParallel(this.updatedEntityQuery, this.Dependency);
                this.Dependency = JobHandle.CombineDependencies(this.Dependency, jobHandle);
            } else {
                raycastSystem.GetRaycastResult(out RaycastResult result);

                if (result.m_Owner != null) {
                    JobHandle jobHandle = new UpdateZoneData() {
                        blockComponentTypeHandle = this.blockComponentTypeHandle,
                        curveComponentLookup = this.curveComponentLookup,
                        entityCommandBuffer = entityCommandBuffer.AsParallelWriter(),
                        entityTypeHandle = this.entityTypeHandle,
                        ownerComponentLookup = this.ownerComponentLookup,
                        zoningMode = this.zoningMode,
                        zoningInfoComponentLookup = this.zoningInfoComponentLookup,
                        upgradedEntity = result.m_Owner
                    }.ScheduleParallel(this.updatedEntityQuery, this.Dependency);
                    this.Dependency = JobHandle.CombineDependencies(this.Dependency, jobHandle);
                }
            }

            this.modificationBarrier4B.AddJobHandleForProducer(this.Dependency);
        }

        public void setZoningMode(string zoningMode) {
            Console.WriteLine($"Changing zoning mode to ${zoningMode}");
            switch (zoningMode)
            {
                case "Left":
                    this.zoningMode = ZoningMode.Left;
                    break;
                case "Right":
                    this.zoningMode = ZoningMode.Right;
                    break;
                case "Default":
                    this.zoningMode = ZoningMode.Default;
                    break;
                case "None":
                    this.zoningMode = ZoningMode.None;
                    break;
                default:
                    this.zoningMode = ZoningMode.Default;
                    break;
            }
        }

        public Vector2 GetTangent(Bezier4x2 curve, float t) {
            // Calculate the derivative of the Bezier curve
            float2 derivative = 3 * math.pow(1 - t, 2) * (curve.b - curve.a) +
                                    6 * (1 - t) * t * (curve.c - curve.b) +
                                    3 * math.pow(t, 2) * (curve.d - curve.c);
            return new Vector2(derivative.x, derivative.y);
        }

        [BurstCompile]
        private struct UpdateZoneData : IJobChunk
        {
            [ReadOnly]
            public ZoningMode zoningMode;
            [ReadOnly]
            public Entity? upgradedEntity;
            [ReadOnly]
            public EntityTypeHandle entityTypeHandle;
            public ComponentTypeHandle<Block> blockComponentTypeHandle;
            [ReadOnly]
            public ComponentLookup<Owner> ownerComponentLookup;
            [ReadOnly]
            public ComponentLookup<Curve> curveComponentLookup;
            public ComponentLookup<ZoningInfo> zoningInfoComponentLookup;
            public EntityCommandBuffer.ParallelWriter entityCommandBuffer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                Console.WriteLine("Executing Zone Adjustment Job.");
                Stopwatch stopwatch = new Stopwatch();

                stopwatch.Start();
                NativeArray<Block> blocks = chunk.GetNativeArray(ref this.blockComponentTypeHandle);
                NativeArray<Entity> entities = chunk.GetNativeArray(this.entityTypeHandle);

                for (int i = 0; i < blocks.Length; i++) {
                    Entity entity = entities[i];
                    ZoningInfo entityZoningInfo = new ZoningInfo() {
                        zoningMode = this.zoningMode
                    };
                    if (this.ownerComponentLookup.HasComponent(entity)) {
                        Owner owner = this.ownerComponentLookup[entity];

                        if (this.curveComponentLookup.HasComponent(owner.m_Owner)) {
                            Curve curve = this.curveComponentLookup[owner.m_Owner];

                            if (upgradedEntity != null && upgradedEntity != owner.m_Owner) {
                                Console.WriteLine("Upgraded entity matches owner entity. Setting zoning info from owner...");
                                if (zoningInfoComponentLookup.HasComponent(owner.m_Owner)) {
                                    entityZoningInfo = this.zoningInfoComponentLookup[owner.m_Owner];
                                }
                            }

                            Console.WriteLine($"Entity is {entity}.");

                            Block block = blocks[i];

                            Console.WriteLine($"Block direction ${block.m_Direction}");
                            Console.WriteLine($"Block position ${block.m_Position}");

                            MathUtils.Distance(curve.m_Bezier.xz, block.m_Position.xz, out float closest_point_t);

                            Vector2 tangentVectorAtCurve = GetTangent(curve.m_Bezier.xz, closest_point_t);
                            Vector2 perpendicularToTangent = new Vector2(tangentVectorAtCurve.y, -tangentVectorAtCurve.x);

                            float dotProduct = Vector2.Dot(perpendicularToTangent, block.m_Direction);
                            
                            Console.WriteLine($"Dot product: ${dotProduct}");
                            Console.WriteLine($"Zoning mode is ${zoningMode}");

                            if (dotProduct > 0) {
                                if (entityZoningInfo.zoningMode == ZoningMode.Right || entityZoningInfo.zoningMode == ZoningMode.None) {
                                    entityCommandBuffer.AddComponent(unfilteredChunkIndex, entity, new Deleted());
                                    entityCommandBuffer.RemoveComponent<Updated>(unfilteredChunkIndex, entity);
                                    entityCommandBuffer.RemoveComponent<Created>(unfilteredChunkIndex, entity);
                                }
                            } else {
                                if (entityZoningInfo.zoningMode == ZoningMode.Left || entityZoningInfo.zoningMode == ZoningMode.None) {
                                    entityCommandBuffer.AddComponent(unfilteredChunkIndex, entity, new Deleted());
                                    entityCommandBuffer.RemoveComponent<Updated>(unfilteredChunkIndex, entity);
                                    entityCommandBuffer.RemoveComponent<Created>(unfilteredChunkIndex, entity);
                                }
                            }
                            entityCommandBuffer.AddComponent(unfilteredChunkIndex, owner.m_Owner, entityZoningInfo);
                        }
                    }
                }

                entities.Dispose();
                blocks.Dispose();

                stopwatch.Stop();

                Console.WriteLine($"Job took ${stopwatch.ElapsedMilliseconds}");
            }

            private Vector2 GetTangent(Bezier4x2 curve, float t) {
                // Calculate the derivative of the Bezier curve
                float2 derivative = 3 * math.pow(1 - t, 2) * (curve.b - curve.a) +
                                        6 * (1 - t) * t * (curve.c - curve.b) +
                                        3 * math.pow(t, 2) * (curve.d - curve.c);
                return new Vector2(derivative.x, derivative.y);
            }

        }
    }
}