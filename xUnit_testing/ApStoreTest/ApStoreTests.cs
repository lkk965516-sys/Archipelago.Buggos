
using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using Xunit;

namespace HarmonyModBuggos.Tests
{
	public class ApStoreTests : IDisposable
	{
		private readonly string root;

		public ApStoreTests()
		{
			root = Path.Combine(Path.GetTempPath(), "apstore_tests_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(root);
			Paths.BepInExRootPath = root;
		}

		public void Dispose()
		{
			try { Directory.Delete(root, true); } catch (Exception) { }
		}

		private string FilePath(string slot, string seed)
		{
			return Path.Combine(root, "BuggosAPWorld", slot + "_" + seed + ".json");
		}

		// Simulates a restart
		private void Restart(string slot, string seed)
		{
			ApStore.Load("zz_other", "zz_seed");
			ApStore.Load(slot, seed);
		}

		[Fact]
		public void Load_FreshSlot_ReturnsTrueWithEmptyState()
		{
			bool loaded = ApStore.Load("fresh", "seed1");

			Assert.True(loaded);
			Assert.True(ApStore.IsLoaded);
			Assert.Equal(0, ApStore.Data.ItemIndex);
			Assert.Empty(ApStore.Data.PendingItems);
			Assert.Empty(ApStore.Data.SentLocations);
			Assert.Empty(ApStore.Data.ClearedAreas);
		}

		[Fact]
		public void Load_SameSlotAndSeedTwice_SecondCallReturnsFalse()
		{
			ApStore.Load("twice", "seed1");

			Assert.False(ApStore.Load("twice", "seed1"));
		}

		[Fact]
		public void MarkSent_And_MarkFirstCleared_SurviveRestart()
		{
			ApStore.Load("sent", "seed1");
			ApStore.MarkSent("c12");
			ApStore.MarkFirstCleared(12);

			Restart("sent", "seed1");

			Assert.True(ApStore.HasSent("c12"));
			Assert.True(ApStore.IsFirstCleared(12));
		}

		[Fact]
		public void Load_CorruptFile_MovesToBadAndStartsFresh()
		{
			string file = FilePath("corrupt", "seed1");
			Directory.CreateDirectory(Path.GetDirectoryName(file));
			File.WriteAllText(file, "{ this is not json");

			bool loaded = ApStore.Load("corrupt", "seed1");

			Assert.True(loaded);
			Assert.True(File.Exists(file + ".bad"));
			Assert.Equal("corrupt", ApStore.Data.Slot);
			Assert.Empty(ApStore.Data.SentLocations);
		}

		[Fact]
		public void Flush_BookmarkAndQueue_SurviveRestart()
		{
			ApStore.Load("queue", "seed1");
			var session = new ApSession();
			session.SavedItemIndex = 3;
			session.ItemsQueue.Add(("Adrenaline", false));
			session.ItemsQueue.Add(("HP Increase", true));

			ApStore.Flush(session);
			Restart("queue", "seed1");

			Assert.Equal(3, ApStore.Data.ItemIndex);
			Assert.Equal(2, ApStore.Data.PendingItems.Count);
			Assert.Equal("Adrenaline", ApStore.Data.PendingItems[0].name);
			Assert.Equal("HP Increase", ApStore.Data.PendingItems[1].name);
			Assert.True(ApStore.Data.PendingItems[1].FromAnother);
		}

		// Regression test for the old length-only change detection
		[Fact]
		public void Flush_QueueSameLengthDifferentContent_IsSaved()
		{
			ApStore.Load("swap", "seed1");
			var session = new ApSession();
			session.SavedItemIndex = 1;
			session.ItemsQueue.Add(("A", false));
			ApStore.Flush(session);

			session.ItemsQueue[0] = ("B", false);
			ApStore.Flush(session);
			Restart("swap", "seed1");

			Assert.Single(ApStore.Data.PendingItems);
			Assert.Equal("B", ApStore.Data.PendingItems[0].name);
		}
	}

}

namespace HarmonyModBuggos
{


	public static class InternalLogger
	{
		public static void LogInfo(object message) { }
		public static void LogWarning(object message) { }
	}

	public static class FillerBoosts
	{
		public static void Rebuild() { }
	}

	public class ApSession
	{
		public int SavedItemIndex = 0;
		public List<(string, bool)> ItemsQueue = new List<(string, bool)>();
	}
}

namespace BepInEx
{
	public static class Paths
	{
		public static string BepInExRootPath { get; set; } = "";
	}
}

namespace UnityEngine
{
	public static class Time
	{
		public static float unscaledTime = 0.0f;
	}
}