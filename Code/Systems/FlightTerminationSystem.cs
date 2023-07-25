// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;

using Entitas;

using PhantomBrigade;

using UnityEngine;

namespace EchKode.PBMods.MissilePrediction
{
	internal class FlightTerminationSystem : ReactiveSystem<ECS.EkPredictionEntity>
	{
		private readonly float triggerDistance;

		internal FlightTerminationSystem(ECS.Contexts ekContexts, float triggerDistance)
			: base(ekContexts.ekPrediction)
		{
			this.triggerDistance = triggerDistance;
		}

		protected override bool Filter(ECS.EkPredictionEntity entity) => entity.hasSliceIndex;

		protected override ICollector<ECS.EkPredictionEntity> GetTrigger(IContext<ECS.EkPredictionEntity> context) =>
			context.CreateCollector(ECS.EkPredictionMatcher.SliceIndex);

		protected override void Execute(List<ECS.EkPredictionEntity> entities)
		{
			foreach (var predicted in entities)
			{
				if (!predicted.hasMotionTimeSlices)
				{
					continue;
				}
				if (!predicted.hasSliceIndex)
				{
					continue;
				}

				var sliceIndex = predicted.sliceIndex.i - 1;
				if (sliceIndex < 0)
				{
					continue;
				}
				if (sliceIndex >= predicted.motionTimeSlices.a.Length)
				{
					continue;
				}
				if (predicted.motionTimeSlices.a[sliceIndex].Status == TimeSliceStatus.Destroyed)
				{
					continue;
				}

				var expired = CheckExpiration(predicted, sliceIndex);
				if (expired)
				{
					TerminatePrediction(predicted, DestructionReason.Expired);
					continue;
				}

				var proximity = CheckProximity(predicted, sliceIndex);
				if (proximity)
				{
					TerminatePrediction(predicted, DestructionReason.Proximity);
					continue;
				}

				if (sliceIndex == predicted.motionTimeSlices.a.Length - 1)
				{
					continue;
				}

				var previousSlice = predicted.motionTimeSlices.a[sliceIndex];
				var currentSlice = predicted.motionTimeSlices.a[sliceIndex + 1];
				currentSlice.TimeToLive = previousSlice.TimeToLive - ECS.Contexts.sharedInstance.ekTime.timeStep.f;
				predicted.motionTimeSlices.a[sliceIndex + 1] = currentSlice;
			}
		}

		private bool CheckExpiration(ECS.EkPredictionEntity predicted, int sliceIndex)
		{
			var timeSlice = predicted.motionTimeSlices.a[sliceIndex];
			return timeSlice.TimeToLive <= 0f;
		}

		private bool CheckProximity(ECS.EkPredictionEntity predicted, int sliceIndex)
		{
			var timeSlice = predicted.motionTimeSlices.a[sliceIndex];
			var targetPosition = ResolveTargetPosition(predicted, sliceIndex);
			var distance = Vector3.Distance(timeSlice.Position, targetPosition);
			var triggerDistance = predicted.hasFuseProximityDistance
				? predicted.fuseProximityDistance.f
				: this.triggerDistance;
			return distance < triggerDistance;
		}

		private Vector3 ResolveTargetPosition(ECS.EkPredictionEntity predicted, int sliceIndex)
		{
			var timeSlice = predicted.motionTimeSlices.a[sliceIndex];
			var targetPosition = timeSlice.TargetPosition;
			if (!predicted.hasTargetEntityLink)
			{
				return targetPosition;
			}

			var targetEntity = IDUtility.GetCombatEntity(predicted.targetEntityLink.combatID);
			if (targetEntity == null)
			{
				return targetPosition;
			}
			var time = Contexts.sharedInstance.combat.simulationTime.f + ECS.Contexts.sharedInstance.ekTime.timeStep.f * sliceIndex;
			PathUtility.GetProjectedTransformAtTime(targetEntity, time, out targetPosition, out var _);
			if (targetEntity.hasLocalCenterPoint)
			{
				targetPosition += targetEntity.localCenterPoint.v;
			}
			return targetPosition;
		}

		private static void TerminatePrediction(ECS.EkPredictionEntity predicted, DestructionReason reason)
		{
			var timeSlices = predicted.motionTimeSlices.a;
			var sliceIndex = predicted.sliceIndex.i;
			for (var i = sliceIndex; i < timeSlices.Length; i += 1)
			{
				var ts = timeSlices[i];
				ts.Status = TimeSliceStatus.Destroyed;
				ts.DestroyedBy = reason;
				timeSlices[i] = ts;
			}

			predicted.RemoveSliceIndex();
			predicted.isPredictionMotionReady = true;

			if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Guidance))
			{
				Debug.LogFormat(
					"Mod {0} ({1}) flight termination triggered | ID: {2}{3} | time slice: {4} | reason: {5}",
					ModLink.modIndex,
					ModLink.modID,
					predicted.projectileLink.combatID < 0 ? "LIT" : "C-",
					predicted.projectileLink.combatID,
					sliceIndex,
					reason);
			}
		}
	}
}
