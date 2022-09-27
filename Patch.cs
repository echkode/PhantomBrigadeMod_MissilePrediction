using HarmonyLib;

using PBCIViewCombatTimeline = CIViewCombatTimeline;

namespace EchKode.PBMods.MissilePrediction
{
	[HarmonyPatch]
	static class Patch
	{
		[HarmonyPatch(typeof(PhantomBrigade.Heartbeat), "Start")]
		[HarmonyPrefix]
		static void Hb_StartPrefix()
		{
			Heartbeat.Start();
		}

		[HarmonyPatch(typeof(PBCIViewCombatTimeline), "OnActionDrag")]
		[HarmonyPrefix]
		static void Civct_OnActionDragPrefix(object callbackAsObject)
		{
			CIViewCombatTimeline.OnActionDrag();
		}

		[HarmonyPatch(typeof(PBCIViewCombatTimeline), "OnActionDragEnd")]
		[HarmonyPostfix]
		static void Civct_OnActionDragEndPostfix(object callbackAsObject)
		{
			CIViewCombatTimeline.OnActionDragEnd(callbackAsObject);
		}

		[HarmonyPatch(typeof(PBCIViewCombatTimeline), "UpdateScrubbing")]
		[HarmonyPrefix]
		static bool Civct_UpdateScrubbingPrefix()
		{
			return CIViewCombatTimeline.UpdateScrubbing();
		}
	}
}
