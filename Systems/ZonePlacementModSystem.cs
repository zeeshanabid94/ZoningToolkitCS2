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
using BuildOrder = Game.Zones.BuildOrder;
// using HookUILib.Core;

namespace ZonePlacementMod.Systems
{
    // public class ZonePlacementModUI : UIExtension {
    //     public new readonly ExtensionType extensionType = ExtensionType.Panel;
    //     public new readonly string extensionID = "zoning-adjuster";
    //     public new readonly string extensionContent;
        
    //     public ZonePlacementModUI() {
    //         extensionContent = LoadEmbeddedResource("ZoningAdjuster.ui-src.helloworld.transpiled.js");
    //     }
    // }

    [UpdateAfter(typeof(CellCheckSystem))]
    [UpdateAfter(typeof(BlockSystem))]
    [UpdateAfter(typeof(BlockReferencesSystem))]
    public class EnableZoneSystem: GameSystemBase {
        private enum ZoningMode {
            LeftOnly,
            RightOnly,
            Default,
            None
            
            // No Share
        }
        private EntityQuery entityQuery;
        ComponentLookup<Block> blockComponentLookup;
        ComponentLookup<ValidArea> validAreaLookup;
        BufferLookup<Cell> cellBufferLookup;
        private ComponentLookup<Owner> ownerComponentLookup;
        private ComponentLookup<Curve> curveComponentLookup;
        private ComponentLookup<Created> createComponentLookup;
        private ComponentLookup<Updated> updatedComponentLookup;
        private ComponentLookup<Game.Zones.BuildOrder> buildOrderLookup;
        private ZoningMode zoningMode;

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
            this.buildOrderLookup = this.GetComponentLookup<BuildOrder>();
            this.zoningMode = ZoningMode.RightOnly;
            this.RequireForUpdate(this.entityQuery);
        }
        
