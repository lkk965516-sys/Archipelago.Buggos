using System;
using System.Collections.Generic;
using UnityEngine;

namespace HarmonyModBuggos {
	public static class CheckSender {

		private static bool initialized = false;
		private static bool loginHandled = false;
		private static bool reconciled = false;
		private static float nextReconcile = 0.0f;

		// First clears that happened before the store was loaded (areaId, evolution points).
		private static readonly List<int[]> waiting = new List<int[]>();

		// Called from Plugin.Load.
		public static void Init() {
			if (initialized) {
				return;
			}
			initialized = true;
			MissionState.FirstClear += OnFirstClear;
		}

		private static void OnFirstClear(AreaSection s) {
			try {
				int area = s.AreaID;
				int evo = (int)s.EvolutionPointsRewards;
				if (!LocationTable.IsLevel(area)) {
					InternalLogger.LogWarning("[CHECK] first clear of area " + area + " ignored NAL");
					return;
				}
				if (!ApStore.IsLoaded) {
					waiting.Add(new int[] { area, evo });
					InternalLogger.LogWarning("[CHECK] area " + area + " cleared before logging in. Held in memory, sent after you connect. Connect BEFORE playing, or this clear can be lost");
					return;
				}
				Record(area, evo);
			} catch (Exception e) {
				InternalLogger.LogError("[CHECK] first clear handling faile " + e);
			}
		}

		// Record, send.
		private static void Record(int area, int evo) {
			int table = LocationTable.PointCount(area);
			if (evo != table) {
				InternalLogger.LogWarning("[CHECK] area " + area + " reports " + evo + " evolution points but the table says " + table);
			}
			List<string> keys = LocationTable.KeysFor(area, evo);
			foreach (string key in keys) {
				ApStore.MarkSent(key);
			}
			ApStore.MarkFirstCleared(area);
			InternalLogger.LogInfo("[CHECK] recorded area " + area + ": " + keys.Count + " checks");
			SendKeys(keys);
		}

		private static void SendKeys(List<string> keys) {
			ApSession s = (ApClient.instance != null) ? ApClient.instance.integration : null;
			if (s == null || !s.connected) {
				InternalLogger.LogInfo("[CHECK] not connected; " + keys.Count + " checks are saved and will be sent at the next login");
				return;
			}
			var names = new List<string>();
			foreach (string key in keys) {
				string name = LocationTable.NameForKey(key);
				if (name != null) {
					names.Add(name);
				}
			}
			s.SendLocationNames(names);
		}

		public static void Update(ApSession s) {
			if (s == null) {
				return;
			}
			if (!s.connected) {
				loginHandled = false;   // the next login starts process again
				return;
			}
			if (!ApStore.IsLoaded) {
				return;
			}

			if (!loginHandled) {
				loginHandled = true;
				reconciled = false;
				LocationTable.Resolve(s);
				ResendAll(s);
				if (waiting.Count > 0) {
					var copy = new List<int[]>(waiting);
					waiting.Clear();
					foreach (int[] w in copy) {
						Record(w[0], w[1]);
					}
				}
			}

			TryReconcile();
			TryGoal(s);
		}

		private static void ResendAll(ApSession s) {
			var names = new List<string>();
			foreach (string key in ApStore.Data.SentLocations) {
				string name = LocationTable.NameForKey(key);
				if (name != null) {
					names.Add(name);
				}
			}
			if (names.Count > 0) {
				InternalLogger.LogInfo("[CHECK] re-sending " + names.Count + " recorded checks after login");
				s.SendLocationNames(names);
			}
		}

		private static void TryReconcile() {
			if (reconciled) {
				return;
			}
			if (!ApConfig.ReconcileClearsOnLogin) {
				reconciled = true;
				return;
			}
			if (Time.unscaledTime < nextReconcile) {
				return;
			}
			
			if (GameData.GameState != GameStates.InOverworld && GameData.GameState != GameStates.InEvolutionChamber) {
				return;
			}
			nextReconcile = Time.unscaledTime + 3.0f;

			try {
				var all = Resources.FindObjectsOfTypeAll<AreaSection>();
				if (all == null || all.Length == 0) {
					return;
				}

				int seen = 0;
				int recovered = 0;
				for (int i = 0; i < all.Length; i++) {
					AreaSection a = all[i];
					if (a == null || !LocationTable.IsLevel(a.AreaID)) {
						continue;
					}
					seen++;
					if (a.HasBeenConquered && !ApStore.IsFirstCleared(a.AreaID)) {
						InternalLogger.LogInfo("[CHECK] reconcile: area " + a.AreaID + " is conquered in the game but was never recorded");
						Record(a.AreaID, (int)a.EvolutionPointsRewards);
						recovered++;
					}
				}

				if (seen > 0) {
					reconciled = true;
					InternalLogger.LogInfo("[CHECK] reconcile finished: " + seen + " levels seen, " + recovered + " recovered");
				}
			} catch (Exception e) {
				InternalLogger.LogWarning("[CHECK] reconcile failed: " + e.Message);
			}
		}

		private static void TryGoal(ApSession s) {
			if (!s.sentCompletion && ApStore.IsFirstCleared(LocationTable.LastArea)) {
				InternalLogger.LogInfo("[CHECK] area " + LocationTable.LastArea + " is cleared: sending goal complete");
				s.SendCompletion();
			}
		}
	}
}
