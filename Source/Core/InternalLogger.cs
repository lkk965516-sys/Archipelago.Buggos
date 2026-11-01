using BepInEx.Logging;
using System;

namespace HarmonyModBuggos {
	public static class InternalLogger {

		private static ManualLogSource logger = null;

		public static void SetLogger(ManualLogSource source) {
			logger = source;
		}

		public static void LogInfo(object message) {
			if (logger != null) {
				logger.LogInfo(message);
			} else {
				Console.WriteLine("[Info] " + message);
			}
		}

		public static void LogWarning(object message) {
			if (logger != null) {
				logger.LogWarning(message);
			} else {
				Console.WriteLine("[Warning] " + message);
			}
		}

		public static void LogError(object message) {
			if (logger != null) {
				logger.LogError(message);
			} else {
				Console.WriteLine("[Error] " + message);
			}
		}

		public static void LogTesting(object message) {
			LogInfo("[TEST] " + message);
		}
	}
}
