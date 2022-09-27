using System.Collections.Generic;

using Entitas;

using UnityEngine;

namespace EchKode.PBMods.MissilePrediction
{
	sealed class PredictionTimeSliceSystem : ReactiveSystem<CombatEntity>, IInitializeSystem
	{
		private readonly int timeSlicesPerSecond;
		private readonly CombatContext combat;
		private readonly ECS.EkTimeContext ekTime;

		internal PredictionTimeSliceSystem(Contexts contexts, ECS.Contexts ekContexts, int timeSlicesPerSecond)
			:base(contexts.combat)
		{
			this.timeSlicesPerSecond = timeSlicesPerSecond;
			combat = contexts.combat;
			ekTime = ekContexts.ekTime;
		}

		public void Initialize()
		{
			ekTime.ReplaceSlicesPerSecond(timeSlicesPerSecond);
			ekTime.ReplaceTimeStep(1f / timeSlicesPerSecond);
			ekTime.ReplaceCurrentTimeSlice(-1);

			if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Time))
			{
				Debug.LogFormat(
					"Mod {0} ({1}) initialized PredictionTimeSliceSystem",
					ModLink.modIndex,
					ModLink.modID);
			}
		}

		protected override bool Filter(CombatEntity entity) => !entity.Simulating;

		protected override ICollector<CombatEntity> GetTrigger(IContext<CombatEntity> context) =>
			context.CreateCollector(CombatMatcher.PredictionTime);

		protected override void Execute(List<CombatEntity> entities)
		{
			var turnTime = combat.predictionTime.f - combat.simulationTime.f;
			var timeSlice = Mathf.FloorToInt(turnTime * ekTime.slicesPerSecond.i);
			if (timeSlice < 0)
			{
				Debug.LogWarningFormat(
					"Mod {0} ({1}) prediction time slice -- negative, forcing to zero | turn start: {2:F3} | time: {3:F3}s | time target: {4:F3}s | slice: {5}",
					ModLink.modIndex,
					ModLink.modID,
					combat.simulationTime.f,
					combat.predictionTime.f,
					combat.predictionTimeTarget.f,
					timeSlice);
				timeSlice = 0;
			}

			if (ekTime.currentTimeSlice.i == timeSlice)
			{
				return;
			}

			ekTime.ReplaceCurrentTimeSlice(timeSlice);

			if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Time)
				&& ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Trace))
			{
				Debug.LogFormat(
					"Mod {0} ({1}) prediction time slice | turn start: {2:F3} | time: {3:F3}s | time target: {4:F3}s | slice: {5}",
					ModLink.modIndex,
					ModLink.modID,
					combat.simulationTime.f,
					combat.predictionTime.f,
					combat.predictionTimeTarget.f,
					timeSlice);
			}
		}
	}
}