        [UnityEngine.Scripting.Preserve]
        protected override void OnUpdate()
        {
            // Console.WriteLine("OnUpdate for EnableZoneSystem");
            this.cellBufferLookup.Update(ref this.CheckedStateRef);
            this.blockComponentLookup.Update(ref this.CheckedStateRef);
            this.validAreaLookup.Update(ref this.CheckedStateRef);
            this.ownerComponentLookup.Update(ref this.CheckedStateRef);
            this.curveComponentLookup.Update(ref this.CheckedStateRef);
            this.updatedComponentLookup.Update(ref this.CheckedStateRef);
            this.createComponentLookup.Update(ref this.CheckedStateRef);
            this.buildOrderLookup.Update(ref this.CheckedStateRef);

            NativeArray<Entity> entities = this.entityQuery.ToEntityArray(Allocator.Temp);

            NativeList<Entity> updatedEntities = new NativeList<Entity>(Allocator.Temp);
            NativeList<Entity> createdEntities = new NativeList<Entity>(Allocator.Temp);
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

                if (this.ownerComponentLookup.HasComponent(entity)) {
                    Owner owner = this.ownerComponentLookup[entity];
                    EntityManager.GetChunk(owner.m_Owner).Archetype.GetComponentTypes().ForEach(componentType => {
                        Console.WriteLine($"Entity has following components: ${componentType.GetManagedType()}");
                    });

                    if (this.curveComponentLookup.HasComponent(owner.m_Owner)) {
                        Curve curve = this.curveComponentLookup[owner.m_Owner];

                        if (this.validAreaLookup.HasComponent(entity) 
                            && this.blockComponentLookup.HasComponent(entity)
                            && this.buildOrderLookup.HasComponent(entity)) {
                            Console.WriteLine($"Entity is {entity}.");
                            EntityManager.GetChunk(entity).Archetype.GetComponentTypes().ForEach(componentType => {
                                Console.WriteLine($"Entity has following components: ${componentType.GetManagedType()}");
                            });

                            ValidArea validArea = this.validAreaLookup[entity];
                            Block block = this.blockComponentLookup[entity];
                            BuildOrder buildOrder = this.buildOrderLookup[entity];

                            Console.WriteLine($"Block direction ${block.m_Direction}");
                            Console.WriteLine($"Block position ${block.m_Position}");

                            float closest_point_t = default;
                            MathUtils.Distance(curve.m_Bezier.xz, block.m_Position.xz, out closest_point_t);

                            Vector2 tangentVectorAtCurve = GetTangent(curve.m_Bezier.xz, closest_point_t);
                            Vector2 perpendicularToTangent = new Vector2(tangentVectorAtCurve.y, -tangentVectorAtCurve.x);

                            float dotProduct = Vector2.Dot(perpendicularToTangent, block.m_Direction);
                            
                            Console.WriteLine($"Dot product: ${dotProduct}");

                            Console.WriteLine($"Dot product of direction and position: ${dotProduct}");
                            if (this.cellBufferLookup.HasBuffer(entity)) {
                                Console.WriteLine("Entity has cell buffer.");

                                DynamicBuffer<Cell> buffer = cellBufferLookup[entity];

                                for (int z = validArea.m_Area.z; z < validArea.m_Area.w; z++) {
                                    for (int x = validArea.m_Area.x; x < validArea.m_Area.y; x++) {
                                        int index = z * block.m_Size.x + x;

                                        Console.WriteLine($"x: {x}, y: {z}, index: {index}");
                                        Cell cell = buffer[index];

                                        Console.WriteLine($"Cell state: ${cell.m_State}");
                                        if (dotProduct > 0 ) {
                                            Console.WriteLine("Block is to the Left of road.");

                                            if (this.zoningMode == ZoningMode.RightOnly || this.zoningMode == ZoningMode.None) {
                                                // Prevents zoning & make cell invisible
                                                if ((cell.m_State & (CellFlags.Occupied | CellFlags.Overridden)) != (CellFlags.Occupied | CellFlags.Overridden)) {
                                                    cell.m_State |= CellFlags.Overridden;
                                                    cell.m_State |= CellFlags.Occupied;
                                                    cell.m_State &= ~CellFlags.Visible;
                                                }

                                                if ((cell.m_State & CellFlags.Roadside) != 0) {
                                                    cell.m_State &= ~CellFlags.Roadside;
                                                }
                                            }
                                        } else if (dotProduct < 0) {
                                            Console.WriteLine("Block is to the Right of the road.");

                                            if (this.zoningMode == ZoningMode.LeftOnly || this.zoningMode == ZoningMode.None) {
                                                // Prevents zoning & make cell invisible
                                                if ((cell.m_State & (CellFlags.Occupied | CellFlags.Overridden)) != (CellFlags.Occupied | CellFlags.Overridden)) {
                                                    cell.m_State |= CellFlags.Overridden;
                                                    cell.m_State |= CellFlags.Occupied;
                                                    cell.m_State &= ~CellFlags.Visible;
                                                }

                                                if ((cell.m_State & CellFlags.Roadside) != 0) {
                                                    cell.m_State &= ~CellFlags.Roadside;
                                                }
                                            }
                                        }
                                        buffer[index] = cell;
                                    }
                                }
                            }
                        }
                    }
                }
            }   
        }

        // private ClosestEdgeIterator findClosestEdge(NativeQuadTree<Entity, QuadTreeBoundsXZ> netSearchTree, Block block, Entity blockEntity) {
        //     ClosestEdgeIterator ClosestEdgeIterator = new ClosestEdgeIterator() {
        //         block = block,
        //         edgeGeometryLookup = edgeGeometryLookup
        //     };

        //     netSearchTree.Iterate<ClosestEdgeIterator>(ref ClosestEdgeIterator);

        //     return ClosestEdgeIterator;
        // }

        private struct ClosestEdgeIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
        {

            public Block block;
            public ComponentLookup<EdgeGeometry> edgeGeometryLookup;

            public EdgeGeometry currentEdge = default;
            public float t = default;
            private float distance = float.MaxValue;
            private Bezier4x2 curve;

            public ClosestEdgeIterator()
            {
            }

            public bool Intersect(QuadTreeBoundsXZ bounds)
            {
                Console.WriteLine($"Intersecting with bound ${bounds}.");
                Console.WriteLine($"Block position ${block.m_Position}");
                return MathUtils.Intersect(
                    new Bounds2(
                        new float2(block.m_Position.x - block.m_Size.x * 4f, block.m_Position.z - block.m_Size.x * 4f),
                        new float2(block.m_Position.x + block.m_Size.x * 4f, block.m_Position.z + block.m_Size.y * 4f)),
                    bounds.m_Bounds.xz);
            }

            public void Iterate(QuadTreeBoundsXZ bounds, Entity item)
            {

                if (edgeGeometryLookup.HasComponent(item)) {
                    Console.WriteLine("Item has edge geometry data.");

                    EdgeGeometry edgeGeometry = edgeGeometryLookup[item];

                    Bezier4x2[] bezier4x2s = [
                        edgeGeometry.m_Start.m_Left.xz,
                        edgeGeometry.m_Start.m_Right.xz,
                        edgeGeometry.m_End.m_Left.xz,
                        edgeGeometry.m_End.m_Right.xz
                    ];

                    for (int i = 0; i < bezier4x2s.Length; i++) {
                        float new_t = default;
                        float new_distance = MathUtils.Distance(bezier4x2s[1], block.m_Position.xz, out new_t);
                        if (new_distance < distance) {
                            this.t = new_t;
                            this.distance = new_distance;
                            this.curve = bezier4x2s[i];
                        }
                    }
                }
            }

            public Vector2 GetTangent() {
                // Calculate the derivative of the Bezier curve
                float2 derivative = 3 * math.pow(1 - t, 2) * (curve.b - curve.a) +
                                    6 * (1 - t) * t * (curve.c - curve.b) +
                                    3 * math.pow(t, 2) * (curve.d - curve.c);
                return new Vector2(derivative.x, derivative.y);
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