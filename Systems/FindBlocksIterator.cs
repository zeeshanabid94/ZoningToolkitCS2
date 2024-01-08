using System;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Game.Net;
using Game.Zones;
using HarmonyLib;
using Unity.Collections;
using Unity.Entities;

namespace ZoningToolkitMod.Iterators {
    public struct FindBlocksIterator
        : INativeQuadTreeIterator<Entity, Bounds2>,
        IUnsafeQuadTreeIterator<Entity, Bounds2>
    {
        public EdgeGeometry edgeGeometry;
        public Entity entity;
        public ComponentLookup<Block> blockLookup;
        public ComponentLookup<Owner> ownerLookup;

        public NativeArray<Entity> edgeBlocks;
        public bool Intersect(Bounds2 bounds)
        {
            Bounds2 edgeBounds = edgeGeometry.m_Bounds.xz + 4f;
            return MathUtils.Intersect(bounds, edgeBounds);
        }

        public void Iterate(Bounds2 bounds, Entity item)
        {
            if (blockLookup.HasComponent(item)) {
                if (ownerLookup.HasComponent(item)) {
                    Owner owner = ownerLookup[item];
                    if (owner.m_Owner == entity) {
                        Console.WriteLine("Block found.");
                        edgeBlocks.AddItem(item);
                    }
                    
                }
            }
        }
    }
}