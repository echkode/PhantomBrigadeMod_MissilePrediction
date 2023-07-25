// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;

using Entitas;

using PhantomBrigade;

using UnityEngine;

namespace EchKode.PBMods.MissilePrediction
{
	sealed class PredictionGuidedControlSystem : ReactiveSystem<ECS.EkPredictionEntity>
	{
		private readonly float chaseDistance;
		private readonly ECS.EkTimeContext ekTime;

		internal PredictionGuidedControlSystem(ECS.Contexts ekContexts, float chaseDistance)
			: base(ekContexts.ekPrediction)
		{
			this.chaseDistance = chaseDistance;
			ekTime = ekContexts.ekTime;
		}

		protected override bool Filter(ECS.EkPredictionEntity entity) => entity.hasSliceIndex;

		protected override ICollector<ECS.EkPredictionEntity> GetTrigger(IContext<ECS.EkPredictionEntity> context) =>
			context.CreateCollector(ECS.EkPredictionMatcher.SliceIndex);

		protected override void Execute(List<ECS.EkPredictionEntity> entities)
		{
			var dt = ekTime.timeStep.f;
			foreach (var predicted in entities)
			{
				if (predicted.isPredictionMotionReady)
				{
					continue;
				}
				if (!predicted.hasMotionTimeSlices)
				{
					continue;
				}
				if (!predicted.hasSliceIndex)
				{
					continue;
				}

				var timeSlices = predicted.motionTimeSlices.a;
				var sliceIndex = predicted.sliceIndex.i;
				if (0 == sliceIndex || sliceIndex > timeSlices.Length - 1)
				{
					continue;
				}

				var timeSlice = timeSlices[sliceIndex];
				if (timeSlice.Status != TimeSliceStatus.Active)
				{
					continue;
				}

				var currentPosition = timeSlice.Position;
				var chasePosition = timeSlice.TargetPosition;
				var chaseDirection = Utilities.GetDirection(currentPosition, chasePosition);
				var remainingDistance = Vector3.Distance(currentPosition, chasePosition);

				var guidanceDisabled = IsUnguided(
					predicted,
					chaseDirection,
					remainingDistance,
					sliceIndex,
					timeSlices[sliceIndex - 1].IsGuided);
				if (guidanceDisabled)
				{
					timeSlice.DriverInputPitch = 0f;
					timeSlice.DriverInputYaw = 0f;
					timeSlice.DriverInputThrottle = timeSlices[sliceIndex - 1].DriverInputThrottle;
					timeSlice.IsGuided = false;
					timeSlices[sliceIndex] = timeSlice;
					continue;
				}

				timeSlice.IsGuided = true;

				if (predicted.hasMotionExtraData)
				{
					var ed = predicted.motionExtraData.a[sliceIndex];
					ed.ChaseDirection = chaseDirection;
					predicted.motionExtraData.a[sliceIndex] = ed;
				}

				chasePosition = CompensateForVelocity(
					predicted,
					timeSlice.Velocity,
					currentPosition,
					chasePosition,
					chaseDirection,
					remainingDistance,
					sliceIndex);

				(timeSlice.DriverInputPitch, timeSlice.DriverInputYaw, timeSlice.DriverInputThrottle) =
					UpdateDriverInput(
						dt,
						predicted,
						currentPosition,
						chasePosition,
						chaseDistance,
						timeSlice);

				timeSlices[sliceIndex] = timeSlice;
			}
		}

		private static bool IsUnguided(
			ECS.EkPredictionEntity predicted,
			Vector3 chaseDirection,
			float remainingDistance,
			int sliceIndex,
			bool previousGuided)
		{
			var guidanceData = predicted.guidanceData.data;
			if (guidanceData.directionCheck == null)
			{
				return false;
			}
			if (remainingDistance > guidanceData.directionCheck.distance)
			{
				if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Guidance)
					&& !previousGuided)
				{
					Debug.LogFormat(
						"Mod {0} ({1}) guidance is re-engaged -- distance check | projectile: {2}{3} | index: {4} | distance: {5:F1} | distance threshold: {6:F1}",
						ModLink.modIndex,
						ModLink.modID,
						predicted.projectileLink.combatID < 0 ? "LIT" : "C-",
						predicted.projectileLink.combatID,
						sliceIndex,
						remainingDistance,
						guidanceData.directionCheck.distance);
				}
				return false;
			}

