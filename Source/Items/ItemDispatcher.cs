using UnityEngine;

namespace HarmonyModBuggos {
	public enum DispatchResult {
		Success,   // remove from the queue
		Busy,      // leave it in the queue and retry later
		Invalid    // remove from the queue
	}

	public static class ItemDispatcher {

		// Caps for safety, not needed I assume
		private const int MaxPerFrame = 5;
		private const float BusyLogInterval = 5.0f;
		private static float nextBusyLog = 0.0f;

		public static void Update(ApSession apSession) {
			if (apSession == null || apSession.ItemsQueue.Count == 0) {
				return;
			}

			int handled = 0;
			int i = 0;
			while (i < apSession.ItemsQueue.Count && handled < MaxPerFrame) {
				var queued = apSession.ItemsQueue[i];
				DispatchResult result = Dispatch(queued.Item1, queued.Item2);

				if (result == DispatchResult.Busy) {
					if (Time.unscaledTime >= nextBusyLog) {
						InternalLogger.LogInfo("[ITEM] Busy, will retry: " + queued.Item1);
						nextBusyLog = Time.unscaledTime + BusyLogInterval;
					}
					i++; 
					continue;
				}

				apSession.ItemsQueue.RemoveAt(i);
				handled++;
			}
		}

		// itemFromOtherPlayer is unused, for planned ui
		public static DispatchResult Dispatch(string itemName, bool itemFromOtherPlayer) {

			// Talents: need a safe moment (the data-only grant needs a save name).
			if (ItemTable.TryGetTalent(itemName, out int talentId)) {
				if (!IsSafeToGrant()) {
					return DispatchResult.Busy;
				}
				try {
					switch (GrantService.GrantNow(talentId)) {
						case GrantResult.Granted:
							InternalLogger.LogInfo("[ITEM] Granted " + itemName + " (talent " + talentId + ")");
							return DispatchResult.Success;
						case GrantResult.AlreadyOwned:
							return DispatchResult.Success;
						case GrantResult.NotFound:
							return DispatchResult.Invalid; 
						default:
							return DispatchResult.Busy;     
					}
				} catch (System.Exception e) {
					InternalLogger.LogWarning("[ITEM] Grant threw for " + itemName + ": " + e);
					return DispatchResult.Busy;
				}
			}

			try {
				// The Swarmer is not a talent uses patches to mimic one.
				if (ItemTable.IsSwarmer(itemName)) {
					UnitFlags.SwarmerOwned = true;
					InternalLogger.LogInfo("[ITEM] Swarmer unlocked");
					return DispatchResult.Success;
				}

				// Fillers fire at any time.
				if (ItemTable.TryGetFiller(itemName, out FillerStat stat)) {
					FillerBoosts.Add(stat, ItemTable.FillerStep(stat));
					InternalLogger.LogInfo("[ITEM] Filler " + itemName);
					return DispatchResult.Success;
				}

				// Traps fire at any time
				if (ItemTable.TryGetTrap(itemName, out TrapKind trap)) {
					TrapEffects.Trigger(trap);
					InternalLogger.LogInfo("[ITEM] Trap " + itemName);
					return DispatchResult.Success;
				}
			} catch (System.Exception e) {
				InternalLogger.LogWarning("[ITEM] Handling threw for " + itemName + ", dropping: " + e);
				return DispatchResult.Invalid;
			}

			InternalLogger.LogWarning("[ITEM] Unknown item name, dropping: \"" + itemName + "\"");
			return DispatchResult.Invalid;
		}

		// Talens Granted Outside of these may cause issues, never actually tested, but bare minimum they wouldn't function until they get back to these stages
		private static bool IsSafeToGrant() {
			return GameData.GameState == GameStates.InOverworld
				|| GameData.GameState == GameStates.InEvolutionChamber;
		}
	}
}
