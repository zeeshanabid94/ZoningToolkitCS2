using System;
using Colossal.Collections;
using Colossal.IO.AssetDatabase.Internal;
using Colossal.Mathematics;
using Game;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Zones;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using ZonePlacementMod.Components;
using BuildOrder = Game.Zones.BuildOrder;

namespace ZonePlacementMod.Systems
{
    [UpdateAfter(typeof(CellCheckSystem))]
    [UpdateAfter(typeof(BlockSystem))]
    [UpdateAfter(typeof(BlockReferencesSystem))]
    public class EnableZoneSystem: GameSystemBase {
        private EntityQuery entityQuery;
        ComponentLookup<Block> blockComponentLookup;
        ComponentLookup<ValidArea> validAreaLookup;
        BufferLookup<Cell> cellBufferLookup;
        private ComponentLookup<Owner> ownerComponentLookup;
        private ComponentLookup<Curve> curveComponentLookup;
        private ComponentLookup<Created> createComponentLookup;
        private ComponentLookup<Updated> updatedComponentLookup;
        private ComponentLookup<Applied> appliedComponentLookup;
        private ComponentLookup<ZoningInfo> zoningInfoComponentLookup;

        public ZoningMode zoningMode;

        protected override void OnCreate() {
            base.OnCreate();

            this.entityQuery = this.GetEntityQuery(new EntityQueryDesc() {
                All = [
                    ComponentType.ReadOnly<Block>(),
                    ComponentType.ReadOnly<ValidArea>()
                ],
                Any = [
                    ComponentType.ReadOnly<Updated>(),
                    ComponentType.ReadOnly<Created>()
                ]
            });
            
            this.blockComponentLookup = this.GetComponentLookup<Block>();         
            this.cellBufferLookup = this.GetBufferLookup<Cell>();
            this.validAreaLookup = this.GetComponentLookup<ValidArea>();
            this.ownerComponentLookup = this.GetComponentLookup<Owner>();
            this.curveComponentLookup = this.GetComponentLookup<Curve>();
            this.createComponentLookup = this.GetComponentLookup<Created>();
            this.updatedComponentLookup = this.GetComponentLookup<Updated>();
            this.appliedComponentLookup = this.GetComponentLookup<Applied>();
            this.zoningInfoComponentLookup = this.GetComponentLookup<ZoningInfo>();
            setZoningMode("Default");
            this.RequireForUpdate(this.entityQuery);
        }
        
        [UnityEngine.Scripting.Preserve]
        protected override void OnUpdate()
        {
            Console.WriteLine("**********OnUpdate for EnableZoneSystem******************");
            this.cellBufferLookup.Update(ref this.CheckedStateRef);
            this.blockComponentLookup.Update(ref this.CheckedStateRef);
            this.validAreaLookup.Update(ref this.CheckedStateRef);
            this.ownerComponentLookup.Update(ref this.CheckedStateRef);
            this.curveComponentLookup.Update(ref this.CheckedStateRef);
            this.updatedComponentLookup.Update(ref this.CheckedStateRef);
            this.createComponentLookup.Update(ref this.CheckedStateRef);
            this.appliedComponentLookup.Update(ref this.CheckedStateRef);
            this.zoningInfoComponentLookup.Update(ref this.CheckedStateRef);

            NativeArray<Entity> entities = this.entityQuery.ToEntityArray(Allocator.Temp);

            NativeList<Entity> updatedEntities = new NativeList<Entity>(Allocator.Temp);
            NativeList<Entity> createdEntities = new NativeList<Entity>(Allocator.Temp);
            NativeList<Entity> appliedEntities = new NativeList<Entity>(Allocator.Temp);
            // NativeArray<Entity> createdAndUpdatedEntities = new NativeArray<Entity>();

            for (int i = 0; i < entities.Length; i++) {
                Entity entity = entities[i];

                if (this.updatedComponentLookup.HasComponent(entity)) {
                    if (this.createComponentLookup.HasComponent(entity)) {
                        Console.WriteLine("Entity created.");
                        createdEntities.Add(entity);
                    } else {
                        Console.WriteLine("Entity updated.");
                        updatedEntities.Add(entity);
                    }
                }
            }
            processEntities(updatedEntities);
            processEntities(createdEntities);
            entities.Dispose();
            updatedEntities.Dispose();
            createdEntities.Dispose();
        }