			var forward = predicted.authoritativeRigidbody.rb.transform.forward;
			var dot = Vector3.Dot(forward, chaseDirection);
			var unguided = dot < guidanceData.directionCheck.dotThreshold;

			if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Guidance)
				&& unguided == previousGuided)
			{
				Debug.LogFormat(
					"Mod {0} ({1}) guidance is {2} -- direction check | projectile: {3}{4} | index: {5} | distance: {6:F1} | distance threshold: {7:F1} | dot: {8:F2} | dot threshold: {9:F2}",
					ModLink.modIndex,
					ModLink.modID,
					unguided ? "off" : "re-engaged",
					predicted.projectileLink.combatID < 0 ? "LIT" : "C-",
					predicted.projectileLink.combatID,
					sliceIndex,
					remainingDistance,
					guidanceData.directionCheck.distance,
					dot,
					guidanceData.directionCheck.dotThreshold);
			}

			return unguided;
		}

		private Vector3 CompensateForVelocity(
			ECS.EkPredictionEntity predicted,
			Vector3 selfVelocity,
			Vector3 currentPosition,
			Vector3 chasePosition,
			Vector3 chaseDirection,
			float remainingDistance,
			int sliceIndex)
		{
			var guidanceData = predicted.guidanceData.data;
			if (guidanceData.velocityCompensation == null)
			{
				return chasePosition;
			}
			if (guidanceData.velocityCompensation.rangeLimit.x <= 0f)
			{
				return chasePosition;
			}
			if (guidanceData.velocityCompensation.rangeLimit.y < guidanceData.velocityCompensation.rangeLimit.x
				|| guidanceData.velocityCompensation.rangeLimit.y.RoughlyEqual(guidanceData.velocityCompensation.rangeLimit.x))
			{
				return chasePosition;
			}
			
			var iterations = Mathf.Clamp(guidanceData.velocityCompensation.iterations, 0, 3);
			if (iterations == 0)
			{
				return chasePosition;
			}

			var rangeLimit = guidanceData.velocityCompensation.rangeLimit;
			var compensationFactor = remainingDistance >= rangeLimit.x
				? remainingDistance >= rangeLimit.y
					? 0f
					: 1f - Mathf.Clamp01((remainingDistance - rangeLimit.x) / (rangeLimit.y - rangeLimit.x))
				: 1f;
			if (compensationFactor < 0f || compensationFactor.RoughlyEqual(0f))
			{
				return chasePosition;
			}

			var targetUnit = predicted.hasTargetEntityLink
				? IDUtility.GetCombatEntity(predicted.targetEntityLink.combatID)
				: null;
			var targetVelocity = targetUnit != null
				? VelocityAtTime(targetUnit, sliceIndex)
				: Vector3.zero;
			var speedAlongTargetLine = Vector3.Project(selfVelocity, chaseDirection).magnitude;
			var targetVelocityTowardSelf = Vector3.ProjectOnPlane(targetVelocity, -chaseDirection);
			var relativeVelocity = Vector3.Lerp(selfVelocity, targetVelocityTowardSelf, guidanceData.velocityCompensation.selfVelocityProjection);
			var compensatedSelfVelocity = relativeVelocity * guidanceData.velocityCompensation.selfVelocityCompensation;
			var compensatedTargetVelocity = targetVelocity * guidanceData.velocityCompensation.targetVelocityCompensation;
			var adjustedTargetVelocity = (compensatedTargetVelocity - compensatedSelfVelocity) * compensationFactor;
			if (predicted.hasMotionExtraData)
			{
				var ed = predicted.motionExtraData.a[sliceIndex];
				ed.TargetVelocity = targetVelocity;
				ed.AdjustedTargetVelocity = adjustedTargetVelocity;
				predicted.motionExtraData.a[sliceIndex] = ed;
			}
			if (adjustedTargetVelocity.sqrMagnitude.RoughlyEqual(0f))
			{
				return chasePosition;
			}

			var targetPosFuture = chasePosition;
			for (var i = 0; i < iterations; i += 1)
			{
				targetPosFuture = GetFutureTargetPosition(
					currentPosition,
					chasePosition,
					targetPosFuture,
					adjustedTargetVelocity,
					speedAlongTargetLine);
			}

			return targetPosFuture;
		}

		private Vector3 VelocityAtTime(CombatEntity targetUnit, int sliceIndex)
		{
			var startTime = sliceIndex * ekTime.timeStep.f + Contexts.sharedInstance.combat.simulationTime.f;
			var endTime = startTime + ekTime.timeStep.f;
			if (sliceIndex == ekTime.sampleCount.i - 1)
			{
				endTime = startTime;
				startTime -= ekTime.timeStep.f;
			}
			PathUtility.GetProjectedTransformAtTime(targetUnit, startTime, out var startPosition, out var _);
			PathUtility.GetProjectedTransformAtTime(targetUnit, endTime, out var endPosition, out var _);
			var direction = Utilities.GetDirection(startPosition, endPosition);
			var distance = Vector3.Distance(startPosition, endPosition);
			return direction * (distance / ekTime.timeStep.f);
		}

		private static Vector3 GetFutureTargetPosition(
		  Vector3 startPos,
		  Vector3 targetPosOriginal,
		  Vector3 targetPosFuture,
		  Vector3 targetVelocity,
		  float projectileSpeed)
		{
			var range = Vector3.Distance(startPos, targetPosFuture);
			var t = projectileSpeed > 0f ? range / projectileSpeed : 0f;
			t = Mathf.Min(t, 1f);
			return targetPosOriginal + targetVelocity * t;
		}

		private static (float, float, float) UpdateDriverInput(
			float dt,
			ECS.EkPredictionEntity predicted,
			Vector3 currentPosition,
			Vector3 chasePosition,
			float chaseDistance,
			MotionTimeSlice timeSlice)
		{
			var directionXZ = (chasePosition - currentPosition).Flatten().normalized;
			var chaseAltitude = currentPosition + directionXZ * chaseDistance;
			chaseAltitude = new Vector3(chaseAltitude.x, timeSlice.TargetHeight, chaseAltitude.z);
			var adjustedChasePosition = Vector3.Lerp(chaseAltitude, chasePosition, timeSlice.TargetBlend);
			var localChase = predicted.authoritativeRigidbody.rb.transform.InverseTransformPoint(adjustedChasePosition);
			var pitchError = Mathf.Atan2(localChase.y, localChase.z);
			var yawError = Mathf.Atan2(localChase.x, localChase.z);
			var minAccel = Mathf.Max(0.1f, predicted.guidanceData.data.driverAccelerationMin);
			var t = Mathf.Max(0.0f, Vector3.Dot(localChase.normalized, Vector3.forward));
			var driverInputPitch = Mathf.Clamp(predicted.guidancePID.pitchPID.GetCorrection(pitchError, dt), -1f, 1f);
			var driverInputYaw = Mathf.Clamp(predicted.guidancePID.steeringPID.GetCorrection(yawError, dt), -1f, 1f);
			var driverInputThrottle = Mathf.Clamp01(Mathf.Lerp(minAccel, 1f, t));

			if (predicted.hasMotionExtraData)
			{
				var ed = predicted.motionExtraData.a[predicted.sliceIndex.i];
				ed.ChasePosition1 = chasePosition;
				ed.ChaseAltitude = chaseAltitude;
				ed.ChasePosition2 = adjustedChasePosition;
				ed.LocalChase = localChase;
				ed.PitchError = pitchError;
				ed.YawError = yawError;
				ed.Realtime = Time.realtimeSinceStartup;
				ed.IsValid = true;
				predicted.motionExtraData.a[predicted.sliceIndex.i] = ed;
			}
			return (driverInputPitch, driverInputYaw, driverInputThrottle);
		}
	}
}
