using HarmonyLib;
using System;

namespace HarmonyModBuggos {

	public static class MissionState {
		private static bool active = false;
		private static int entryAreaId = -1;
		private static bool entryConquered = false;

		// Raised once per genuine first clear. CheckSender subscribes
		// Built with event Action, to allow for later plans with ui pop-ups, based of another mod
		public static event Action<AreaSection> FirstClear;

		public static void Begin(AreaSection section) {
			if (section == null) {
				active = false;
				return;
			}
			active = true;
			entryAreaId = section.AreaID;
			entryConquered = section.HasBeenConquered;
			InternalLogger.LogInfo("[MISSION] start area=" + section.AreaID + " Already Done?  " + entryConquered);
		}

		public static void CheckFirstClear(GameCoordinator gc, bool success) {
			if (!success || !active) {
				return;
			}

			AreaSection sect = gc.CurrentSection;
			if (sect == null || sect.AreaID != entryAreaId) {
				return;

			}

			// ALTER WHEN ADDING Challenge Missions
			if (gc.IsCustomMission || gc.IsLevelEditorMission || gc.IsChallengeMission) {
				return;
			}

			active = false;   // a mission can only be reported once

			bool alreadyRecorded = ApStore.IsLoaded && ApStore.IsFirstCleared(sect.AreaID);
			if (entryConquered || alreadyRecorded) {
				InternalLogger.LogInfo("[LOCATION] replay of area " + sect.AreaID + ", no check");
				return;
			}

			InternalLogger.LogInfo("[LOCATION] FIRST CLEAR area=" + sect.AreaID + " evo=" + sect.EvolutionPointsRewards);
			if (FirstClear != null) {
				FirstClear(sect);
			}
		}
	}

	[HarmonyPatch(typeof(GameCoordinator), nameof(GameCoordinator.RunThisMission))]
	public static class RunThisMissionPatch {
		public static void Prefix(GameCoordinator __instance, AreaSection ThisSection) {
			try {
				GameData.Capture(__instance);
				StatBoosts.ApplyAll("RunThisMission");
				MissionState.Begin(ThisSection);
			} catch (Exception e) {
				InternalLogger.LogError("[MISSION] RunThisMission prefix failed: " + e);
			}
		}
	}

	[HarmonyPatch(typeof(GameCoordinator), nameof(GameCoordinator.MissionOver))]
	public static class MissionOverPatch {
		public static void Postfix(GameCoordinator __instance, bool Success) {
			try {
				GameData.Capture(__instance);
				MissionState.CheckFirstClear(__instance, Success);
			} catch (Exception e) {
				InternalLogger.LogError("[MISSION] MissionOver postfix failed: " + e);
			}
		}
	}
}
