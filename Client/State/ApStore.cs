using BepInEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using UnityEngine;

namespace HarmonyModBuggos
{
	public class PendingItem
	{
		public string name { get; set; } = "";
		public bool FromAnother { get; set; } = false;
	}

	public class ApStoreData
	{
		public string Slot { get; set; } = "";
		public string Seed { get; set; } = "";
		public int ItemIndex { get; set; } = 0;
		public bool SwarmerOwned { get; set; } = false;
		public Dictionary<string, int> FillerCounts { get; set; } = new Dictionary<string, int>();
		public List<PendingItem> PendingItems { get; set; } = new List<PendingItem>();
		public List<string> SentLocations { get; set; } = new List<string>();
		public List<int> ClearedAreas { get; set; } = new List<int>();
	}

	public static class ApStore
	{
		public static ApStoreData Data = new ApStoreData();
		public static bool IsLoaded { get; private set; } = false;

		private const float WriteInterval = 1.0f;
		private static string path = null;
		private static bool dirty = false;
		private static float lastWrite = 0.0f;

		// previous = the session whose live state belongs to the slot being left (can be null).
		public static bool Load(string slot, string seed, ApSession previous = null)
		{
			if (IsLoaded && Data.Seed == seed && Data.Slot == slot)
			{
				return false;
			}

			Flush(previous);

			string pathname = slot + "_" + seed;
			foreach (char c in Path.GetInvalidFileNameChars())
			{
				pathname = pathname.Replace(c, '_');
			}
			path = Path.Combine(Paths.BepInExRootPath, "BuggosAPWorld", pathname + ".json");

			ApStoreData loadedFile = null;

			try
			{
				if (File.Exists(path))
				{
					loadedFile = JsonSerializer.Deserialize<ApStoreData>(File.ReadAllText(path));
				}
			}
			catch (Exception e)
			{
				InternalLogger.LogWarning("[STORE] could not read " + path + ", starting anew: " + e.Message);
				try { File.Move(path, path + ".bad", true); } catch (Exception) { }
				loadedFile = null;
			}

			Data = loadedFile ?? new ApStoreData();

			if (Data.FillerCounts == null) { Data.FillerCounts = new Dictionary<string, int>(); }
			if (Data.PendingItems == null) { Data.PendingItems = new List<PendingItem>(); }
			if (Data.SentLocations == null) { Data.SentLocations = new List<string>(); }
			if (Data.ClearedAreas == null) { Data.ClearedAreas = new List<int>(); }

			Data.Slot = slot;
			Data.Seed = seed;
			dirty = false;
			IsLoaded = true;

			InternalLogger.LogInfo("loaded " + path);

			FillerBoosts.Rebuild();

			return true;
		}

		public static List<(string, bool)> GetPending()
		{   // returns a defensive copy
			var list = new List<(string, bool)>();
			foreach (var p in Data.PendingItems)
			{
				list.Add((p.name, p.FromAnother));
			}
			return list;
		}

		public static void MarkDirty()
		{
			dirty = true;
		}

		public static void Save()
		{
			if (!IsLoaded || path == null)
			{
				return;
			}
			lastWrite = Time.unscaledTime;
			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(path));
				string temp = path + ".tmp";

				File.WriteAllText(temp, JsonSerializer.Serialize(Data, new JsonSerializerOptions { WriteIndented = true }));

				File.Move(temp, path, true);
				dirty = false;   
			}
			catch (Exception e)
			{
				InternalLogger.LogWarning("save failed: " + e.Message);
			}
		}

		public static void Tick(ApSession s)
		{
			if (!IsLoaded)
			{
				return;
			}
			Sync(s);
			if (dirty && Time.unscaledTime - lastWrite >= WriteInterval)
			{
				Save();
			}
		}

		public static void Flush(ApSession s)
		{
			if (!IsLoaded)
			{
				return;
			}
			Sync(s);
			if (dirty)
			{
				Save();
			}
		}

		// Copies the session's live bookmark and queue into Data, together, when either differs from what Data holds.
		private static void Sync(ApSession session)
		{
			if (session == null)
			{
				return;
			}
			if (session.SavedItemIndex == Data.ItemIndex && !QueueDiffers(session.ItemsQueue))
			{
				return;
			}
			Data.ItemIndex = session.SavedItemIndex;
			Data.PendingItems.Clear();
			foreach (var item in session.ItemsQueue)
			{
				Data.PendingItems.Add(new PendingItem { name = item.Item1, FromAnother = item.Item2 });
			}
			dirty = true;
		}

		private static bool QueueDiffers(List<(string, bool)> queue)
		{
			if (queue.Count != Data.PendingItems.Count)
			{
				return true;
			}
			for (int i = 0; i < queue.Count; i++)
			{
				if (queue[i].Item1 != Data.PendingItems[i].name || queue[i].Item2 != Data.PendingItems[i].FromAnother)
				{
					return true;
				}
			}
			return false;
		}

		public static bool HasSent(string key)
		{
			return Data.SentLocations.Contains(key);
		}

		public static void MarkSent(string key)
		{
			if (!HasSent(key))
			{
				Data.SentLocations.Add(key);
				dirty = true;
				Save();
			}
		}

		public static bool IsFirstCleared(int areaId)
		{
			return Data.ClearedAreas.Contains(areaId);
		}

		public static void MarkFirstCleared(int areaId)
		{
			if (!IsFirstCleared(areaId))
			{
				Data.ClearedAreas.Add(areaId);
				dirty = true;
				Save();
			}
		}
	}

	public static class UnitFlags
	{
		public static bool SwarmerOwned
		{
			get { return ApStore.Data.SwarmerOwned; }
			set
			{
				if (!ApStore.IsLoaded)
				{
					InternalLogger.LogWarning("SwarmerOwned set before loaded, ignored");
					return;
				}
				if (ApStore.Data.SwarmerOwned != value)
				{
					ApStore.Data.SwarmerOwned = value;
					ApStore.MarkDirty();
				}
			}
		}
	}
}