using System.Runtime.CompilerServices;
using Game;
using Game.Common;
using Game.Events;
using Game.Net;
using Game.Zones;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Jobs;
using UnityEngine.PlayerLoop;
using ZoningToolkitMod.Iterators;
using SearchSystem = Game.Zones.SearchSystem;

namespace ZoningToolkitMod.Systems {
    [UpdateAfter(typeof(BlockSystem))]
    [CompilerGenerated]
    public class ZoningToolkitModSystem : GameSystemBase
    {
        private EntityQuery deletedEntityQuery;
        private EntityQuery createdEntityQuery;
        private EntityQuery updatedEntityQuery;
        private SearchSystem blockSearchSystem;
        private TypeHandle typeHandle;

        protected override void OnCreate()
        {
            base.OnCreate();

            deletedEntityQuery = this.GetEntityQuery(new EntityQueryDesc() {
                All = new ComponentType[] {
                    ComponentType.ReadOnly<EdgeGeometry>(),
                    ComponentType.ReadOnly<Edge>(),
                    ComponentType.ReadOnly<Deleted>()
                }
            });
            createdEntityQuery = this.GetEntityQuery(new EntityQueryDesc() {
                All = new ComponentType[] {
                    ComponentType.ReadOnly<EdgeGeometry>(),
                    ComponentType.ReadOnly<Edge>(),
                    ComponentType.ReadOnly<Created>()
                }
            });
            updatedEntityQuery = this.GetEntityQuery(new EntityQueryDesc() {
                All = new ComponentType[] {
                    ComponentType.ReadOnly<EdgeGeometry>(),
                    ComponentType.ReadOnly<Edge>(),
                    ComponentType.ReadOnly<Updated>()
                }
            });

            blockSearchSystem = World.GetExistingSystemManaged<SearchSystem>();
            typeHandle = new TypeHandle(ref CheckedStateRef);
            this.RequireAnyForUpdate(this.createdEntityQuery, this.updatedEntityQuery);

        }
        protected override void OnUpdate()
        {
            typeHandle.Update(ref CheckedStateRef);
            NativeArray<Entity> entities = this.createdEntityQuery.ToEntityArray(Allocator.Temp);
            NativeList<Entity> edgeBlocks = new NativeList<Entity>(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++) {
                Entity entity = entities[i];
                if (typeHandle.edgeGeometryLookup.HasComponent(entity)) {
                    EdgeGeometry edgeGeometry = typeHandle.edgeGeometryLookup[entity];
                    FindBlocksIterator iterator = new FindBlocksIterator() {
                        blockLookup = typeHandle.blockLookup,
                        ownerLookup = typeHandle.ownerLookup,
                        entity = entity,
                        edgeBlocks = edgeBlocks.AsArray(),
                        edgeGeometry = edgeGeometry
                    };
                    JobHandle jobHandle = this.Dependency;
                    blockSearchSystem.GetSearchTree(true, out jobHandle)
                        .Iterate(ref iterator);

                    for (int j = 0; j < edgeBlocks.Length; j++) {
                        Entity foundEntity = entities[j];

                        if (typeHandle.blockLookup.HasComponent(foundEntity)) {
                            Block block = typeHandle.blockLookup[foundEntity];

                            block.m_Size.y = 0;
                            this.EntityManager.SetComponentData(entity, block);

                        }
                    }
                }
            }
        }
        private struct TypeHandle {
            public ComponentLookup<EdgeGeometry> edgeGeometryLookup;

            public ComponentLookup<Owner> ownerLookup;

            public ComponentLookup<Block> blockLookup;

            public TypeHandle(ref SystemState state) {
                edgeGeometryLookup = state.GetComponentLookup<EdgeGeometry>();
                ownerLookup = state.GetComponentLookup<Owner>();
                blockLookup = state.GetComponentLookup<Block>();
            }

            public void Update(ref SystemState state) {
                this.ownerLookup.Update(ref state);
                this.blockLookup.Update(ref state);
                this.edgeGeometryLookup.Update(ref state);
            }
        }
    }


}