        private void processEntities(NativeList<Entity> entities) {
            Console.WriteLine($"Processing ${entities.Length} entities.");
            for (int i = 0; i < entities.Length; i++) {
                Entity entity = entities[i];
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

                    if (this.curveComponentLookup.HasComponent(owner.m_Owner)) {
                        Curve curve = this.curveComponentLookup[owner.m_Owner];

                        // If the current was just created and used with a tool
                        // denoted by the apply component
                        if (this.appliedComponentLookup.HasComponent(owner.m_Owner)) {
                            if (this.zoningInfoComponentLookup.HasComponent(owner.m_Owner)) {
                                Console.WriteLine($"Setting zoning info ${entityZoningInfo}");
                                this.EntityManager.SetComponentData(owner.m_Owner, entityZoningInfo);
                            } else {
                                Console.WriteLine($"Adding & Setting zoning info ${entityZoningInfo}");
                                this.EntityManager.AddComponent(owner.m_Owner, ComponentType.ReadWrite<ZoningInfo>());
                                this.EntityManager.SetComponentData(owner.m_Owner, entityZoningInfo);
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

                        if (this.validAreaLookup.HasComponent(entity) 
                            && this.blockComponentLookup.HasComponent(entity)) {
                            Console.WriteLine($"Entity is {entity}.");

                            ValidArea validArea = this.validAreaLookup[entity];
                            Block block = this.blockComponentLookup[entity];

                            Console.WriteLine($"Block direction ${block.m_Direction}");
                            Console.WriteLine($"Block position ${block.m_Position}");

                            float closest_point_t = default;
                            MathUtils.Distance(curve.m_Bezier.xz, block.m_Position.xz, out closest_point_t);

                            Vector2 tangentVectorAtCurve = GetTangent(curve.m_Bezier.xz, closest_point_t);
                            Vector2 perpendicularToTangent = new Vector2(tangentVectorAtCurve.y, -tangentVectorAtCurve.x);

                            float dotProduct = Vector2.Dot(perpendicularToTangent, block.m_Direction);
                            
                            Console.WriteLine($"Dot product: ${dotProduct}");
                            Console.WriteLine($"Zoning mode is ${zoningMode}");
                            if (this.cellBufferLookup.HasBuffer(entity)) {
                                DynamicBuffer<Cell> buffer = cellBufferLookup[entity];

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
                }
            }
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

        private struct UpdateZoneData : Unity.Entities.IJobEntity
        {
            // [ReadOnly]
            // public EntityTypeHandle m_EntityType;
            // [ReadOnly]
            // public ComponentTypeHandle<EnableZoneData> enableZoneDataType;
            // [ReadOnly]
            // public ComponentTypeHandle<RoadComposition> RoadCompositionType;
            // [ReadOnly]
            // public ComponentLookup<EnableZoneData> enableZoneDataLookup;
            // public ComponentLookup<RoadComposition> roadCompositionLookup;
            public void Execute(ref RoadComposition entity)
            {
                UnityEngine.Debug.Log("Executing job for an entity");
                // if (roadCompositionLookup.HasComponent(entity)) {
                //     UnityEngine.Debug.Log("Entity has enable zone lookup data.");

                //     if (enableZoneDataLookup.HasComponent(entity)) {
                //         UnityEngine.Debug.Log("Entity has road composition.");

                //         RoadComposition roadComposition = roadCompositionLookup[entity];
                //         EnableZoneData enableZoneData = enableZoneDataLookup[entity];

                //         if (enableZoneData.enableRightZone && enableZoneData.enableRightZone) {
                //             roadComposition.m_Flags |= Game.Prefabs.RoadFlags.EnableZoning;
                //         } else {
                //             roadComposition.m_Flags |= ~Game.Prefabs.RoadFlags.EnableZoning;
                //         }
                //     }
                // }
            }
        }
    }
}