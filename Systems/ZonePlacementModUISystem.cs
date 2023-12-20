using System;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Prefabs;
using Game.UI;
using Unity.Entities;
using ZonePlacementMod.Components;
using ZoningAdjusterMod.Utilties;

namespace ZonePlacementMod.Systems {
    class ZonePlacementModUISystem: UISystemBase {
        private string kGroup = "zoning_adjuster_ui_namespace";
        private string toolbarKGroup = "toolbar";
        private EnableZoneSystem enableZoneSystem;
        private int currentLoadedIndex;
        protected override void OnCreate()
        {
            base.OnCreate();

            this.enableZoneSystem = World.GetExistingSystemManaged<EnableZoneSystem>();

            this.AddUpdateBinding(new GetterValueBinding<string>(this.kGroup, "zoning_mode", () => enableZoneSystem.zoningMode.ToString()));
            this.AddUpdateBinding(new GetterValueBinding<bool>(this.kGroup, "enabled", () => enableZoneSystem.upgradeEnabled));
            this.AddUpdateBinding(new GetterValueBinding<bool>(this.kGroup, "apply_to_new_roads", () => enableZoneSystem.shouldApplyToNewRoads));

            this.AddBinding(new TriggerBinding<string>(this.kGroup, "zoning_mode_update", zoningMode => {
                Console.WriteLine($"Zoning mode updated to ${zoningMode}.");
                this.enableZoneSystem.setZoningMode(zoningMode);
            }));
            this.AddBinding(new TriggerBinding<bool>(this.kGroup, "enabled", enabled => {
                Console.WriteLine($"Enabled updated to ${enabled}.");
                if (currentLoadedIndex == 1685) {
                    this.enableZoneSystem.setUpgradeEnabled(enabled);
                }
            }));
            this.AddBinding(new TriggerBinding<bool>(this.kGroup, "apply_to_new_roads", shouldApply => {
                Console.WriteLine($"Should Apply to new Roads updated to ${shouldApply}.");
                this.enableZoneSystem.setShouldApplyToNewRoads(shouldApply);
            }));
            this.AddBinding(new TriggerBinding<Entity>(toolbarKGroup, "selectAssetMenu", (Action<Entity>) (assetMenu =>
            {
                Entity menuEntity = assetMenu;
                if (!(menuEntity != Entity.Null) || !this.EntityManager.HasComponent<UIAssetMenuData>(menuEntity))
                    return;
                this.listEntityComponents(menuEntity);

                DynamicBuffer<LoadedIndex> loadedIndex = this.EntityManager.GetBuffer<LoadedIndex>(menuEntity);
                Console.WriteLine("Selected menu item index: " + loadedIndex.ElementAt(0).m_Index);

                currentLoadedIndex = loadedIndex.ElementAt(0).m_Index;
                if (loadedIndex.ElementAt(0).m_Index != 1685) {
                    enableZoneSystem.setUpgradeEnabled(false);
                } else {
                    enableZoneSystem.setUpgradeEnabled(true);
                }
            })));
        }
    }
}