using System;
using System.Collections.Generic;
using Colossal.UI.Binding;
using Game.Prefabs;
using Game.Tools;
using Game.UI;
using Unity.Entities;

namespace ZonePlacementMod.Systems {
    class ZonePlacementModUISystem: UISystemBase {
        private string kGroup = "zoning_adjuster_ui_namespace";
        private EnableZoneSystem enableZoneSystem;
        private bool activateModUI = false;
        private bool deactivateModUI = false;
        private bool modUIVisible = false;
        private GetterValueBinding<bool> visible;
        private List<IUpdateBinding> toUIBindings;
        private List<IBinding> fromUIBindings;
        private ToolSystem toolSystem;

        protected override void OnCreate()
        {
            base.OnCreate();

            this.enableZoneSystem = World.GetExistingSystemManaged<EnableZoneSystem>();
            this.toolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            this.toUIBindings = new List<IUpdateBinding>();
            this.fromUIBindings = new List<IBinding>();

            this.toolSystem.EventPrefabChanged = (Action<PrefabBase>) Delegate.Combine(toolSystem.EventPrefabChanged, new Action<PrefabBase>(OnPrefabChanged));
            this.toolSystem.EventToolChanged = (Action<ToolBaseSystem>)Delegate.Combine(toolSystem.EventToolChanged, new Action<ToolBaseSystem>(OnToolChange));
        }

        private void OnToolChange(ToolBaseSystem tool) {
            Console.WriteLine("Tool changed!");

            if (tool is NetToolSystem) {
                if (tool.GetPrefab() is RoadPrefab) {
                    Console.WriteLine("Prefab is RoadPrefab!");
                    RoadPrefab roadPrefab = (RoadPrefab) tool.GetPrefab();
                    Console.WriteLine($"Road prefab information.");
                    Console.WriteLine($"Road Type {roadPrefab.m_RoadType}.");
                    Console.WriteLine($"Road Zone Block {roadPrefab.m_ZoneBlock}.");

                    if (roadPrefab.m_ZoneBlock != null) {
                        activateModUI = true;
                    } else {
                        deactivateModUI = true;
                    }
                } else {
                    deactivateModUI = true;
                }
            } else {
                deactivateModUI = true;
            }
        }

        private void OnPrefabChanged(PrefabBase prefabBase) {
            Console.WriteLine("Prefab changed!");

            if (prefabBase is RoadPrefab) {
                Console.WriteLine("Prefab is RoadPrefab!");
                RoadPrefab roadPrefab = (RoadPrefab) prefabBase;
                Console.WriteLine($"Road prefab information.");
                Console.WriteLine($"Road Type {roadPrefab.m_RoadType}.");
                Console.WriteLine($"Road Zone Block {roadPrefab.m_ZoneBlock}.");

                if (roadPrefab.m_ZoneBlock != null) {
                    activateModUI = true;
                } else {
                    deactivateModUI = true;
                }
            } else {
                Console.WriteLine("Prefab is not RoadPrefab!");
                deactivateModUI = true;
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (activateModUI) {
                Console.WriteLine("Activating Mod UI.");

                // unset the trigger
                activateModUI = false;

                if (!modUIVisible) {
                    // do trigger processing
                    this.enableZoneSystem.setUpgradeEnabled(true);
                    
                    // setup pipe from game to UI and make mod visible
                    visible = new GetterValueBinding<bool>(this.kGroup, "visible", () => modUIVisible);
                    
                    if (toUIBindings.Count == 0) {
                        this.toUIBindings.Add(visible);
                        this.toUIBindings.Add(new GetterValueBinding<string>(this.kGroup, "zoning_mode", () => enableZoneSystem.zoningMode.ToString()));
                        this.toUIBindings.Add(new GetterValueBinding<bool>(this.kGroup, "upgrade_enabled", () => enableZoneSystem.upgradeEnabled));
                        foreach(IUpdateBinding binding in toUIBindings) {
                            this.AddUpdateBinding(binding);
                            binding.Update();
                        }
                    }
                    modUIVisible = true;
                    visible.Update();

                    // setup pipe from UI to game
                    if (fromUIBindings.Count == 0) {
                        this.fromUIBindings.Add(new TriggerBinding<string>(this.kGroup, "zoning_mode_update", zoningMode => {
                            Console.WriteLine($"Zoning mode updated to ${zoningMode}.");
                                this.enableZoneSystem.setZoningMode(zoningMode);
                            })
                        );
                        this.fromUIBindings.Add(new TriggerBinding<bool>(this.kGroup, "upgrade_enabled", upgrade_enabled => {
                            Console.WriteLine($"Upgrade Enabled updated to ${upgrade_enabled}.");
                                this.enableZoneSystem.setUpgradeEnabled(upgrade_enabled);
                            })
                        );

                        foreach(IBinding binding in fromUIBindings) {
                            this.AddBinding(binding);
                        }
                    }            

                    return;
                }
            }
                
            if (deactivateModUI) {
                Console.WriteLine("Deactivating Mod UI.");

                // Unset trigger
                deactivateModUI = false;

                if (modUIVisible) {

                    modUIVisible = false;
                    // update UI to hid UI
                    visible.Update();

                    // Disable mod upgrade
                    this.enableZoneSystem.setUpgradeEnabled(false);

                    return;
                }
            }
        }
    }
}