// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using Entitas;

namespace EchKode.PBMods.MissilePrediction
{
	sealed class PredictionTearDownSystem : ITearDownSystem
	{
		private readonly ECS.EkPredictionContext prediction;
		private readonly ECS.EkTimeContext ekTime;

		internal PredictionTearDownSystem(ECS.Contexts ekContexts)
		{
			prediction = ekContexts.ekPrediction;
			ekTime = ekContexts.ekTime;
		}

		public void TearDown()
		{
			foreach (var entity in prediction.GetEntities())
			{
				PredictionDoubleFunctions.DestroyPredicted(entity);
			}

			if (prediction.hasRoundQueue)
			{
				prediction.roundQueue.q.Clear();
				prediction.RemoveRoundQueue();
			}

			prediction.isPendingPlaceRound = false;
			if (ekTime.hasCurrentTimeTarget)
			{
				ekTime.RemoveCurrentTimeTarget();
			}
		}
	}
}
