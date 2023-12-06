using System;
using Colossal.IO.AssetDatabase.Internal;
using Colossal.Mathematics;
using Game;
using Game.Common;
using Game.Net;
using Game.Zones;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using ZonePlacementMod.Components;

namespace ZonePlacementMod.Systems
{
    [UpdateAfter(typeof(CellCheckSystem))]
    [UpdateAfter(typeof(BlockSystem))]
    [UpdateAfter(typeof(BlockReferencesSystem))]
    public class EnableZoneSystem: GameSystemBase {
        private EntityQuery createdEntityQuery;
        private EntityQuery roadQuery;

        private EntityQuery updatedEntityQuery;
        ComponentTypeHandle<Block> blockComponentTypeHandle;
        ComponentTypeHandle<ValidArea> validAreaTypeHandle;
        BufferTypeHandle<Cell> cellBufferTypeHandle;
        private ComponentLookup<Owner> ownerComponentLookup;
        private ComponentLookup<Curve> curveComponentLookup;
        private ComponentLookup<Created> createComponentLookup;
        private ComponentLookup<Updated> updatedComponentLookup;
        private ComponentLookup<Applied> appliedComponentLookup;
        private ComponentLookup<ZoningInfo> zoningInfoComponentLookup;

        private ModificationEndBarrier modificationEndBarrier;

        private EntityTypeHandle entityTypeHandle;

        public ZoningMode zoningMode;

        protected override void OnCreate() {
            base.OnCreate();

            this.roadQuery = this.GetEntityQuery(new EntityQueryDesc() {
                All = [
                    ComponentType.ReadOnly<Curve>(),
                    ComponentType.ReadOnly<SubBlock>(),
                    ComponentType.ReadOnly<Road>(),
                ],
                Any = [
                    ComponentType.ReadOnly<Updated>(),
                    ComponentType.ReadOnly<Applied>(),
                    ComponentType.ReadOnly<Created>()
                ]
            });

            this.updatedEntityQuery = this.GetEntityQuery(new EntityQueryDesc() {
                All = [
                    ComponentType.ReadOnly<Block>(),
                    ComponentType.ReadOnly<ValidArea>(),
                    ComponentType.ReadWrite<Cell>()
                ],
                Any = [
                    ComponentType.ReadOnly<Updated>(),
                    ComponentType.ReadOnly<Applied>()
                ]
            });

            this.createdEntityQuery = this.GetEntityQuery(new EntityQueryDesc() {
                All = [
                    ComponentType.ReadOnly<Block>(),
                    ComponentType.ReadOnly<ValidArea>(),
                    ComponentType.ReadWrite<Cell>()
                ],
                Any = [
                    ComponentType.ReadOnly<Created>()
                ]
            });
            
            this.blockComponentTypeHandle = this.GetComponentTypeHandle<Block>();         
            this.cellBufferTypeHandle = this.GetBufferTypeHandle<Cell>();
            this.validAreaTypeHandle = this.GetComponentTypeHandle<ValidArea>();
            this.ownerComponentLookup = this.GetComponentLookup<Owner>();
            this.curveComponentLookup = this.GetComponentLookup<Curve>();
            this.createComponentLookup = this.GetComponentLookup<Created>();
            this.updatedComponentLookup = this.GetComponentLookup<Updated>();
            this.appliedComponentLookup = this.GetComponentLookup<Applied>();
            this.zoningInfoComponentLookup = this.GetComponentLookup<ZoningInfo>();
            this.modificationEndBarrier = World.GetExistingSystemManaged<ModificationEndBarrier>();
            this.entityTypeHandle = this.GetEntityTypeHandle();
            setZoningMode("Default");
            this.RequireForUpdate(this.updatedEntityQuery);
            this.RequireForUpdate(this.createdEntityQuery);
        }
        
