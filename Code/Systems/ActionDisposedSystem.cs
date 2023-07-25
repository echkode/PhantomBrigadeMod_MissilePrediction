// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;

using Entitas;

namespace EchKode.PBMods.MissilePrediction
{
	sealed class ActionDisposedSystem : ReactiveSystem<ActionEntity>
	{
		private readonly ECS.EkPredictionContext ekPrediction;
		private readonly HashSet<int> disposedActions;

		internal ActionDisposedSystem(Contexts contexts, ECS.Contexts ekContexts)
			: base(contexts.action)
		{
			ekPrediction = ekContexts.ekPrediction;
			disposedActions = new HashSet<int>();
		}

		protected override bool Filter(ActionEntity entity) => entity.isDisposed;

		protected override ICollector<ActionEntity> GetTrigger(IContext<ActionEntity> context) =>
			context.CreateCollector(ActionMatcher.Disposed);

		protected override void Execute(List<ActionEntity> entities)
		{
			disposedActions.Clear();
			foreach (var action in entities)
			{
				disposedActions.Add(action.id.id);
			}

			foreach (var predicted in ekPrediction.GetEntities())
			{
				if (!predicted.hasActionLink)
				{
					continue;
				}
				if (!disposedActions.Contains(predicted.actionLink.actionID))
				{
					continue;
				}
				PredictionDoubleFunctions.DestroyPredicted(predicted);
			}
		}
	}
}
