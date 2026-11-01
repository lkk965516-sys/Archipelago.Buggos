

using System;
using UnityEngine;

namespace HarmonyModBuggos {
	public static class ModTick {

		private static bool autoConnectTried = false;

		public static void Update(ApSession session) {
		try { ConnectionWindow.Update(); }
		catch (Exception e) { InternalLogger.LogError("[TICK] " + "ConnectionWindow" + " threw: " + e); }

		try { GameData.Refresh(); }
		catch (Exception e) { InternalLogger.LogError("[TICK] " + "GameData" + " threw: " + e); }

		try { AutoConnect(); }
		catch (Exception e) { InternalLogger.LogError("[TICK] " + "AutoConnect" + " threw: " + e); }

		try { ItemDispatcher.Update(session); }
		catch (Exception e) { InternalLogger.LogError("[TICK] " + "ItemDispatcher" + " threw: " + e); }

		try { TrapEffects.Update(); }
		catch (Exception e) { InternalLogger.LogError("[TICK] " + "TrapEffects" + " threw: " + e); }

		try { CheckSender.Update(session); }
		catch (Exception e) { InternalLogger.LogError("[TICK] " + "CheckSender" + " threw: " + e); }

		try { ApStore.Tick(session); }
		catch (Exception e) { InternalLogger.LogError("[TICK] " + "ApStore" + " threw: " + e); }
}

		// Might need to alter auto connect, does once Xs after launch, might want it to postfix a specific operation 
		private static void AutoConnect() {
			if (!ApConfig.ConnectOnStart || autoConnectTried) {
				return;
			}
			if (Time.realtimeSinceStartup < ApConfig.ConnectDelaySeconds) {
				return;
			}
			autoConnectTried = true;
			string error = ApClient.instance.Connect();
			if (string.IsNullOrEmpty(error)) {
				InternalLogger.LogInfo("[CONNECT] automatic connect finished");
			} else {
				InternalLogger.LogWarning("[CONNECT] automatic connect failed: " + error);
			}
		}
	}
}
