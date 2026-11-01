// Used a FF1 Mod as a a base and then deleted and edited to function with my other files, may rebuild later

using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace HarmonyModBuggos {
	public class ApSession {

		public const string GameName = "Buggos";

		public bool connected {
			get { return session != null ? session.Socket.Connected : false; }
		}

		public ArchipelagoSession session;
		private IEnumerator<bool> incomingItemHandler;
		private IEnumerator<bool> checkItemsReceived;
		private ConcurrentQueue<(ItemInfo ItemInfo, int index)> incomingItems;

		public Dictionary<string, object> slotData;
		public bool sentCompletion = false;
		public int ItemIndex = 0;
		public bool previousConnectionStatus = false;

		private Version archipelagoVersion = new Version("0.6.2");

		public int SavedItemIndex = 0;
		public List<(string, bool)> ItemsQueue = new List<(string, bool)>();

		public void Update()
		{
			if (!connected)
			{
				if (previousConnectionStatus)
				{
					InternalLogger.LogWarning("Archipelago: Lost connection to Archipelago! Unable to send or receive items. Re-connect and try again.");
				}

				previousConnectionStatus = connected;
				return;
			}

			previousConnectionStatus = connected;

			if (checkItemsReceived != null)
			{
				checkItemsReceived.MoveNext();
			}

			if (incomingItemHandler != null)
			{
				incomingItemHandler.MoveNext();
			}
		}

		public string TryConnect() {

			string Host;
			int Port;

			if (!TryParseServer(ApConfig.Server.Value, out Host, out Port)) {
				return "Server must look like host:port";
			}

			if (connected && ApConfig.Slotname.Value == session.Players.GetPlayerName(session.ConnectionInfo.Slot)) {
				return "";
			}

			TryDisconnect();

			LoginResult LoginResult;

			if (session == null) {
				try {
					session = ArchipelagoSessionFactory.CreateSession(Host, Port);
				} catch (Exception e) {
					InternalLogger.LogError("Failed to create archipelago session!");
					InternalLogger.LogError(e.GetBaseException().Message);
				}
			}

			incomingItemHandler = IncomingItemHandler();
			checkItemsReceived = CheckItemsReceived();
			incomingItems = new ConcurrentQueue<(ItemInfo ItemInfo, int index)>();

			try {
				LoginResult = session.TryConnectAndLogin(GameName, ApConfig.Slotname.Value, ItemsHandlingFlags.AllItems, version: archipelagoVersion, requestSlotData: true, password: ApConfig.Password.Value);
			} catch (Exception e) {
				LoginResult = new LoginFailure(e.GetBaseException().Message);
			}

			if (LoginResult is LoginSuccessful LoginSuccess)
			{
				slotData = LoginSuccess.SlotData;

				// Load saved state for this run
				if (ApStore.Load(ApConfig.Slotname.Value, session.RoomState.Seed)) {
					SavedItemIndex = ApStore.Data.ItemIndex;
					ItemsQueue.Clear();
					ItemsQueue.AddRange(ApStore.GetPending());
				}

				SaveSwapper.OnLogin(session.RoomState.Seed);

				// Start from the saved item bookmark.
				ItemIndex = SavedItemIndex;

				InternalLogger.LogInfo("Successfully connected to Archipelago Multiworld server!");

				foreach (var entry in slotData)
				{
					InternalLogger.LogTesting($"slot_data {entry.Key}: {entry.Value}");
				}

			} else {
				LoginFailure loginFailure = (LoginFailure)LoginResult;
				InternalLogger.LogWarning("Error connecting to Archipelago:");

				string BottomLine = "\"Check your settings and/or log output.\"";

				foreach (string Error in loginFailure.Errors)
				{
					InternalLogger.LogWarning($"{Error}");
					BottomLine = $"\"{Error}\"";
				}

				foreach (ConnectionRefusedError Error in loginFailure.ErrorCodes)
				{
					InternalLogger.LogWarning(Error.ToString());
				}

				TryDisconnect();
				return BottomLine;
			}

			return "";
		}

		public void TryDisconnect() {

			try {
				if (connected) {
					InternalLogger.LogInfo("Disconnected from Archipelago");
					previousConnectionStatus = false;
				}

				if (session != null) {
					session.Socket.DisconnectAsync();
					session = null;
				}

				incomingItemHandler = null;
				checkItemsReceived = null;
				incomingItems = new ConcurrentQueue<(ItemInfo ItemInfo, int ItemIndex)>();
				slotData = null;
				ItemIndex = 0;

				// SavedItemIndex survives a disconnect.
			} catch (Exception e) {
				InternalLogger.LogWarning("Encountered an error disconnecting from Archipelago! " + e);
			}
		}

		private IEnumerator<bool> CheckItemsReceived() {
			while (connected) {
				while (session.Items.AllItemsReceived.Count > ItemIndex) {
					ItemInfo ItemInfo = session.Items.AllItemsReceived[ItemIndex];

					InternalLogger.LogTesting("Placing item " + ItemInfo.ItemDisplayName + " with index " + ItemIndex + " in queue.");

					incomingItems.Enqueue((ItemInfo, ItemIndex));
					ItemIndex++;
				}

				yield return true;
			}
		}

		private IEnumerator<bool> IncomingItemHandler() {

			while (connected) {

				if (!incomingItems.TryPeek(out var pendingItem)) {
					yield return true;
					continue;
				}

				InternalLogger.LogInfo($"Item incoming: {pendingItem.ItemInfo.ItemDisplayName}");

				var itemInfo = pendingItem.ItemInfo;
				var itemName = itemInfo.ItemDisplayName;
				var itemDisplayName = itemName + " (" + itemInfo.ItemId + ") at index " + pendingItem.index;

				ItemsQueue.Add((itemName, ApConfig.Slotname.Value != itemInfo.Player.Name));

				InternalLogger.LogInfo("Received " + itemDisplayName + " from " + itemInfo.Player.Name + " at " + itemInfo.LocationDisplayName);

				incomingItems.TryDequeue(out _);
				SavedItemIndex = pendingItem.index + 1;

				yield return true;
			}
		}

		// Server ID of a location name, or -1 if unknown or not connected.
		public long LocationId(string name) {
			try {
				if (!connected || string.IsNullOrEmpty(name)) {
					return -1;
				}

				return session.Locations.GetLocationIdFromName(GameName, name);
			} catch (Exception) {
				return -1;
			}
		}

		// Sends a batch of location names
		public int SendLocationNames(List<string> names) {
			if (!connected || names == null || names.Count == 0) {
				return 0;
			}

			var ids = new List<long>();

			foreach (string name in names) {
				long id = LocationId(name);

				if (id > 0) {
					ids.Add(id);
				} else {
					InternalLogger.LogWarning("Unknown location name, not sent: " + name);
				}
			}

			if (ids.Count > 0) {
				try {
					session.Locations.CompleteLocationChecks(ids.ToArray());
					InternalLogger.LogInfo("Sent " + ids.Count + " location check(s)");
				} catch (Exception e) {
					InternalLogger.LogWarning("Sending location checks failed (they stay recorded and are re-sent at the next login): " + e.Message);
					return 0;
				}
			}

			return ids.Count;
		}

		// A few real location names from the server, for validation
		public string SampleLocationNames(int count) {
			var sample = new List<string>();

			try {
				foreach (long id in session.Locations.AllLocations) {
					string n = session.Locations.GetLocationNameFromId(id, GameName);

					if (!string.IsNullOrEmpty(n)) {
						sample.Add("\"" + n + "\"");
					}

					if (sample.Count >= count) {
						break;
					}
				}
			} catch (Exception e) {
				return "(could not read names: " + e.Message + ")";
			}

			return sample.Count > 0 ? string.Join(", ", sample) : "(none found)";
		}

		public void SendCompletion() {
			if (!connected || sentCompletion) {
				return;
			}

			sentCompletion = true;
			session.SetGoalAchieved();
		}

		// Parses the host:port connection setting.
		public static bool TryParseServer(string server, out string host, out int port) {
			host = "";
			port = 38281;

			if (string.IsNullOrWhiteSpace(server)) {
				return false;
			}

			server = server.Trim();
			int colon = server.LastIndexOf(':');

			if (colon < 0) {
				host = server;
				return true;
			}

			host = server.Substring(0, colon);
			return host.Length > 0 && int.TryParse(server.Substring(colon + 1), out port);
		}
	}
}