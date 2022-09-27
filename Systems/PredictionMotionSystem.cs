using System.Collections.Generic;

using Entitas;

using UnityEngine;

namespace EchKode.PBMods.MissilePrediction
{
	sealed class PredictionMotionSystem : ReactiveSystem<ECS.EkTimeEntity>
	{
		private readonly ECS.EkTimeContext ekTime;
		private readonly ECS.EkPredictionContext prediction;

		internal PredictionMotionSystem(ECS.Contexts ekContexts)
			: base(ekContexts.ekTime)
		{
			ekTime = ekContexts.ekTime;
			prediction = ekContexts.ekPrediction;
		}

		protected override bool Filter(ECS.EkTimeEntity entity) => entity.currentTimeSlice.i != -1;

		protected override ICollector<ECS.EkTimeEntity> GetTrigger(IContext<ECS.EkTimeEntity> context) =>
			context.CreateCollector(ECS.EkTimeMatcher.CurrentTimeSlice);

		protected override void Execute(List<ECS.EkTimeEntity> entities)
		{
			var sliceIndex = ekTime.currentTimeSlice.i;

			foreach (var predicted in prediction.GetEntities())
			{
				if (!predicted.isPredictionMotionReady)
				{
					continue;
				}
				if (sliceIndex >= predicted.motionTimeSlices.a.Length)
				{
					continue;
				}

				var timeSlice = predicted.motionTimeSlices.a[sliceIndex];
				if (timeSlice.Status != TimeSliceStatus.Active)
				{
					if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Asset)
						&& ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Trace)
						&& predicted.hasAsset)
					{
						Debug.LogFormat(
							"Mod {0} ({1}) hiding prediction double | ID: {2}{3} | time slice: {4}/{5:F3}s",
							ModLink.modIndex,
							ModLink.modID,
							predicted.projectileLink.combatID < 0 ? "LIT" : "C-",
							predicted.projectileLink.combatID,
							sliceIndex,
							sliceIndex * ekTime.timeStep.f);
					}

					PredictionDoubleFunctions.ReleaseAsset(predicted);
					continue;
				}

				if (predicted.hasAsset)
				{
					predicted.asset.instance.OnPosition(timeSlice.Position);
					predicted.asset.instance.OnRotation(timeSlice.Rotation);
				}
				else
				{
					PredictionDoubleFunctions.AddAsset(predicted);
					SyncAssetToPrediction(predicted, timeSlice);
					if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Asset)
						&& ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Trace))
					{
						Debug.LogFormat(
							"Mod {0} ({1}) showing prediction double | ID: {2}{3} | time slice: {4}/{5:F3}s | position: {6:F1} | facing: {7:F1}",
							ModLink.modIndex,
							ModLink.modID,
							predicted.projectileLink.combatID < 0 ? "LIT" : "C-",
							predicted.projectileLink.combatID,
							sliceIndex,
							sliceIndex * ekTime.timeStep.f,
							timeSlice.Position,
							timeSlice.Facing);
					}
				}
			}
		}

		private static void SyncAssetToPrediction(ECS.EkPredictionEntity predicted, MotionTimeSlice timeSlice)
		{
			var transform = predicted.asset.instance.transform;
			transform.position = timeSlice.Position;
			transform.forward = timeSlice.Facing;
			if (predicted.hasScale)
			{
				transform.localScale = predicted.scale.v;
			}
		}
	}
}
