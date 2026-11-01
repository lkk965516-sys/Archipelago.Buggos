
using System;
using System.Collections.Generic;

namespace HarmonyModBuggos {
	public static class LocationTable {

		public const int FirstArea = 1;
		public const int LastArea = 54;

		// These formats match the Python world
		public const string ClearFormat = "{0} Conquered";
		public const string PointFormat = "{0} Evo Point {1}";

		// (AreaID, level name, evolution points).
		private static readonly (int id, string name, int pts)[] Rows = {
			(1, "Scum Laden Outpost", 1),
			(2, "Filthy Peasant Village", 2),
			(3, "Defensive Dirt Town", 2),
			(4, "Disgusting Hamlet", 2),
			(5, "Regrettable Looking City", 3),
			(6, "Protective Trash Heap", 2),
			(7, "Makeshift Barricade", 2),
			(8, "Fortified Settlement", 2),
			(9, "Rat-Like Large City", 2),
			(10, "Shifty Military Outpost", 3),
			(11, "Tasty looking Morsel", 2),
			(12, "Simpleton Housing", 1),
			(13, "Forward Military Base", 3),
			(14, "Very large Peasant City", 2),
			(15, "Cockroach Town", 1),
			(16, "Hidden Flea Village", 2),
			(17, "Curious Technology Center", 2),
			(18, "Fortified Military Command Center", 2),
			(19, "High Tech Human City", 2),
			(20, "Human Garbage Village", 1),
			(21, "Civilian Power Reactor", 2),
			(22, "Military Weapon Facility", 3),
			(23, "Creepy Technology Center", 2),
			(24, "Simple Military Outpost", 2),
			(25, "Human Government Capital", 2),
			(26, "Pathetic Fusion Reactor", 2),
			(27, "High Security Outpost", 1),
			(28, "Gross Residential Compound", 2),
			(29, "Affluent Human City", 3),
			(30, "Inferior Human Religious Site", 2),
			(31, "Sad Military Capital", 2),
			(32, "Last Human City", 2),
			(33, "Quivering Human Bastion", 2),
			(34, "Emergency Distress Beacon", 2),
			(35, "Impressive Explosives Facility", 2),
			(36, "Communications Relay", 2),
			(37, "Arrival", 1),
			(38, "Listening Post", 1),
			(39, "Barracks", 2),
			(40, "Logistics Hub", 2),
			(41, "Landing Pad", 2),
			(42, "Forward Command", 3),
			(43, "Solar Arrays", 2),
			(44, "Mech Processing", 2),
			(45, "Mile 3 Fortification", 2),
			(46, "Radio Labs", 2),
			(47, "Robot Testing Facility", 2),
			(48, "Drop Pods", 2),
			(49, "Final Military Compound", 2),
			(50, "Medical Center", 2),
			(51, "Radiation Wall", 2),
			(52, "Research Lab", 2),
			(53, "Energy Generator", 2),
			(54, "Invasion Ship", 2)
		};

		private static readonly Dictionary<int, string> names = new Dictionary<int, string>();
		private static readonly Dictionary<int, int> points = new Dictionary<int, int>();

		// True once it has matched every name against the server.
		public static bool AllResolved { get; private set; } = false;

		static LocationTable() {
			foreach (var r in Rows) {
				names[r.id] = r.name;
				points[r.id] = r.pts;
			}
		}

		public static bool IsLevel(int areaId) {
			return names.ContainsKey(areaId);
		}

		public static int PointCount(int areaId) {
			int p;
			return points.TryGetValue(areaId, out p) ? p : 0;
		}

		// ---- Internal keys

		public static string ClearKey(int areaId) {
			return "c" + areaId;
		}

		public static string PointKey(int areaId, int n) {
			return "p" + areaId + "_" + n;
		}

		// a key every clear, then each evolution point.
		public static List<string> KeysFor(int areaId, int evoPoints) {
			var keys = new List<string>();

			keys.Add(ClearKey(areaId));

			for (int n = 1; n <= evoPoints; n++) {
				keys.Add(PointKey(areaId, n));
			}

			return keys;
		}

		// All keys for validation.
		public static List<string> AllKeys() {
			var keys = new List<string>();

			foreach (var r in Rows) {
				keys.AddRange(KeysFor(r.id, r.pts));
			}

			return keys;
		}

		public static string NameForKey(string key) {
			if (string.IsNullOrEmpty(key) || key.Length < 2) {
				return null;
			}

			if (key[0] == 'c') {
				int area;

				if (!int.TryParse(key.Substring(1), out area)) {
					return null;
				}

				string levelName;
				if (names.TryGetValue(area, out levelName)) {
					return string.Format(ClearFormat, levelName);
				}
			}

			if (key[0] == 'p') {
				string[] parts = key.Substring(1).Split('_');

				if (parts.Length != 2) {
					return null;
				}

				int area;
				int n;

				if (!int.TryParse(parts[0], out area) || !int.TryParse(parts[1], out n)) {
					return null;
				}

				string levelName;
				if (names.TryGetValue(area, out levelName)) {
					return string.Format(PointFormat, levelName, n);
				}
			}

			return null;
		}

		// Checks every generated location name against the Archipelago server.

		public static void Resolve(ApSession s) {
			try {
				var missing = new List<string>();
				int resolved = 0;
				int total = 0;

				foreach (string key in AllKeys()) {
					total++;

					string name = NameForKey(key);

					if (name != null && s.LocationId(name) > 0) {
						resolved++;
					} else {
						missing.Add(name ?? key);
					}
				}

				AllResolved = missing.Count == 0;

				InternalLogger.LogInfo(
					"[LOCATIONS] " + resolved + " of " + total + " names resolved"
				);

				if (!AllResolved) {
					InternalLogger.LogWarning(
						"[LOCATIONS] " + missing.Count + " names do NOT exist on the server (checks for them will be skipped)"
					);

					InternalLogger.LogWarning(
						"[LOCATIONS] Check the level names and location formats against the Python world's"
					);
				}
			} catch (Exception e) {
				AllResolved = false;
				InternalLogger.LogError("[LOCATIONS] Resolve failed: " + e);
			}
		}
	}
}