        [UnityEngine.Scripting.Preserve]
        protected override void OnUpdate()
        {
            Console.WriteLine("**********OnUpdate for EnableZoneSystem******************");
            this.cellBufferTypeHandle.Update(ref this.CheckedStateRef);
            this.blockComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.validAreaTypeHandle.Update(ref this.CheckedStateRef);
            this.ownerComponentLookup.Update(ref this.CheckedStateRef);
            this.curveComponentLookup.Update(ref this.CheckedStateRef);
            this.updatedComponentLookup.Update(ref this.CheckedStateRef);
            this.createComponentLookup.Update(ref this.CheckedStateRef);
            this.appliedComponentLookup.Update(ref this.CheckedStateRef);
            this.zoningInfoComponentLookup.Update(ref this.CheckedStateRef);
            this.entityTypeHandle.Update(ref this.CheckedStateRef);

            NativeArray<Entity> updatedEntities = this.updatedEntityQuery.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < updatedEntities.Length; i++) {
                Entity entity = updatedEntities[i];
                ZoningInfo entityZoningInfo = new ZoningInfo() {
                    zoningMode = this.zoningMode
                };

                EntityManager.GetChunk(entity).Archetype.GetComponentTypes().ForEach(componentType => {
                    Console.WriteLine($"Entity has following component: ${componentType.GetManagedType()}");
                });

                if (this.ownerComponentLookup.HasComponent(entity)) {
                    Owner owner = this.ownerComponentLookup[entity];
                    EntityManager.GetChunk(owner.m_Owner).Archetype.GetComponentTypes().ForEach(componentType => {
                        Console.WriteLine($"Owner Entity has following components: ${componentType.GetManagedType()}");
                    });
                }
            }

            updatedEntities.Dispose();

            NativeArray<Entity> createdEntities = this.updatedEntityQuery.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < createdEntities.Length; i++) {
                Entity entity = createdEntities[i];
                ZoningInfo entityZoningInfo = new ZoningInfo() {
                    zoningMode = this.zoningMode
                };

                EntityManager.GetChunk(entity).Archetype.GetComponentTypes().ForEach(componentType => {
                    Console.WriteLine($"Entity has following component: ${componentType.GetManagedType()}");
                });

                if (this.ownerComponentLookup.HasComponent(entity)) {
                    Owner owner = this.ownerComponentLookup[entity];
                    EntityManager.GetChunk(owner.m_Owner).Archetype.GetComponentTypes().ForEach(componentType => {
                        Console.WriteLine($"Owner Entity has following components: ${componentType.GetManagedType()}");
                    });
                }
            }

            createdEntities.Dispose();

            JobHandle allJobs = this.Dependency;

            JobHandle updateJobHandle = new UpdateZoneData() {
                appliedComponentLookup = this.appliedComponentLookup,
                blockComponentTypeHandle = this.blockComponentTypeHandle,
                cellBufferTypeHandle = this.cellBufferTypeHandle,
                createComponentLookup = this.createComponentLookup,
                curveComponentLookup = this.curveComponentLookup,
                entityCommandBuffer = this.modificationEndBarrier.CreateCommandBuffer(),
                entityTypeHandle = this.entityTypeHandle,
                ownerComponentLookup = this.ownerComponentLookup,
                updatedComponentLookup = this.updatedComponentLookup,
                validAreaTypeHandle = this.validAreaTypeHandle,
                zoningMode = this.zoningMode,
                zoningInfoComponentLookup = this.zoningInfoComponentLookup
            }.Schedule(this.updatedEntityQuery, allJobs);

            allJobs = JobHandle.CombineDependencies(updateJobHandle, allJobs);

            JobHandle createdJobHandle = new UpdateZoneData() {
                appliedComponentLookup = this.appliedComponentLookup,
                blockComponentTypeHandle = this.blockComponentTypeHandle,
                cellBufferTypeHandle = this.cellBufferTypeHandle,
                createComponentLookup = this.createComponentLookup,
                curveComponentLookup = this.curveComponentLookup,
                entityCommandBuffer = this.modificationEndBarrier.CreateCommandBuffer(),
                entityTypeHandle = this.entityTypeHandle,
                ownerComponentLookup = this.ownerComponentLookup,
                updatedComponentLookup = this.updatedComponentLookup,
                validAreaTypeHandle = this.validAreaTypeHandle,
                zoningMode = this.zoningMode,
                zoningInfoComponentLookup = this.zoningInfoComponentLookup
            }.Schedule(this.createdEntityQuery, allJobs);

