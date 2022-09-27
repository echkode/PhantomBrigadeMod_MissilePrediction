using System.Collections.Generic;

using Entitas;

using PhantomBrigade.Data;

using UnityEngine;

namespace EchKode.PBMods.MissilePrediction
{
	sealed class ActionDragSystem : ReactiveSystem<ActionEntity>
	{
		private readonly ECS.EkPredictionContext ekPrediction;
		private readonly Dictionary<int, List<ECS.EkPredictionEntity>> linkedActions;

		internal ActionDragSystem(Contexts contexts, ECS.Contexts ekContexts)
			: base(contexts.action)
		{
			ekPrediction = ekContexts.ekPrediction;
			linkedActions = new Dictionary<int, List<ECS.EkPredictionEntity>>();
		}

		protected override bool Filter(ActionEntity entity) => !entity.isDisposed && !entity.isDestroyed;

		protected override ICollector<ActionEntity> GetTrigger(IContext<ActionEntity> context) =>
			context.CreateCollector(ActionMatcher.StartTime);

		protected override void Execute(List<ActionEntity> entities)
		{
			if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Link)
				&& ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Trace))
			{
				Debug.LogFormat(
					"Mod {0} ({1}) ActionDragSystem triggered | turn: {2} | action dragging: {3}",
					ModLink.modIndex,
					ModLink.modID,
					Contexts.sharedInstance.combat.currentTurn.i,
					CIViewCombatTimeline.IsDraggingAction);
			}

			if (CIViewCombatTimeline.IsDraggingAction)
			{
				return;
			}

			linkedActions.Clear();
			foreach (var predicted in ekPrediction.GetEntities())
			{
				if (!predicted.hasActionLink)
				{
					continue;
				}
				if (!linkedActions.TryGetValue(predicted.actionLink.actionID, out var linkedPredictions))
				{
					linkedPredictions = new List<ECS.EkPredictionEntity>();
					linkedActions.Add(predicted.actionLink.actionID, linkedPredictions);
				}
				linkedPredictions.Add(predicted);
			}

			var targetedActionBuffer = DataShortcuts.anim.targetedActionBuffer;
			foreach (var action in entities)
			{
				if (!DataHelperAction.IsValid(action))
				{
					continue;
				}

				if (!linkedActions.ContainsKey(action.id.id))
				{
					continue;
				}

				if (!linkedActions.TryGetValue(action.id.id, out var linkedPredictions))
				{
					continue;
				}

				var duration = action.duration.f - targetedActionBuffer * 2f;
				foreach (var predicted in linkedPredictions)
				{
					var rounds = predicted.activationCount.i;
					var roundSpacing = rounds > 1 ? (float)predicted.roundSequenceNumber.i / (rounds - 1) : 1f;
					var startTime = action.startTime.f + targetedActionBuffer;
					var endTime = startTime + duration;
					if (predicted.hasActivationTiming)
					{
						roundSpacing = Mathf.Pow(roundSpacing, predicted.activationTiming.instance.exponent);
						var adjustedStartTime = Mathf.Lerp(startTime, endTime, predicted.activationTiming.instance.timeFrom);
						endTime = Mathf.Lerp(startTime, endTime, predicted.activationTiming.instance.timeTo);
						startTime = adjustedStartTime;
					}
					var roundStartTime = Mathf.Lerp(startTime, endTime, roundSpacing);
					predicted.ReplaceRoundStartTime(roundStartTime);
					PredictionDoubleFunctions.ResetRoundPlacement(predicted);
				}
			}
		}
	}
}
