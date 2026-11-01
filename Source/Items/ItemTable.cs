
// Excluded 62 Nutrient Goo, 64 Nutrient Harvest, 74 Sentience - not in the item pool
// Also holds the non talent items

using System;
using System.Collections.Generic;

namespace HarmonyModBuggos {
	public enum FillerStat { Health, Armor, Damage, AttackSpeed, MoveSpeed, Cost }
	public enum TrapKind { DamageZero, LeaveMission, LockSwarmer }

	public static class ItemTable {

		// Not a talent the Swarmer has no EvolutionID. Receiving it sets a flag (see ItemDispatcher).
		public const string SwarmerItemName = "The Swarmer";

		private static readonly Dictionary<string, FillerStat> Fillers = new Dictionary<string, FillerStat>(StringComparer.Ordinal) {
			{ "HP Increase", FillerStat.Health },
			{ "Armor Increase", FillerStat.Armor },
			{ "Damage Increase", FillerStat.Damage },
			{ "Attack Speed Increase", FillerStat.AttackSpeed },
			{ "Move Speed Increase", FillerStat.MoveSpeed },
			{ "Cost Decrease", FillerStat.Cost },
		};

		private static readonly Dictionary<string, TrapKind> Traps = new Dictionary<string, TrapKind>(StringComparer.Ordinal) {
			{ "Set Damage to Zero for 30s", TrapKind.DamageZero },
			{ "Leave Current Mission", TrapKind.LeaveMission },
			{ "Lock Swarmer for 2m", TrapKind.LockSwarmer },
		};

