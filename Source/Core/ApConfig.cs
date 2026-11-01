using BepInEx.Configuration;
using UnityEngine;

namespace HarmonyModBuggos {
	public static class ApConfig {
		public static ConfigEntry<string> Server;
		public static ConfigEntry<string> Slotname;
		public static ConfigEntry<string> Password;

	
		// Switch off to play the game normally. WARNING: your save file is shared, so do not mix normal play and randomizer runs.
		public static bool GuardsEnabled;

		// Connect once automatically a few seconds after launch (at the main menu), using the server, slot and password above.
		public static bool ConnectOnStart;
		public static float ConnectDelaySeconds;

		// Level checks (CheckSender.cs / LocationTable.cs)
		public static bool ReconcileClearsOnLogin;

		// Traps (TrapEffects.cs)
		public static float DamageZeroSeconds;
		public static float LockSwarmerSeconds;
		public static ConfigEntry<KeyCode> ToggleKey;

		public static void Init(ConfigFile cfg) {
			InternalLogger.LogInfo("[CONFIG] file=" + cfg.ConfigFilePath);

			Server = cfg.Bind(
				"Archipelago",
				"Server",
				"localhost:38281",
				"Archipelago server as host:port"
			);

			Slotname = cfg.Bind(
				"Archipelago",
				"Slotname",
				"Bug1",
				"Archipelago Slot Name from your YAML"
			);

			Password = cfg.Bind(
				"Archipelago",
				"Password",
				"",
				"Archipelago Password from your YAML"
			);

			GuardsEnabled = cfg.Bind(
				"General", 
				"GuardsEnabled", 
				true,
				"Block the player from buying or selling talents and hold evolution points at 0.").Value;

			ConnectOnStart = cfg.Bind(
				"Connection", 
				"ConnectOnStart", 
				false,
				"Connect to the server once, shortly after the game starts.").Value;

			ConnectDelaySeconds = cfg.Bind(
				"Connection", 
				"ConnectDelaySeconds", 
				5f,
				"Seconds after launch to wait before the automatic connect.").Value;

			ToggleKey = cfg.Bind(
				"Connection", 
				"ToggleWindowKey", 
				KeyCode.F8,
				"Key that shows / hides the connection window.");


			ReconcileClearsOnLogin = cfg.Bind(
				"Checks", 
				"ReconcileClearsOnLogin", 
				true,
				"After login, send checks for levels the game says are conquered but the mod never recorded, turn off if this game save already has progress from another seed.").Value;

			DamageZeroSeconds = cfg.Bind("Traps", "DamageZeroSeconds", 30f, "How long the Damage Zero trap lasts.").Value;
			LockSwarmerSeconds = cfg.Bind("Traps", "LockSwarmerSeconds", 120f, "How long the Lock Swarmer trap lasts.").Value;

		}
	}
}
