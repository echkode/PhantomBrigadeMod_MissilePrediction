// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;

using Entitas;

using PhantomBrigade.AI.Components;

namespace EchKode.PBMods.MissilePrediction
{
	sealed class AIPhaseTrackingSystem : ReactiveSystem<AIEntity>
	{
		private readonly ECS.EkPredictionContext ekPrediction;

		internal AIPhaseTrackingSystem(Contexts contexts, ECS.Contexts ekContexts)
			: base(contexts.aI)
		{
			ekPrediction = ekContexts.ekPrediction;
		}

		protected override bool Filter(AIEntity entity) => entity.aIPlanningRequest.phase == AIPhase.Finished;

		protected override ICollector<AIEntity> GetTrigger(IContext<AIEntity> context) =>
			context.CreateCollector(AIMatcher.AIPlanningRequest);

		protected override void Execute(List<AIEntity> entities)
		{
			ekPrediction.isPendingPlaceRound = ekPrediction.hasRoundQueue
				&& ekPrediction.roundQueue.q.Count != 0;
		}
	}
}
