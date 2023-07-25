// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;

using Entitas;

using PhantomBrigade;

using UnityEngine;

namespace EchKode.PBMods.MissilePrediction
{
	sealed class PredictionGuidanceProgramSystem : ReactiveSystem<ECS.EkPredictionEntity>
	{
		private readonly ActionContext actionContext;
		private readonly CombatContext combat;
		private readonly ECS.EkTimeContext ekTime;
		private readonly float chaseDistance;

		internal PredictionGuidanceProgramSystem(
			Contexts contexts,
			ECS.Contexts ekContexts,
			float chaseDistance)
			: base(ekContexts.ekPrediction)
		{
			actionContext = contexts.action;
			combat = contexts.combat;
			ekTime = ekContexts.ekTime;
			this.chaseDistance = chaseDistance;
		}

		protected override bool Filter(ECS.EkPredictionEntity entity) => entity.hasSliceIndex;

		protected override ICollector<ECS.EkPredictionEntity> GetTrigger(IContext<ECS.EkPredictionEntity> context) =>
			context.CreateCollector(ECS.EkPredictionMatcher.SliceIndex);

		protected override void Execute(List<ECS.EkPredictionEntity> entities)
		{
			var startTime = combat.simulationTime.f;
			var dt = ekTime.timeStep.f;
			foreach (var entity in entities)
			{
				if (entity.isPredictionMotionReady)
				{
					continue;
				}
				if (!entity.hasMotionTimeSlices)
				{
					continue;
				}
				if (!entity.hasSliceIndex)
				{
					continue;
				}

				var timeSlices = entity.motionTimeSlices.a;
				var sliceIndex = entity.sliceIndex.i;
				if (0 == sliceIndex || sliceIndex > timeSlices.Length - 1)
				{
					continue;
				}

				if (timeSlices[sliceIndex - 1].Status != TimeSliceStatus.Active)
				{
					continue;
				}
				if (timeSlices[sliceIndex].Status != TimeSliceStatus.Active)
				{
					continue;
				}

				var time = startTime + sliceIndex * ekTime.timeStep.f;
				GenerateNextTimeSlice(
					actionContext,
					time,
					dt,
					timeSlices,
					sliceIndex,
					entity,
					chaseDistance);
			}
		}

		private static void GenerateNextTimeSlice(
			ActionContext actionContext,
			float time,
			float dt,
			MotionTimeSlice[] timeSlices,
			int sliceIndex,
			ECS.EkPredictionEntity entity,
			float chaseDistance)
		{
			var previousSlice = timeSlices[sliceIndex - 1];
			var currentSlice = timeSlices[sliceIndex];
			var guidanceSuspended = false;
			if (previousSlice.GuidanceSuspensionTime > 0f)
			{
				currentSlice.GuidanceSuspensionTime = previousSlice.GuidanceSuspensionTime - dt;
				guidanceSuspended = currentSlice.GuidanceSuspensionTime > 0f;
			}

			var position = currentSlice.Position;
			var (isTargetDashing, chasePosition) = GetChasePosition(
				actionContext,
				time,
				entity,
				position,
				chaseDistance);
			var evaluatedProgress = EvaluateProgress(
				entity,
				position,
				chasePosition);
			evaluatedProgress = Mathf.Max(previousSlice.Progress, evaluatedProgress);
			currentSlice.Progress = evaluatedProgress;

			(currentSlice.TargetPosition, currentSlice.IsTrackingActive) = UpdateTargetPosition(
				entity,
				chasePosition,
				guidanceSuspended,
				isTargetDashing,
				evaluatedProgress,
				previousSlice.TargetPosition,
				previousSlice.IsTrackingActive);

			currentSlice.TargetHeight = UpdateTargetHeight(
				entity,
				evaluatedProgress);

			currentSlice.TargetBlend = entity.guidanceData.data.inputTargetBlend != null
				? Mathf.Clamp01(entity.guidanceData.data.inputTargetBlend.Evaluate(evaluatedProgress))
				: 1f;

			timeSlices[sliceIndex] = currentSlice;
		}

		private static (bool, Vector3) GetChasePosition(
			ActionContext actionContext,
			float time,
			ECS.EkPredictionEntity entity,
			Vector3 position,
			float chaseDistance)
		{
			var forward = entity.authoritativeRigidbody.rb.transform.forward;
			var chasePosition = position + forward * chaseDistance;
			var isTargetDashing = false;
			if (entity.hasTargetEntityLink)
			{
				var targetEntity = IDUtility.GetCombatEntity(entity.targetEntityLink.combatID);
				if (targetEntity != null)
				{
					PathUtility.GetProjectedTransformAtTime(targetEntity, time, out var targetPosition, out var _);
					chasePosition = targetPosition;
					if (targetEntity.hasLocalCenterPoint)
					{
						chasePosition += targetEntity.localCenterPoint.v;
					}
					isTargetDashing = IsDashingAtTime(actionContext, targetEntity, time);
				}
			}
			else if (entity.hasTargetPosition)
			{
				chasePosition = entity.targetPosition.v;
			}

			return (isTargetDashing, chasePosition);
		}

