using System.Collections.Generic;

using Entitas;

using PhantomBrigade;

namespace EchKode.PBMods.MissilePrediction
{
	sealed class UnitTrackingSystem : ReactiveSystem<ActionEntity>
	{
		private readonly ActionContext actionContext;
		private readonly ECS.EkPredictionContext ekPrediction;
		private readonly Dictionary<int, List<ECS.EkPredictionEntity>> targetedUnitMap;
		private readonly Dictionary<int, List<ECS.EkPredictionEntity>> linkedActionMap;

		internal UnitTrackingSystem(Contexts contexts, ECS.Contexts ekContexts)
			: base(contexts.action)
		{
			actionContext = contexts.action;
			ekPrediction = ekContexts.ekPrediction;
			targetedUnitMap = new Dictionary<int, List<ECS.EkPredictionEntity>>();
			linkedActionMap = new Dictionary<int, List<ECS.EkPredictionEntity>>();
		}

		protected override bool Filter(ActionEntity entity) => !entity.isDestroyed
			&& !entity.CompletedAction
			&& entity.hasActionOwner;

		protected override ICollector<ActionEntity> GetTrigger(IContext<ActionEntity> context) =>
			context.CreateCollector(ActionMatcher.AnyOf(ActionMatcher.Disposed, ActionMatcher.MovementPathChanged));

		protected override void Execute(List<ActionEntity> entities)
		{
			targetedUnitMap.Clear();
			linkedActionMap.Clear();
			foreach (var predicted in ekPrediction.GetEntities())
			{
				if (predicted.hasActionLink)
				{
					if (!linkedActionMap.TryGetValue(predicted.actionLink.actionID, out var linkedActions))
					{
						linkedActions = new List<ECS.EkPredictionEntity>();
						linkedActionMap.Add(predicted.actionLink.actionID, linkedActions);
					}
					linkedActions.Add(predicted);
				}

				if (!predicted.hasTargetEntityLink)
				{
					continue;
				}

				var combatID = predicted.targetEntityLink.combatID;
				var targetedUnit = IDUtility.GetCombatEntity(combatID);
				if (targetedUnit == null)
				{
					continue;
				}
				if (!targetedUnitMap.TryGetValue(combatID, out var targetingProjectiles))
				{
					targetingProjectiles = new List<ECS.EkPredictionEntity>();
					targetedUnitMap.Add(combatID, targetingProjectiles);
				}
				targetingProjectiles.Add(predicted);
			}

			foreach (var action in entities)
			{
				if (!action.hasActionOwner)
				{
					continue;
				}
				if (!action.isOnPrimaryTrack)
				{
					continue;
				}

				if (targetedUnitMap.TryGetValue(action.actionOwner.combatID, out var targetingProjectiles))
				{
					foreach (var predicted in targetingProjectiles)
					{
						PredictionDoubleFunctions.MarkForRecalculation(predicted, action);
					}
				}

				foreach (var ownedAction in actionContext.GetEntitiesWithActionOwner(action.actionOwner.combatID))
				{
					if (!linkedActionMap.TryGetValue(ownedAction.id.id, out var linkedPredictions))
					{
						continue;
					}

					linkedPredictions.Sort(PredictionDoubleFunctions.Compare);
					foreach (var predicted in linkedPredictions)
					{
						PredictionDoubleFunctions.MarkForRecalculation(predicted, action);
					}
				}
			}
		}
	}
}
