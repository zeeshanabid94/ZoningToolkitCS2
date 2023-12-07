using System;
using Colossal.UI.Binding;
using Game.UI;
using ZonePlacementMod.Components;

namespace ZonePlacementMod.Systems {
    class ZonePlacementModUISystem: UISystemBase {
        private string kGroup = "zoning_adjuster_ui_namespace";
        private EnableZoneSystem enableZoneSystem;
        protected override void OnCreate()
        {
            base.OnCreate();

            this.enableZoneSystem = World.GetExistingSystemManaged<EnableZoneSystem>();

            this.AddUpdateBinding(new GetterValueBinding<string>(this.kGroup, "zoning_mode", () => enableZoneSystem.zoningMode.ToString()));

            this.AddBinding(new TriggerBinding<string>(this.kGroup, "zoning_mode_update", zoningMode => {
                Console.WriteLine($"Zoning mode updated to ${zoningMode}.");
                this.enableZoneSystem.setZoningMode(zoningMode);
            }));
        }
    }
}