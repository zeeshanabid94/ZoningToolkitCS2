using System.Reflection;
using Colossal.Logging;
using Game;
using Game.Modding;
using ZonePlacementMod.Systems;
using ZoningToolkitMod.Utilties;

namespace ZoningToolkitMod.Mod {
    public class ZoningToolkitMod : IMod
    {
        public static ZoningToolkitMod Instance { private set; get; }

        public static string ModName = "ZoningToolkit";

        private static ILog Logger;
        public void OnCreateWorld(UpdateSystem updateSystem)
        {
            Logger.Info("Mod is being loaded as part of OnCreateWorld.");
            // Add our defined VehicleCounterSystem to the update system which makes it part of
            // the game loop. Make sure it runs at the Phase `PostSimulation`
            updateSystem.UpdateAt<EnableZoneSystem>(SystemUpdatePhase.Modification4B);
            // updateSystem.UpdateAt<ZoningToolkitModSystem>(SystemUpdatePhase.Modification4B);
            updateSystem.UpdateAt<ZonePlacementModUISystem>(SystemUpdatePhase.UIUpdate);
        }

        public void OnDispose()
        {
            Instance = null;
            Logger.Info("Mod has been disposed.");
        }

        public void OnLoad()
        {
                        // Set instance reference.
            Instance = this;

            // Initialize logger.
            Logger = this.getLogger();
#if DEBUG
            Logger.Info("setting logging level to Debug");
            Logger.effectivenessLevel = Level.Debug;
#endif

            Logger.Info($"loading {ModName} version {Assembly.GetExecutingAssembly().GetName().Version}");
        }
    }
}
