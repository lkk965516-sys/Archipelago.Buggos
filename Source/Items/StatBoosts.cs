
using HarmonyLib;
using System;
using System.Collections.Generic;

namespace HarmonyModBuggos {

	public static class StatBoosts {

		private static readonly string[] AllStats = { "hp", "armor", "dmg", "atk", "move", "cost" };


		private static readonly Dictionary<string, float> totals = new Dictionary<string, float>();


		private static readonly Dictionary<IntPtr, Dictionary<string, float>> applied = new Dictionary<IntPtr, Dictionary<string, float>>();


		private static readonly Dictionary<IntPtr, int> baseCost = new Dictionary<IntPtr, int>();

		public static bool HoldDamage = false;

		public static void SetTotal(string stat, float total) {
			totals[stat] = total;
		}

		// Sets every total back to 0. The next ApplyAll takes back what was applied. Used when the slot or seed changes.
		public static void ResetTotals() {
			foreach (var key in new List<string>(totals.Keys)) {
				totals[key] = 0f;
			}
		}

		private static float Total(string stat) {
			float t;
			return totals.TryGetValue(stat, out t) ? t : 0f;
		}

		public static void ApplyAll(string where) {
			try {
				var list = BuggosMaster.AllBugUnits;
				if (list == null || list.Count == 0) {
					return;   // nothing to apply to yet
				}

				for (int i = 0; i < list.Count; i++) {
					BugUnit u = list[i];
					if (u == null) {
						continue;
					}

					Dictionary<string, float> done;
					if (!applied.TryGetValue(u.Pointer, out done)) {
						applied[u.Pointer] = done = new Dictionary<string, float>();
						baseCost[u.Pointer] = u.NutrientCost;   // captured before anything changes it
					}

					bool logged = false;
					foreach (string stat in AllStats) {
						if (HoldDamage && stat == "dmg") {
							continue;
						}
						float target = Total(stat);
						float current;
						done.TryGetValue(stat, out current);
						if (target == current) {
							continue;
						}

						if (!logged) {
							logged = true;
							InternalLogger.LogInfo("[BOOST] (" + where + ") before " + u.name + ": hp=" + u.HealthMax + " armor=" + u.Armor
								+ " dmg=" + u.Damage + " atk=" + u.AttackSpeed + " move=" + u.MoveSpeed + " cost=" + u.NutrientCost);
						}

						Apply(u, stat, target - current, target);
						done[stat] = target;
					}

					if (logged) {
						InternalLogger.LogInfo("[BOOST] (" + where + ") after  " + u.name + ": hp=" + u.HealthMax + " armor=" + u.Armor
							+ " dmg=" + u.Damage + " atk=" + u.AttackSpeed + " move=" + u.MoveSpeed + " cost=" + u.NutrientCost);
					}
				}
			} catch (Exception e) {
				InternalLogger.LogError("[BOOST] apply failed: " + e);
			}
		}

		private static void Apply(BugUnit u, string stat, float delta, float target) {
			switch (stat) {
				case "hp": u.HealthMax += (int)delta; u.HealthCurrent += (int)delta; 
					break;

				case "armor": u.Armor += (int)delta; 
					break;

				case "dmg": u.Damage += (int)delta; 
					break;

				case "atk": u.AttackSpeed += delta; 
					break;

				case "move": u.MoveSpeed += delta; 
					break;

				case "cost": u.NutrientCost = Math.Max(1, baseCost[u.Pointer] + (int)target); 
					break;
					
				default: InternalLogger.LogInfo("[BOOST] unknown stat '" + stat + "'"); 
					break;
			}
		}
	}

	[HarmonyPatch(typeof(BugOvermind), nameof(BugOvermind.SetSpawnableBugs))]
	public static class BoostSpawnablePatch {
		public static void Postfix() {
			StatBoosts.ApplyAll("SetSpawnableBugs");
		}
	}
}