            allJobs = JobHandle.CombineDependencies(createdJobHandle, allJobs);

            this.modificationEndBarrier.AddJobHandleForProducer(allJobs);

            this.Dependency = allJobs;
        }

        public void setZoningMode(string zoningMode) {
            Console.WriteLine($"Changing zoning mode to ${zoningMode}");
            switch (zoningMode)
            {
                case "Left":
                    this.zoningMode = ZoningMode.LeftOnly;
                    break;
                case "Right":
                    this.zoningMode = ZoningMode.RightOnly;
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

        private struct UpdateZoneData : IJobChunk
        {
            public ZoningMode zoningMode;
            public EntityTypeHandle entityTypeHandle;
            public ComponentTypeHandle<Block> blockComponentTypeHandle;
            public ComponentTypeHandle<ValidArea> validAreaTypeHandle;
            public BufferTypeHandle<Cell> cellBufferTypeHandle;
            public ComponentLookup<Owner> ownerComponentLookup;
            public ComponentLookup<Curve> curveComponentLookup;
            public ComponentLookup<Created> createComponentLookup;
            public ComponentLookup<Updated> updatedComponentLookup;
            public ComponentLookup<Applied> appliedComponentLookup;
            public ComponentLookup<ZoningInfo> zoningInfoComponentLookup;
            public EntityCommandBuffer entityCommandBuffer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                UnityEngine.Debug.Log("Executing Zone Adjustment Job.");
                NativeArray<ValidArea> validAreas = chunk.GetNativeArray(ref this.validAreaTypeHandle);
                NativeArray<Block> blocks = chunk.GetNativeArray(ref this.blockComponentTypeHandle);
                NativeArray<Entity> entities = chunk.GetNativeArray(this.entityTypeHandle);
                BufferAccessor<Cell> cellBuffers= chunk.GetBufferAccessor(ref this.cellBufferTypeHandle);
                ZoningInfo entityZoningInfo = new ZoningInfo() {
                    zoningMode = this.zoningMode
                };

                for (int i = 0; i < entities.Length; i++) {
                    Entity entity = entities[i];
                    if (this.ownerComponentLookup.HasComponent(entity)) {
                        Owner owner = this.ownerComponentLookup[entity];

                        if (this.curveComponentLookup.HasComponent(owner.m_Owner)) {
                            Curve curve = this.curveComponentLookup[owner.m_Owner];

                            // If the current was just created and used with a tool
                            // denoted by the apply component
                            if (this.appliedComponentLookup.HasComponent(owner.m_Owner)) {
                                if (this.zoningInfoComponentLookup.HasComponent(owner.m_Owner)) {
                                    Console.WriteLine($"Setting zoning info ${entityZoningInfo}");
                                    entityCommandBuffer.SetComponent(owner.m_Owner, entityZoningInfo);
                                } else {
                                    Console.WriteLine($"Adding & Setting zoning info ${entityZoningInfo}");
                                    entityCommandBuffer.AddComponent(owner.m_Owner, ComponentType.ReadWrite<ZoningInfo>());
                                    entityCommandBuffer.SetComponent(owner.m_Owner, entityZoningInfo);
                                }
                            } else {
                                if (this.zoningInfoComponentLookup.HasComponent(owner.m_Owner)) {
                                    Console.WriteLine($"Getting zoning info ${entityZoningInfo}");
                                    entityZoningInfo = this.zoningInfoComponentLookup[owner.m_Owner];
                                } else {
                                    // Don't do anything and continue
                                    continue;
                                }
                            }

                            Console.WriteLine($"Entity is {entity}.");

                            ValidArea validArea = validAreas[i];
                            Block block = blocks[i];

                            Console.WriteLine($"Block direction ${block.m_Direction}");
                            Console.WriteLine($"Block position ${block.m_Position}");

                            MathUtils.Distance(curve.m_Bezier.xz, block.m_Position.xz, out float closest_point_t);

                            Vector2 tangentVectorAtCurve = GetTangent(curve.m_Bezier.xz, closest_point_t);
                            Vector2 perpendicularToTangent = new Vector2(tangentVectorAtCurve.y, -tangentVectorAtCurve.x);

                            float dotProduct = Vector2.Dot(perpendicularToTangent, block.m_Direction);
                            
                            Console.WriteLine($"Dot product: ${dotProduct}");
                            Console.WriteLine($"Zoning mode is ${zoningMode}");

                            DynamicBuffer<Cell> buffer = cellBuffers[i];

                            for (int z = validArea.m_Area.z; z < validArea.m_Area.w; z++) {
                                for (int x = validArea.m_Area.x; x < validArea.m_Area.y; x++) {
                                    int index = z * block.m_Size.x + x;
                                    Cell cell = buffer[index];

                                    Console.WriteLine($"Cell state before: ${cell.m_State}");
                                    if (dotProduct > 0 ) {
                                        Console.WriteLine("Block is to the Left of road.");

                                        if (entityZoningInfo.zoningMode == ZoningMode.RightOnly || entityZoningInfo.zoningMode == ZoningMode.None) {
                                            // Prevents zoning & make cell invisible
                                            if ((cell.m_State & (CellFlags.Occupied | CellFlags.Overridden)) != (CellFlags.Occupied | CellFlags.Overridden)) {
                                                cell.m_State |= CellFlags.Overridden;
                                                cell.m_State |= CellFlags.Occupied;
                                            }

                                            if ((cell.m_State & CellFlags.Visible) != 0) {
                                                cell.m_State &= ~CellFlags.Visible;
                                            }
                                        } else {
                                            // Reverse the state here to make the cells appear.

                                            if ((cell.m_State & (CellFlags.Occupied | CellFlags.Overridden)) == (CellFlags.Occupied | CellFlags.Overridden)) {
                                                cell.m_State &= ~CellFlags.Overridden;
                                                cell.m_State &= ~CellFlags.Occupied;
                                            }

                                            if ((cell.m_State & CellFlags.Visible) == 0) {
                                                cell.m_State |= CellFlags.Visible;
                                            }
                                        }
                                    } else if (dotProduct < 0) {
                                        Console.WriteLine("Block is to the Right of the road.");

                                        if (entityZoningInfo.zoningMode == ZoningMode.LeftOnly || entityZoningInfo.zoningMode == ZoningMode.None) {
                                            // Prevents zoning & make cell invisible
                                            if ((cell.m_State & (CellFlags.Occupied | CellFlags.Overridden)) != (CellFlags.Occupied | CellFlags.Overridden)) {
                                                cell.m_State |= CellFlags.Overridden;
                                                cell.m_State |= CellFlags.Occupied;
                                            }

                                            if ((cell.m_State & CellFlags.Visible) != 0) {
                                                cell.m_State &= ~CellFlags.Visible;
                                            }
                                        }
                                    } else {
                                        // Reverse the state here to make the cells appear.
                                        if (x == 0 && (cell.m_State & CellFlags.Roadside) == 0) {
                                            cell.m_State |= CellFlags.Roadside;
                                        }

                                        if ((cell.m_State & CellFlags.Visible) == 0) {
                                            cell.m_State |= CellFlags.Visible;
                                        }

                                        if ((cell.m_State & (CellFlags.Occupied | CellFlags.Overridden)) == (CellFlags.Occupied | CellFlags.Overridden)) {
                                            cell.m_State &= ~CellFlags.Overridden;
                                            cell.m_State &= ~CellFlags.Occupied;
                                        }
                                    }
                                    Console.WriteLine($"Cell state after: ${cell.m_State}");

                                    buffer[index] = cell;
                                }
                            }
                        }
                    }
                }

                validAreas.Dispose();
                entities.Dispose();
                blocks.Dispose();
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