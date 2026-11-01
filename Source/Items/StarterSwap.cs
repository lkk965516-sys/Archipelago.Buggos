using HarmonyLib;
using System;
using System.Collections.Generic;

namespace HarmonyModBuggos {

	public static class StarterSwap {

		private const int SwarmerUnitId = 3;

		// Just to not spam logs I only do the fist 6 
		private static int logs = 0;

		// Set by the Lock Swarmer trap
		public static bool TrapLock = false;

		// Entries removed because of the TRAP lock only so TrapEffects can put them back when the lock ends
		public static readonly List<KeyValuePair<Il2CppSystem.Collections.Generic.List<BugUnit>, BugUnit>> Removed
			= new List<KeyValuePair<Il2CppSystem.Collections.Generic.List<BugUnit>, BugUnit>>();

		public static bool ShouldStrip {
			get { return TrapLock || (ApStore.IsLoaded && !UnitFlags.SwarmerOwned); }
		}

		private static bool RecordRemovals {
			get { return TrapLock && ApStore.IsLoaded && UnitFlags.SwarmerOwned; }
		}

		public static void StripLists(BugOvermind o, string where) {
			if (!ShouldStrip) {
				return;
			}
			try {
				Strip(o.SpawnableBugs);
				Strip(o.WeightedSpawn);
				Strip(o.AvailableBugsForSpawning);
				if (logs < 6) {
					logs++;
					InternalLogger.LogInfo("[SWAP] stripped the Swarmer at " + where);
				}
			} catch (Exception e) {
				InternalLogger.LogError("[SWAP] failed: " + e);
			}
		}

		private static void Strip(Il2CppSystem.Collections.Generic.List<BugUnit> list) {
			if (list == null) {
				return;
			}
			for (int i = list.Count - 1; i >= 0; i--) {
				if (list[i] != null && list[i].UnitID == SwarmerUnitId) {
					if (RecordRemovals) {
						Removed.Add(new KeyValuePair<Il2CppSystem.Collections.Generic.List<BugUnit>, BugUnit>(list, list[i]));
					}
					list.RemoveAt(i);
				}
			}
		}
	}

	[HarmonyPatch(typeof(BugOvermind), nameof(BugOvermind.SetSpawnableBugs))]
	public static class SpawnListsPatch {
		public static void Postfix(BugOvermind __instance) {
			StarterSwap.StripLists(__instance, "SetSpawnableBugs");
		}
	}

	[HarmonyPatch(typeof(BugOvermind), nameof(BugOvermind.SetWeightSpawnChange))]
	public static class WeightListsPatch {
		public static void Postfix(BugOvermind __instance) {
			StarterSwap.StripLists(__instance, "SetWeightSpawnChange");
		}
	}

	[HarmonyPatch(typeof(EggSpawnerController), nameof(EggSpawnerController.SetSelectableBugs))]
	public static class EggFlagsPatch {
		public static void Postfix(EggSpawnerController __instance) {
			try {
				if (StarterSwap.ShouldStrip) {
					__instance.SwarmerSpawnOn = false;
				}
			} catch (Exception e) {
				InternalLogger.LogError("[SWAP] egg flag failed: " + e);
			}
		}
	}
}
