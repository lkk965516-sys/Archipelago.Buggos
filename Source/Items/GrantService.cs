using System;
using System.Collections.Generic;

namespace HarmonyModBuggos {

	public enum GrantResult {
		Granted,  
		AlreadyOwned, 
		Retry,      
		NotFound     
	}

	public static class GrantService {

		// lets purchases while true
		public static bool Allow = false;

		public static GrantResult GrantNow(int talentId) {
			try {
				return GrantNowMain(talentId);
			} catch (Exception e) {
				InternalLogger.LogError("[GRANT] failed for id" + talentId + ": " + e);
				return GrantResult.Retry;
			}
		}

		private static GrantResult GrantNowMain(int talentId) {
			EvolutionTalent talent = FindTalent(talentId);
			if (talent == null) {
				InternalLogger.LogWarning("[GRANT] no talent with id " + talentId);
				return GrantResult.NotFound;
			}
			if (talent.TalentHasBeenPurchased) {
				return GrantResult.AlreadyOwned;
			}

			EvolutionManagerFinal mgr = EvolutionManagerFinal.Instance;
			bool screenLoaded = mgr != null && mgr.AllEvolutionNodes != null && mgr.AllEvolutionNodes.Count > 0;

			if (screenLoaded) {
				EvolutionNode node = FindNode(mgr, talentId);
				if (node != null) {
					GrantViaManager(mgr, node);
					if (talent.TalentHasBeenPurchased) {
						InternalLogger.LogInfo("[GRANT] bought talent " + talentId);
						return GrantResult.Granted;
					}
					InternalLogger.LogWarning("[GRANT] purchase of " + talentId + " did not suc");
				}
			}

			return GrantDataOnly(talent);
		}

		private static EvolutionTalent FindTalent(int talentId) {
			var talents = BuggosMaster.AllEvolutionsTalents;
			if (talents == null) {
				return null;
			}
			for (int i = 0; i < talents.Count; i++) {
				if (talents[i] != null && talents[i].EvolutionID == talentId) {
					return talents[i];
				}
			}
			return null;
		}

		private static EvolutionNode FindNode(EvolutionManagerFinal mgr, int talentId) {
			var nodes = mgr.AllEvolutionNodes;
			for (int i = 0; i < nodes.Count; i++) {
				if (nodes[i] != null && nodes[i].ThisTalent != null && nodes[i].ThisTalent.EvolutionID == talentId) {
					return nodes[i];
				}
			}
			return null;
		}


		private static void GrantViaManager(EvolutionManagerFinal mgr, EvolutionNode node) {
			var spoofed = new List<EvolutionTalent>();
			CollectUnownedAncestors(node, new HashSet<int> { node.ThisTalent.EvolutionID }, spoofed);

			Allow = true;
			try {
				foreach (var t in spoofed) {
					t.TalentHasBeenPurchased = true;
				}
				mgr.SetEvolutionPoints(node.ThisTalent.TalentCost);
				mgr.PurchaseThisNode(node);
				mgr.SetEvolutionPoints(0);
			} finally {
				foreach (var t in spoofed) {
					t.TalentHasBeenPurchased = false;
				}
				Allow = false;
			}
		}


		private static void CollectUnownedAncestors(EvolutionNode node, HashSet<int> seen, List<EvolutionTalent> unowned) {
			var parents = node.ParentNodes;
			if (parents == null) {
				return;
			}
			for (int i = 0; i < parents.Count; i++) {
				var p = parents[i];
				var t = p != null ? p.ThisTalent : null;
				if (t == null || !seen.Add(t.EvolutionID)) {
					continue;
				}
				if (!t.TalentHasBeenPurchased) {
					unowned.Add(t);
					CollectUnownedAncestors(p, seen, unowned);
				}
			}
		}


		private static GrantResult GrantDataOnly(EvolutionTalent talent) {
			string saveFile = SaveSwapper.CurrentSaveFileName();
			if (string.IsNullOrEmpty(saveFile)) {
				return GrantResult.Retry;
			}
			
			talent.TalentHasBeenPurchased = true;
			try {
				SavingAndLoading.SaveEvolutionTalents(saveFile);
				
			} catch (Exception) {
				talent.TalentHasBeenPurchased = false;
				throw;                                  
			}
			return GrantResult.Granted;
		}
	}
}
