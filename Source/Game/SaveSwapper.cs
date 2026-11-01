using BepInEx;
using System;
using System.IO;

namespace HarmonyModBuggos {
	public static class SaveSwapper {

		private const string SaveFileName = "PlayerSave.Bug";
		private const string VanillaProfile = "vanilla";

		private static string saveDir;
		private static bool ready;

		public static string ActiveProfile { get; private set; } = "";

		private static string BaseDir {
			get { return Path.Combine(Paths.BepInExRootPath, "BuggosAP"); }
		}

		private static string ProfilesDir {
			get { return Path.Combine(BaseDir, "SaveProfiles"); }
		}

		private static string ActiveProfileFile {
			get { return Path.Combine(BaseDir, "active_profile.txt"); }
		}


		public static void Run() {
			try {
				if (!Prepare()) {
					return;
				}

				if (!LoadActiveProfile()) {
					return;
				}

				if (!SwitchProfile(VanillaProfile)) {
					InternalLogger.LogWarning("[SAVES] Could not restore the vanilla save profile.");
				}
			} catch (Exception e) {
				InternalLogger.LogError("[SAVES] Save initialisation failed: " + e);
			}
		}


		public static void OnLogin(string seed) {
			if (string.IsNullOrWhiteSpace(seed)) {
				InternalLogger.LogWarning("[SAVES] Cannot select a save profile: the Archipelago seed is empty.");
				return;
			}

			if (string.IsNullOrWhiteSpace(ApConfig.Slotname.Value)) {
				InternalLogger.LogWarning("[SAVES] Cannot select a save profile: the Archipelago slot name is empty.");
				return;
			}

			ActivateProfile(ApConfig.Slotname.Value, seed);
		}

		// For the future UI.
		public static bool ActivateProfile(string slotName, string seed) {
			if (string.IsNullOrWhiteSpace(slotName) || string.IsNullOrWhiteSpace(seed)) {
				InternalLogger.LogWarning("[SAVES] A slot name and seed are required to select a save profile.");
				return false;
			}

			try {
				if (!Prepare() || !LoadActiveProfile()) {
					return false;
				}

				string profile = "slot_" + SafePart(slotName) + "_" + SafePart(seed);
				return SwitchProfile(profile);
			} catch (Exception e) {
				InternalLogger.LogError("[SAVES] Could not activate the requested profile: " + e);
				return false;
			}
		}

		// For the future UI.
		public static string GetActiveProfile() {
			return ActiveProfile;
		}

		// for when the game does not show one yet
		public static string CurrentSaveFileName() {
			try {
				if (!Prepare()) {
					return null;
				}
				return File.Exists(Path.Combine(saveDir, SaveFileName)) ? SaveFileName : null;
			} catch (Exception) {
				return null;
			}
		}

		private static bool Prepare() {
			if (ready) {
				return true;
			}

			string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			string appData = Path.GetDirectoryName(local);
			if (string.IsNullOrEmpty(appData)) {
				InternalLogger.LogWarning("[SAVES] Could not locate the Windows AppData folder.");
				return false;
			}

			saveDir = Path.Combine(appData, "LocalLow", "Intrepid Marmot", "Buggos");
			Directory.CreateDirectory(saveDir);
			Directory.CreateDirectory(ProfilesDir);
			ready = true;
			return true;
		}


		private static bool LoadActiveProfile() {
			if (!File.Exists(ActiveProfileFile)) {
				BackupOriginal();
				WriteActiveProfile(VanillaProfile);
				ActiveProfile = VanillaProfile;
				return true;
			}

			string profile = File.ReadAllText(ActiveProfileFile).Trim();
			if (string.IsNullOrEmpty(profile)) {
				profile = VanillaProfile;
				WriteActiveProfile(profile);
			}

			if (!IsSafeStoredProfile(profile)) {
				InternalLogger.LogError("[SAVES] The active profile file contains an invalid profile name. No save was moved.");
				return false;
			}

			ActiveProfile = profile;
			return true;
		}

		private static bool SwitchProfile(string wanted) {
			string current = ActiveProfile;
			if (string.IsNullOrEmpty(current)) {
				InternalLogger.LogWarning("[SAVES] No active save profile has been initialised.");
				return false;
			}

			if (string.Equals(current, wanted, StringComparison.OrdinalIgnoreCase)) {
				InternalLogger.LogInfo("[SAVES] Profile already active: " + wanted);
				return true;
			}

			string savePath = Path.Combine(saveDir, SaveFileName);
			string currentDir = Path.Combine(ProfilesDir, current);
			string wantedDir = Path.Combine(ProfilesDir, wanted);
			string currentProfileSave = Path.Combine(currentDir, SaveFileName);
			string wantedProfileSave = Path.Combine(wantedDir, SaveFileName);

	

			// Grabbed from a similar mod that moves files back and forth, never wrote it myself, just edited but makes sense
			try {
				Directory.CreateDirectory(currentDir);
				Directory.CreateDirectory(wantedDir);

				if (File.Exists(savePath)) {
					File.Move(savePath, currentProfileSave, true);
				}


				if (File.Exists(wantedProfileSave)) {
					File.Move(wantedProfileSave, savePath, true);
				}

				WriteActiveProfile(wanted);
				ActiveProfile = wanted;

				return true;
			} catch (Exception e) {
				InternalLogger.LogError("[SAVES] Could not switch " + current + " to " + wanted + ": " + e);
				return false;
			}
		}

		private static void BackupOriginal() {
			string savePath = Path.Combine(saveDir, SaveFileName);
			if (!File.Exists(savePath)) {
				return;
			}

			string backupDir = Path.Combine(BaseDir, "SaveBackup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"));
			Directory.CreateDirectory(backupDir);
			File.Copy(savePath, Path.Combine(backupDir, SaveFileName), false);
			InternalLogger.LogInfo("[SAVES] Original save backed up to " + backupDir);
		}

		private static void WriteActiveProfile(string profile) {
			string tempFile = ActiveProfileFile + ".tmp";
			File.WriteAllText(tempFile, profile);
			File.Move(tempFile, ActiveProfileFile, true);
		}

		private static string SafePart(string value) {
			char[] chars = value.Trim().ToCharArray();
			for (int i = 0; i < chars.Length; i++) {
				char c = chars[i];
				if (!char.IsLetterOrDigit(c) && c != '_' && c != '-') {
					chars[i] = '_';
				}
			}

			string result = new string(chars).Trim('_', '-');
			if (result.Length == 0) {
				return "default";
			}
			return result.Length > 64 ? result.Substring(0, 64) : result;
		}

		private static bool IsSafeStoredProfile(string profile) {
			if (profile == VanillaProfile) {
				return true;
			}
			foreach (char c in profile) {
				if (!char.IsLetterOrDigit(c) && c != '_' && c != '-') {
					return false;
				}
			}
			return true;
		}
	}
}