		private static bool IsDashingAtTime(ActionContext actionContext, CombatEntity targetEntity, float time)
		{
			foreach (var action in actionContext.GetEntitiesWithActionOwner(targetEntity.id.id))
			{
				if (action.isDestroyed)
				{
					continue;
				}
				if (action.isDisposed)
				{
					continue;
				}
				if (!action.hasStartTime)
				{
					continue;
				}
				if (!action.hasDuration)
				{
					continue;
				}
				if (!action.hasMovementDash && !action.hasMovementMeleeAttacker)
				{
					continue;
				}
				if (!TimeUtility.ContainsTime(time, action.startTime.f, action.duration.f))
				{
					continue;
				}

				return true;
			}

			return false;
		}

		private static float EvaluateProgress(
			ECS.EkPredictionEntity entity,
			Vector3 currentPosition,
			Vector3 targetPosition)
		{
			if (entity.guidanceData.data.inputProgressFromTarget)
			{
				var startPosition = entity.startPosition.v;
				return EvaluateProgressFromDistance(startPosition, currentPosition, targetPosition);
			}
			return EvaluateProgressFromLifetime(entity);
		}

		private static float EvaluateProgressFromDistance(
			Vector3 startPosition,
			Vector3 currentPosition,
			Vector3 targetPosition)
		{
			// Uses linear distances on the XZ plane.

			targetPosition = targetPosition.Flatten();
			var remainingDistance = Vector3.Distance(targetPosition, currentPosition.Flatten());
			var totalDistance = Vector3.Distance(targetPosition, startPosition.Flatten());
			if (totalDistance > 0f)
			{
				return 1f - Mathf.Clamp01(remainingDistance / totalDistance);
			}
			return 1f;
		}

		private static float EvaluateProgressFromLifetime(ECS.EkPredictionEntity entity)
		{
			var ttl = entity.hasTimeToLive ? entity.timeToLive.f : 0f;
			var projLifetime = entity.projectileLifetime.f;
			projLifetime = Mathf.Max(0.1f, projLifetime);
			return 1f - Mathf.Clamp01(ttl / projLifetime);
		}

		private static (Vector3, bool) UpdateTargetPosition(
			ECS.EkPredictionEntity entity,
			Vector3 targetPosition,
			bool isGuidanceSuspended,
			bool isTargetDashing,
			float evaluatedProgress,
			Vector3 previousTrackedPosition,
			bool isTracking)
		{
			if (isGuidanceSuspended)
			{
				return (previousTrackedPosition, false);
			}
			if (isTargetDashing)
			{
				return (previousTrackedPosition, false);
			}

			var inputTargetUpdate = entity.guidanceData.data.inputTargetUpdate;
			var evaluatedUpdate = inputTargetUpdate != null
				? Mathf.Clamp01(inputTargetUpdate.Evaluate(evaluatedProgress))
				: 1f;
			if (evaluatedUpdate < 0.5f)
			{
				if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Guidance) && isTracking)
				{
					Debug.LogFormat(
						"Mod {0} ({1}) tracking is off -- evaluated target update | projectile: {2}-{3} | progress: {4:F2} | target update: {5:F2}",
						ModLink.modIndex,
						ModLink.modID,
						entity.projectileLink.combatID < 0 ? "LIT" : "C",
						Mathf.Abs(entity.projectileLink.combatID),
						evaluatedProgress,
						evaluatedUpdate);
				}
				return (previousTrackedPosition, false);
			}

			var offset = GetTargetOffset(entity, evaluatedProgress);
			return (targetPosition + new Vector3(offset.x, 0f, offset.y), true);
		}

		private static Vector2 GetTargetOffset(
			ECS.EkPredictionEntity entity,
			float evaluatedProgress)
		{
			var inputTargetOffset = entity.guidanceData.data.inputTargetOffset;
			var offsetFactor = inputTargetOffset != null
				? Mathf.Clamp01(inputTargetOffset.Evaluate(evaluatedProgress))
				: 0f;
			return entity.targetOffset.v * offsetFactor;
		}

		private static float UpdateTargetHeight(
			ECS.EkPredictionEntity entity,
			float evaluatedProgress)
		{
			var y = entity.startPosition.v.y;
			var guidanceData = entity.guidanceData.data;
			if (guidanceData.inputTargetHeight == null)
			{
				return y;
			}

			var altitudeCeiling = y + guidanceData.inputTargetHeightScale;
			var t = Mathf.Clamp01(guidanceData.inputTargetHeight.Evaluate(evaluatedProgress));
			return Mathf.Lerp(y, altitudeCeiling, t);
		}
	}
}
