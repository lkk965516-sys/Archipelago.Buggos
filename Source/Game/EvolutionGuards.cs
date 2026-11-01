using HarmonyLib;

namespace HarmonyModBuggos {

	// Blocks the evolution tree reset known issue reset button loses all animations when clicked
	[HarmonyPatch(typeof(EvolutionGUI), nameof(EvolutionGUI.ResetEvolutionPoints))]
	public static class ResetEvolutionPointsPatch {
		public static bool Prefix() {
			if (!ApConfig.GuardsEnabled) {
				return true;
			}
			InternalLogger.LogInfo("[GATE] blocked evolution tree reset");
			return false;
		}
	}

	[HarmonyPatch(typeof(EvolutionManagerFinal), nameof(EvolutionManagerFinal.PurchaseThisNode))]
	public static class PurchaseGatePatch {
		public static bool Prefix() {
			if (!ApConfig.GuardsEnabled || GrantService.Allow) {
				return true;
			}
			InternalLogger.LogInfo("[GATE] blocked a purchase or sell through PurchaseThisNode");
			return false;
		}
	}

	[HarmonyPatch(typeof(EvolutionManagerFinal), nameof(EvolutionManagerFinal.SellThisNode))]
	public static class SellGatePatch {
		public static bool Prefix() {
			if (!ApConfig.GuardsEnabled) {
				return true;
			}
			InternalLogger.LogInfo("[GATE] blocked SellThisNode");
			return false;
		}
	}

	// evolution points are held at 0 at all times
	// Only GrantService.Allow can set a non-zero value
	public static class PointsLock {
		private static bool applying = false;

		public static void ForceZero(EvolutionManagerFinal mgr, string reason) {
			if (!ApConfig.GuardsEnabled || applying || GrantService.Allow) {
				return;
			}
			if (mgr.GetEvolutionPoints() == 0) {
				return;
			}
			applying = true;
			try {
				InternalLogger.LogInfo("[POINTS] " + reason + ": was " + mgr.GetEvolutionPoints() + ", forcing 0 Points");
				mgr.SetEvolutionPoints(0);
			} finally {
				applying = false;
			}
		}
	}

	[HarmonyPatch(typeof(EvolutionManagerFinal), nameof(EvolutionManagerFinal.SetEvolutionPoints))]
	public static class PointsLockPatch {
		public static void Postfix(EvolutionManagerFinal __instance) {
			PointsLock.ForceZero(__instance, "SetEvolutionPoints");
		}
	}

	// Runs every time the evolution screen initialises, game was found to recompute points randomly .
	[HarmonyPatch(typeof(EvolutionManagerFinal), nameof(EvolutionManagerFinal.EvolutionInit))]
	public static class EvolutionInitPatch {
		public static void Postfix(EvolutionManagerFinal __instance) {
			try {
				PointsLock.ForceZero(__instance, "EvolutionInit");
				StatBoosts.ApplyAll("EvolutionInit");
			} catch (System.Exception e) {
				InternalLogger.LogError("[EVO] EvolutionInit postfix failed: " + e);
			}
		}
	}
}
