using System;
using System.Collections.Generic;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Prefabs;
using Game.UI;
using Unity.Collections;
using Unity.Entities;
using ZoningAdjusterMod.Utilties;

namespace ZonePlacementMod.Systems {
    class ZonePlacementModUISystem: UISystemBase {
        private static HashSet<String> expectedPrefabNames = [
            "RoadsHighways",
            "RoadsLargeRoads",
            "RoadsMediumRoads",
            "RoadsServices",
            "RoadsSmallRoads",
            "RoadsIntersections",
            "RoadsParking",
            "RoadsRoundabouts"
        ];
        private string kGroup = "zoning_adjuster_ui_namespace";
        private string toolbarKGroup = "toolbar";
        private EnableZoneSystem enableZoneSystem;
        private PrefabSystem prefabSystem;
        private bool roadToolbarOpen;
        protected override void OnCreate()
        {
            base.OnCreate();

            this.enableZoneSystem = World.GetExistingSystemManaged<EnableZoneSystem>();
            this.prefabSystem = World.GetExistingSystemManaged<PrefabSystem>();
            this.roadToolbarOpen = false;

            this.AddUpdateBinding(new GetterValueBinding<string>(this.kGroup, "zoning_mode", () => enableZoneSystem.zoningMode.ToString()));
            this.AddUpdateBinding(new GetterValueBinding<bool>(this.kGroup, "upgrade_enabled", () => enableZoneSystem.upgradeEnabled));
            this.AddUpdateBinding(new GetterValueBinding<bool>(this.kGroup, "apply_to_new_roads", () => enableZoneSystem.shouldApplyToNewRoads));

            this.AddBinding(new TriggerBinding<string>(this.kGroup, "zoning_mode_update", zoningMode => {
                Console.WriteLine($"Zoning mode updated to ${zoningMode}.");
                if (this.roadToolbarOpen) {
                    this.enableZoneSystem.setZoningMode(zoningMode);
                }
            }));
            this.AddBinding(new TriggerBinding<bool>(this.kGroup, "upgrade_enabled", upgrade_enabled => {
                Console.WriteLine($"Upgrade Enabled updated to ${upgrade_enabled}.");
                if (this.roadToolbarOpen) {
                    this.enableZoneSystem.setUpgradeEnabled(upgrade_enabled);
                }
            }));
            this.AddBinding(new TriggerBinding<bool>(this.kGroup, "apply_to_new_roads", shouldApply => {
                Console.WriteLine($"Should Apply to new Roads updated to ${shouldApply}.");
                if (this.roadToolbarOpen) {
                    this.enableZoneSystem.setShouldApplyToNewRoads(shouldApply);
                }
            }));
            this.AddBinding(new TriggerBinding<Entity>(toolbarKGroup, "selectAssetMenu", (Action<Entity>) (assetMenu =>
            {
                Entity menuEntity = assetMenu;
                DynamicBuffer<UIGroupElement> buffer;
                if (menuEntity == Entity.Null
                    || !this.EntityManager.HasComponent<UIAssetMenuData>(menuEntity)
                    || !this.EntityManager.TryGetBuffer<UIGroupElement>(menuEntity, true, out buffer))
                    return;
                this.listEntityComponents(menuEntity);

                NativeList<UIObjectInfo> objects = UIObjectInfo.GetObjects(this.EntityManager, buffer, Allocator.TempJob);

                for (int index = 0; index < objects.Length; ++index) {
                    UIObjectInfo uiObjectInfo = objects[index];
                    Entity objectInfoEntity = uiObjectInfo.entity;
                    PrefabData uiInfoPrefab = uiObjectInfo.prefabData;
                    PrefabBase prefabBase = prefabSystem.GetPrefab<PrefabBase>(uiInfoPrefab);

                    Console.WriteLine($"Prefab name: {prefabBase.name}");

                    if (expectedPrefabNames.Contains(prefabBase.name)) {
                        this.enableZoneSystem.setUpgradeEnabled(true);
                        this.enableZoneSystem.setShouldApplyToNewRoads(true);
                        this.roadToolbarOpen = true;
                        objects.Dispose();
                        return;
                    }
                }

                this.roadToolbarOpen = false;
                this.enableZoneSystem.setUpgradeEnabled(false);
                this.enableZoneSystem.setShouldApplyToNewRoads(false);
                objects.Dispose();
            })));
        }
    }
}