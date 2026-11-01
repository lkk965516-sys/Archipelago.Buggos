using System;

namespace HarmonyModBuggos {
	public static class FillerBoosts {

		// switch for FilllerStats
		public static string KeyFor(FillerStat stat) {
			switch (stat) {
				case FillerStat.Health: return "hp";
				case FillerStat.Armor: return "armor";
				case FillerStat.Damage: return "dmg";
				case FillerStat.AttackSpeed: return "atk";
				case FillerStat.MoveSpeed: return "move";
				case FillerStat.Cost: return "cost";
				default: return "";
			}
		}

		// Called by ItemDispatcher
		public static void Add(FillerStat stat, float step) {
			string name = stat.ToString();
			int count;
			ApStore.Data.FillerCounts.TryGetValue(name, out count);
			count++;
			ApStore.Data.FillerCounts[name] = count;
			ApStore.MarkDirty();

			StatBoosts.SetTotal(KeyFor(stat), count * step);
			StatBoosts.ApplyAll("Filler");
		}

		// Called by ApStore.Load
		public static void Rebuild() {
			StatBoosts.ResetTotals();
			foreach (FillerStat stat in Enum.GetValues(typeof(FillerStat))) {
				int count;
				if (ApStore.Data.FillerCounts.TryGetValue(stat.ToString(), out count) && count > 0) {
					StatBoosts.SetTotal(KeyFor(stat), count * ItemTable.FillerStep(stat));
				}
			}
			StatBoosts.ApplyAll("FillerRebuild");
			InternalLogger.LogInfo("[FILLER] rebuilt from saved counts");
		}
	}
}
