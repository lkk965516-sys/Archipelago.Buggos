using System;
using UnityEngine;

namespace HarmonyModBuggos {
	public enum GameStates {
		Unknown,
		InMainMenu,
		InOverworld,
		TransitionToMission,
		InMission,
		TransitionToOverworld,
		TransitionToEvolutionChamber,
		InEvolutionChamber,
		TransitionToMainMenu,
		TransitionToLevelEditor,
		InLevelEditor
	}

	public static class GameData {

		public static GameStates GameState { get; private set; } = GameStates.Unknown;

		private static GameCoordinator coordinator = null;
		private static float nextLookup = 0.0f;
		private static GameStates lastLogged = GameStates.Unknown;

		// The coordinator, or null if none has been found yet, TrapEffects uses it for Leave Mission
		public static GameCoordinator Coordinator {
			get { return coordinator; }
		}

		// Called from anywhere that receive the coordinator as an instance.
		public static void Capture(GameCoordinator gc) {
			if (gc != null) {
				coordinator = gc;
			}
		}

		// Called per frame from ModTick. XYX
		public static void Refresh() {
			if (coordinator == null && Time.unscaledTime >= nextLookup) {
				nextLookup = Time.unscaledTime + 2.0f;
				try {
					var found = Resources.FindObjectsOfTypeAll(Il2CppInterop.Runtime.Il2CppType.Of<GameCoordinator>());
					foreach (var o in found) {
						var gc = o.TryCast<GameCoordinator>();
						if (gc != null) {
							coordinator = gc;
							InternalLogger.LogInfo("[STATE] found GameCoordinator");
							break;
						}
					}
				} catch (Exception e) {
					InternalLogger.LogWarning("[STATE] GameCoordinator lookup failed: " + e.Message);
				}
			}

			if (coordinator == null) {
				GameState = GameStates.Unknown;
				return;
			}

			GameStates parsed;
			if (!Enum.TryParse(coordinator.CurrentGameState.ToString(), out parsed)) {
				parsed = GameStates.Unknown;
			}
			GameState = parsed;

			if (GameState != lastLogged) {
				lastLogged = GameState;
				InternalLogger.LogInfo("[STATE] " + GameState);
			}
		}
	}
}
