

using System;
using UnityEngine;

namespace HarmonyModBuggos {
	public static class ConnectionWindow {

		private static bool visible = !ApConfig.ConnectOnStart;
		private static bool seeded = false;
		private static string server = "";
		private static string slot = "";
		private static string password = "";
		private static string status = "";
		private static bool connectRequested = false;
		private static int connectFrame = 0;
		private static int focus = -1;
		private static bool drawErrorLogged = false;

		public static void Update() {
			if (!seeded) {
				seeded = true;
				server = ApConfig.Server.Value;
				slot = ApConfig.Slotname.Value;
				password = ApConfig.Password.Value;
			}

			if (connectRequested && Time.frameCount > connectFrame) {
				connectRequested = false;
				Connect();
			}
		}

		private static void Connect() {
			try {
				ApConfig.Server.Value = server.Trim();
				ApConfig.Slotname.Value = slot.Trim();
				ApConfig.Password.Value = password;

				string error = ApClient.instance.Connect();
				status = string.IsNullOrEmpty(error) ? "Connected" : "Failed: " + error;
			} catch (Exception e) {
				status = "Failed: " + e.Message;
				InternalLogger.LogError("[UI] connect failed: " + e);
			}
		}

		private static void Disconnect() {
			ApClient.instance.Disconnect();
			status = "Disconnected";
		}

		// TextEditor is stripped in this game, so every IMGUI text field throws
		private static string TextBox(Rect r, string value, int id, bool mask) {
			Event ev = Event.current;

			if (GUI.enabled && ev.type == EventType.MouseDown) {
				if (r.Contains(ev.mousePosition)) {
					focus = id;
					ev.Use();
				} else if (focus == id) {
					focus = -1;
				}
			}

			if (GUI.enabled && focus == id && ev.type == EventType.KeyDown) {
				if ((ev.control || ev.command) && ev.keyCode == KeyCode.V) {
					try {
						string clip = GUIUtility.systemCopyBuffer;
						if (!string.IsNullOrEmpty(clip)) {
							value += clip.Replace("\r", "").Replace("\n", "");
						}
					} catch (Exception) { }
					ev.Use();
				} else if (ev.keyCode == KeyCode.Backspace) {
					if (value.Length > 0) {
						value = value.Substring(0, value.Length - 1);
					}
					ev.Use();
				} else if (ev.keyCode == KeyCode.Return || ev.keyCode == KeyCode.KeypadEnter || ev.keyCode == KeyCode.Escape || ev.keyCode == KeyCode.Tab) {
					focus = -1;
					ev.Use();
				} else if (ev.character >= ' ' && ev.character != (char)127) {
					value += ev.character;
					ev.Use();
				}
			}

			string shown = mask ? new string('*', value.Length) : value;
			if (focus == id && ((int)(Time.realtimeSinceStartup * 2f) % 2) == 0) {
				shown += "|";
			}
			GUI.Box(r, shown);
			return value;
		}

		public static void Draw() {
			Event ev = Event.current;
			if (ev != null && ev.type == EventType.KeyDown && focus < 0 && ev.keyCode == ApConfig.ToggleKey.Value) {
				visible = !visible;
				ev.Use();
			}

			if (!visible || !seeded) {
				return;
			}

			try {
				float w = 360f;
				float h = 250f;
				Rect box = new Rect(20f, 20f, w, h);
				GUI.Box(box, "Archipelago");

				float x = box.x + 12f;
				float y = box.y + 28f;
				float fw = w - 24f;
				float row = 22f;

				bool connected = ApClient.instance != null && ApClient.instance.IsConnected();
				GUI.enabled = !connectRequested;

				GUI.Label(new Rect(x, y, fw, row), "Server (host:port)");
				y += row;
				server = TextBox(new Rect(x, y, fw, row), server, 0, false);
				y += row + 4f;

				GUI.Label(new Rect(x, y, fw, row), "Slot name");
				y += row;
				slot = TextBox(new Rect(x, y, fw, row), slot, 1, false);
				y += row + 4f;

				GUI.Label(new Rect(x, y, fw, row), "Password");
				y += row;
				password = TextBox(new Rect(x, y, fw, row), password, 2, true);
				y += row + 8f;

				float bw = (fw - 8f) / 3f;
				if (GUI.Button(new Rect(x, y, bw, 26f), "Connect")) {
					connectRequested = true;
					connectFrame = Time.frameCount;
					status = "Connecting...";
					focus = -1;
				}
				if (connected && GUI.Button(new Rect(x + bw + 4f, y, bw, 26f), "Disconnect")) {
					Disconnect();
				}
				if (GUI.Button(new Rect(x + 2f * (bw + 4f), y, bw, 26f), "Close")) {
					visible = false;
					focus = -1;
				}
				y += 32f;

				GUI.enabled = true;

				string line;
				if (connected) {
					line = "Connected as " + ApConfig.Slotname.Value + ", " + ApClient.instance.integration.ItemsQueue.Count + " items pending";
				} else {
					line = string.IsNullOrEmpty(status) ? "Not connected" : status;
				}
				GUI.Label(new Rect(x, y, fw, row), line);
				y += row;
				GUI.Label(new Rect(x, y, fw, row), ApConfig.ToggleKey.Value + " shows or hides this window");
			} catch (Exception e) {
				GUI.enabled = true;
				if (!drawErrorLogged) {
					drawErrorLogged = true;
					InternalLogger.LogError("[UI] draw failed: " + e);
				}
			}
		}
	}
}