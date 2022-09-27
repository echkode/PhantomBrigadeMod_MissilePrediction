using System.Collections.Generic;

using Entitas;

using PhantomBrigade.Input.Components;

using UnityEngine;

namespace EchKode.PBMods.MissilePrediction
{
	sealed class ModeTrackingSystem : ReactiveSystem<InputEntity>
	{
		private static readonly List<ECS.EkPredictionEntity> predictions = new List<ECS.EkPredictionEntity>();

		private readonly CombatContext combat;
		private readonly ECS.EkPredictionContext prediction;
		private readonly ECS.EkTimeContext ekTime;

		internal ModeTrackingSystem(Contexts contexts, ECS.Contexts ekContexts)
			: base(contexts.input)
		{
			combat = contexts.combat;
			prediction = ekContexts.ekPrediction;
			ekTime = ekContexts.ekTime;
		}

		protected override bool Filter(InputEntity entity) => entity.combatUIMode.e == CombatUIModes.Path_Drawing;

		protected override ICollector<InputEntity> GetTrigger(IContext<InputEntity> context) =>
			context.CreateCollector(InputMatcher.CombatUIMode);

		protected override void Execute(List<InputEntity> entities)
		{
			if (!combat.hasUnitSelected)
			{
				return;
			}

			var targetedUnitID = combat.unitSelected.id;

			predictions.Clear();
			foreach (var prediction in prediction.GetEntities())
			{
				if (!prediction.hasTargetEntityLink)
				{
					continue;
				}
				var combatID = prediction.targetEntityLink.combatID;
				if (combatID != targetedUnitID)
				{
					continue;
				}
				predictions.Add(prediction);
			}

			foreach (var prediction in predictions)
			{
				MarkForRecalculation(prediction, targetedUnitID);
			}
		}

		private void MarkForRecalculation(ECS.EkPredictionEntity prediction, int targetedUnitID)
		{
			var sliceIndex = 0;
			for (; sliceIndex < prediction.motionTimeSlices.a.Length; sliceIndex += 1)
			{
				if (prediction.motionTimeSlices.a[sliceIndex].Status != TimeSliceStatus.Uninitialized)
				{
					break;
				}
			}

			if (sliceIndex == prediction.motionTimeSlices.a.Length)
			{
				return;
			}

			ResetRigidbody(prediction, sliceIndex);
			for (var i = sliceIndex + 1; i < prediction.motionTimeSlices.a.Length; i += 1)
			{
				var ts = prediction.motionTimeSlices.a[i];
				ts.Status = TimeSliceStatus.Recalculate;
				prediction.motionTimeSlices.a[i] = ts;
			}
			prediction.ReplaceSliceIndex(sliceIndex);
			prediction.isPredictionMotionReady = false;

			if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Recalc))
			{
				Debug.LogFormat(
					"Mod {0} ({1}) ModeTrackingSystem marked for recalc -- path drawing | projectile: {2}-{3} | start: {4} | target: C-{5}",
					ModLink.modIndex,
					ModLink.modID,
					prediction.projectileLink.combatID < 0 ? "LIT" : "C",
					Mathf.Abs(prediction.projectileLink.combatID),
					sliceIndex,
					targetedUnitID);
			}
		}

		private void ResetRigidbody(ECS.EkPredictionEntity prediction, int sliceIndex)
		{
			var timeSlice = prediction.motionTimeSlices.a[sliceIndex];
			var rb = prediction.authoritativeRigidbody.rb;
			rb.transform.position = timeSlice.Position;
			rb.transform.rotation = timeSlice.Rotation;
			rb.velocity = timeSlice.Velocity;
		}
	}
}
