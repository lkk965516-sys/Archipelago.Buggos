

using System;
using System.Collections.Generic;
using UnityEngine;

namespace HarmonyModBuggos {
	public static class TrapEffects {

		// ---- damage zero
		private static bool damageActive = false;
		private static float damageEnd = 0.0f;
		private static float nextDamageScan = 0.0f;
		private static readonly Dictionary<IntPtr, int> savedDamage = new Dictionary<IntPtr, int>();
		private static readonly Dictionary<int, int> templateDamage = new Dictionary<int, int>();   // UnitID -> original damage

		// ---  lock swarmer
		private static bool lockActive = false;
		private static float lockEnd = 0.0f;

		// ---- leave mission
		private const float LeaveDelaySeconds = 3.0f;
		private const float LeaveCooldownSeconds = 10.0f;
		private static int pendingLeaves = 0;
		private static float inMissionSince = -1.0f;
		private static float lastLeave = -100.0f;

		public static void Trigger(TrapKind kind) {
			try {
				switch (kind) {
					case TrapKind.DamageZero:
						StartDamageZero();
						break;
					case TrapKind.LeaveMission:
						pendingLeaves++;
						InternalLogger.LogInfo("[TRAP] LeaveMission queued (" + pendingLeaves + " pending)");
						break;
					case TrapKind.LockSwarmer:
						StartLock();
						break;
					default:
						InternalLogger.LogWarning("[TRAP] unknown trap " + kind);
						break;
				}
			} catch (Exception e) {
				InternalLogger.LogError("[TRAP] trigger failed for " + kind + ": " + e);
			}
		}

		public static void Update() {
			float now = Time.unscaledTime;
			try {
				if (damageActive) {
					if (now >= damageEnd) {
						EndDamageZero();
					} else if (now >= nextDamageScan) {
						nextDamageScan = now + 2.0f;
						ZeroAll(false);
					}
				}

				if (lockActive && now >= lockEnd) {
					EndLock();
				}

				UpdateLeave(now);
			} catch (Exception e) {
				InternalLogger.LogError("[TRAP] update failed: " + e);
			}
		}

		// damage zero

		private static void StartDamageZero() {
			damageEnd = Time.unscaledTime + ApConfig.DamageZeroSeconds;
			if (damageActive) {
				InternalLogger.LogInfo("[TRAP] DamageZero extended");
				return;
			}
			damageActive = true;
			savedDamage.Clear();
			templateDamage.Clear();

			var templates = BuggosMaster.AllBugUnits;
			if (templates != null) {
				for (int i = 0; i < templates.Count; i++) {
					BugUnit t = templates[i];
					if (t != null) {
						templateDamage[t.UnitID] = t.Damage;
					}
				}
			}

			StatBoosts.HoldDamage = true;
			nextDamageScan = Time.unscaledTime + 2.0f;
			ZeroAll(true);
			InternalLogger.LogInfo("[TRAP] DamageZero started for " + ApConfig.DamageZeroSeconds + "s");
		}

		private static int OriginalFor(BugUnit u, bool first) {
			if (first) {
				return u.Damage;
			}
			int original;
			return templateDamage.TryGetValue(u.UnitID, out original) ? original : u.Damage;
		}

		private static void ZeroAll(bool first) {
			var all = Resources.FindObjectsOfTypeAll<BugUnit>();
			if (all == null) {
				return;
			}
			for (int i = 0; i < all.Length; i++) {
				BugUnit u = all[i];
				if (u == null) {
					continue;
				}
				try {
					if (!savedDamage.ContainsKey(u.Pointer)) {
						savedDamage[u.Pointer] = OriginalFor(u, first);
					}
					u.Damage = 0;
				} catch (Exception) {
					// a unit being destroyed, no need for an error log
				}
			}
		}

		private static void EndDamageZero() {
			damageActive = false;
			int restored = 0;
			try {
				var all = Resources.FindObjectsOfTypeAll<BugUnit>();
				if (all != null) {
					for (int i = 0; i < all.Length; i++) {
						BugUnit u = all[i];
						if (u == null) {
							continue;
						}
						try {
							int original;
							if (savedDamage.TryGetValue(u.Pointer, out original)) {
								u.Damage = original;
							} else if (templateDamage.TryGetValue(u.UnitID, out original)) {
								u.Damage = original;
							} else {
								continue;
							}
							restored++;
						} catch (Exception) {
						}
					}
				}
			} finally {
				savedDamage.Clear();
				templateDamage.Clear();
				StatBoosts.HoldDamage = false;
				StatBoosts.ApplyAll("DamageZeroEnd");
			}
			InternalLogger.LogInfo("[TRAP] DamageZero ended, restored " + restored + " units");
		}

		// lock swarmer
		private static void StartLock() {
			if (!ApStore.IsLoaded || !UnitFlags.SwarmerOwned) {
				InternalLogger.LogInfo("[TRAP] LockSwarmer skipped, the Swarmer is not owned");
				return;
			}
			lockEnd = Time.unscaledTime + ApConfig.LockSwarmerSeconds;
			if (lockActive) {
				InternalLogger.LogInfo("[TRAP] LockSwarmer extended");
				return;
			}
			lockActive = true;
			StarterSwap.TrapLock = true;
			StarterSwap.Removed.Clear();

			// A mission strips its live lists and egg flags, Between missions these objects do not exist
			// strip the lists when the next mission builds them
			var overminds = Resources.FindObjectsOfTypeAll<BugOvermind>();
			if (overminds != null) {
				for (int i = 0; i < overminds.Length; i++) {
					if (overminds[i] != null) {
						StarterSwap.StripLists(overminds[i], "trap");
					}
				}
			}
			SetEggFlags(false);
			InternalLogger.LogInfo("[TRAP] LockSwarmer started for " + ApConfig.LockSwarmerSeconds + "s");
		}

		private static void EndLock() {
			lockActive = false;
			StarterSwap.TrapLock = false;
			int back = 0;
			foreach (var entry in StarterSwap.Removed) {
				try {
					entry.Key.Add(entry.Value);
					back++;
				} catch (Exception) {
					InternalLogger.LogInfo("[TRAP] Not In Mission");
				}
			}
			StarterSwap.Removed.Clear();
			SetEggFlags(UnitFlags.SwarmerOwned);
		}

		private static void SetEggFlags(bool on) {
			try {
				var eggs = Resources.FindObjectsOfTypeAll<EggSpawnerController>();
				if (eggs == null) {
					return;
				}
				for (int i = 0; i < eggs.Length; i++) {
					if (eggs[i] != null) {
						eggs[i].SwarmerSpawnOn = on;
					}
				}
			} catch (Exception e) {
				InternalLogger.LogWarning("[TRAP] egg flags faile " + e.Message);
			}
		}

		// leave mission

		private static void UpdateLeave(float now) {
			if (GameData.GameState != GameStates.InMission) {
				inMissionSince = -1.0f;
				return;
			}
			if (inMissionSince < 0.0f) {
				inMissionSince = now;
			}
			if (pendingLeaves > 0 && now - inMissionSince >= LeaveDelaySeconds && now - lastLeave >= LeaveCooldownSeconds) {
				pendingLeaves--;
				lastLeave = now;
				LeaveNow();
			}
		}

		private static void LeaveNow() {
			GameCoordinator gc = GameData.Coordinator;
			if (gc == null) {
				InternalLogger.LogWarning("[TRAP] LeaveMission: no GameCoordinator, trap lost");
				return;
			}
			InternalLogger.LogInfo("[TRAP] LeaveMission: calling MissionOver(false)");
			gc.MissionOver(false);
		}
	}
}