		// 127 entries. Key = item name as the server reports it (ItemInfo.ItemDisplayName), value = EvolutionID.
		private static readonly Dictionary<string, int> Talents = new Dictionary<string, int>(StringComparer.Ordinal) {
			{ "Adrenaline", 0 },
			{ "Aggressive Fertilization", 1 },
			{ "A Mother's Touch", 2 },
			{ "Armor Shredding", 3 },
			{ "Artistic Zen", 4 },
			{ "Best Parent", 5 },
			{ "Boom Slug Explosion Radius", 6 },
			{ "Boom Slug Explosion Radius II", 7 },
			{ "Boom Slug Influence", 8 },
			{ "Boom Slug Move Speed", 9 },
			{ "Boom Slug Move Speed II", 10 },
			{ "Boom Slug Thicker Skin", 11 },
			{ "Builder Influence", 12 },
			{ "Builder Legs", 13 },
			{ "Goo on Death", 14 },
			{ "Burrow Deeper", 15 },
			{ "Carnivorous Floor Traps", 16 },
			{ "Caustic Chemicals", 17 },
			{ "Chew Faster", 18 },
			{ "Chitinous Plates", 19 },
			{ "Compressed Acid Glands", 20 },
			{ "Construction Squad", 21 },
			{ "Corrosive Chemicals", 22 },
			{ "Dash Attack", 23 },
			{ "Defender Influence", 24 },
			{ "Defender Legs", 25 },
			{ "Defensive Goo Tactics", 26 },
			{ "Double Spines", 27 },
			{ "Efficient Builders", 28 },
			{ "Egg Twins", 29 },
			{ "Enormous Belly", 30 },
			{ "Enormous Belly II", 31 },
			{ "Enormous Swarm Capacity", 32 },
			{ "Extreme Swarm Capacity", 33 },
			{ "Fast Twitch Muscles", 34 },
			{ "Final Farewell", 35 },
			{ "Flesh Walls", 36 },
			{ "Flesh Wall Armor", 37 },
			{ "Flesh Wall Health", 38 },
			{ "Frenzied Egg Laying", 39 },
			{ "Greater Compression", 40 },
			{ "Greater Goo", 41 },
			{ "Healthy Digestion", 42 },
			{ "Hexagonal Breeding", 43 },
			{ "Hooked Spines", 44 },
			{ "Huge Spawn Size", 45 },
			{ "Huge Swarm Capacity", 46 },
			{ "Hunker Down", 47 },
			{ "Infestation", 48 },
			{ "Infestation II", 49 },
			{ "Inorganic Nutrient Harvest", 50 },
			{ "Insane Spawn Size", 51 },
			{ "Insane Swarm Capacity", 52 },
			{ "Interlocking Plates", 53 },
			{ "Large Spawn Size", 54 },
			{ "Large Swarm Capacity", 55 },
			{ "Massive Swarm Capacity", 56 },
			{ "Medium Spawn Size", 57 },
			{ "Medium Swarm Capacity", 58 },
			{ "More Acidic Spit", 59 },
			{ "More Motherly", 60 },
			{ "More Pus", 61 },
			{ "Nutrient Gooier", 63 },
			{ "Nutrient Pustules", 65 },
			{ "Offensive Goo Tactics", 66 },
			{ "Organic Nutrient Harvest", 67 },
			{ "Overeating", 68 },
			{ "Rapid Egg Laying", 69 },
			{ "Razor Spines", 70 },
			{ "Rebuilding", 71 },
			{ "Regen Aura", 72 },
			{ "Selective Breeding Program", 73 },
			{ "Speed Goo", 75 },
			{ "Speed Hooks", 76 },
			{ "Spiked Carapace", 77 },
			{ "Spiked Carapace II", 78 },
			{ "Spiner Health", 79 },
			{ "Spiner Influence", 80 },
			{ "Spiner Legs", 81 },
			{ "Spiner Range I", 82 },
			{ "Spiner Range II", 83 },
			{ "Spore Artillery", 84 },
			{ "Spore Launchers", 85 },
			{ "Spore Launchers Range", 86 },
			{ "Spore Launchers Range II", 87 },
			{ "Starting Nutrients", 88 },
			{ "Starting Nutrients II", 89 },
			{ "Starting Nutrients III", 90 },
			{ "Starting Nutrients IV", 91 },
			{ "Starting Nutrients V", 92 },
			{ "Swarm Mother Influence", 93 },
			{ "Swarm Mother Legs", 94 },
			{ "Swarm Attack Speed", 95 },
			{ "Swarm Attack Speed II", 96 },
			{ "Swarm Attack Speed III", 97 },
			{ "Swarmer Armor", 98 },
			{ "Swarmer Guidance", 99 },
			{ "Swarmer Health", 100 },
			{ "Swarmer Influence", 101 },
			{ "Swarm Health", 102 },
			{ "Swarm Health II", 103 },
			{ "Swarm Health III", 104 },
			{ "Swarm Speed", 105 },
			{ "Tearing Claws", 106 },
			{ "The Defender", 107 },
			{ "The Spiner", 108 },
			{ "The Wasp", 109 },
			{ "The Boom Slug", 110 },
			{ "The Builder", 111 },
			{ "The Swarm Mother", 112 },
			{ "The Titan", 113 },
			{ "The Warrior", 114 },
			{ "Titan Legs", 115 },
			{ "Titan Influence", 116 },
			{ "Twin Juices", 117 },
			{ "Uber Swarm Capacity", 118 },
			{ "Warrior Endurance", 119 },
			{ "Warrior Influence", 120 },
			{ "Warrior Legs", 121 },
			{ "Wasp Influence", 122 },
			{ "Wasp Legs", 123 },
			{ "Well Fed", 124 },
			{ "Nutrient Recycle", 125 },
			{ "Building Regeneration", 126 },
			{ "Survival Instinct", 127 },
			{ "Wasp Infestation", 128 },
			{ "Radiation Resistant", 129 },
		};

		public static bool TryGetTalent(string itemName, out int talentId) {
			talentId = -1;
			if (itemName == null) {
				return false;
			}
			return Talents.TryGetValue(itemName.Trim(), out talentId);
		}

		public static bool IsSwarmer(string itemName) {
			return itemName != null && string.Equals(itemName.Trim(), SwarmerItemName, StringComparison.Ordinal);
		}

		public static bool TryGetFiller(string itemName, out FillerStat stat) {
			stat = FillerStat.Health;
			return itemName != null && Fillers.TryGetValue(itemName.Trim(), out stat);
		}
		
		public static float FillerStep(FillerStat stat) {
			switch (stat) {
				case FillerStat.Health: return 1f;
				case FillerStat.Armor: return 1f;
				case FillerStat.Damage: return 1f;
				case FillerStat.AttackSpeed: return 0.2f;
				case FillerStat.MoveSpeed: return 0.2f;
				case FillerStat.Cost: return -1f;
				default: return 0f;
			}
		}

		public static bool TryGetTrap(string itemName, out TrapKind trap) {
			trap = TrapKind.DamageZero;
			return itemName != null && Traps.TryGetValue(itemName.Trim(), out trap);
		}

		public static int TalentCount {
			get { return Talents.Count; }
		}
	}
}
