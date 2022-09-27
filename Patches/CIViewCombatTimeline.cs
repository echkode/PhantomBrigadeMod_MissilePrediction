using PhantomBrigade;

namespace EchKode.PBMods.MissilePrediction
{
	static class CIViewCombatTimeline
	{
		internal static bool IsDraggingAction;

		internal static bool UpdateScrubbing()
		{
			return !ECS.Contexts.sharedInstance.ekPrediction.isPendingPlaceRound;
		}

		internal static void OnActionDrag()
		{
			IsDraggingAction = true;
		}

		internal static void OnActionDragEnd(object callbackAsObject)
		{
			IsDraggingAction = false;
			if (!CombatUIUtility.IsCombatUISafe() || !(callbackAsObject is UICallback))
				return;
			var uiCallback = (UICallback)callbackAsObject;
			var actionID = uiCallback.argumentInt;
			var action = IDUtility.GetActionEntity(actionID);
			if (action == null)
			{
				return;
			}
			if (!action.hasStartTime)
			{
				return;
			}
			action.ReplaceStartTime(action.startTime.f);
		}
	}
}
