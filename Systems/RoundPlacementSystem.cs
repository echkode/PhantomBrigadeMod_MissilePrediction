using System.Collections.Generic;

using Entitas;

using PhantomBrigade;
using PhantomBrigade.Data;

using UnityEngine;

namespace EchKode.PBMods.MissilePrediction
{
	sealed class RoundPlacementSystem : ReactiveSystem<ECS.EkPredictionEntity>
	{
		private struct TargetInfo
		{
			public int CombatTargetID;
			public Vector3 FiringPoint;
			public Vector3 FiringDirection;
			public Vector3 TargetedPosition;
			public float WeaponSpeed;
		}

		private readonly CombatContext combat;
		private readonly ECS.EkTimeContext ekTime;
		private readonly ECS.EkPredictionContext ekPrediction;
		private readonly int delay;
		private readonly float timeTargetThreshold;
		private RoundInfo round;
		private float lastDelta;

		internal RoundPlacementSystem(Contexts contexts, ECS.Contexts ekContexts, int delay, float timeTargetThreshold)
			: base(ekContexts.ekPrediction)
		{
			combat = contexts.combat;
			ekTime = ekContexts.ekTime;
			ekPrediction = ekContexts.ekPrediction;
			this.delay = delay;
			this.timeTargetThreshold = timeTargetThreshold;
		}

		protected override bool Filter(ECS.EkPredictionEntity entity) => entity.isPendingPlaceRound;

		protected override ICollector<ECS.EkPredictionEntity> GetTrigger(IContext<ECS.EkPredictionEntity> context) =>
			context.CreateCollector(ECS.EkPredictionMatcher.PendingPlaceRound);

		public new void Execute()
		{
			if (!ekPrediction.isPendingPlaceRound
				&& ekPrediction.hasRoundQueue
				&& ekPrediction.roundQueue.q.Count != 0)
			{
				if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.NewRound))
				{
					Debug.LogFormat(
						"Mod {0} ({1}) PlaceRoundSystem pending rounds -- new turn, round queue not empty",
						ModLink.modIndex,
						ModLink.modID);
				}
				ekPrediction.isPendingPlaceRound = true;
			}
			base.Execute();
		}

		protected override void Execute(List<ECS.EkPredictionEntity> entities)
		{
			if (!ekPrediction.hasRoundQueue)
			{
				if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.NewRound))
				{
					Debug.LogFormat(
						"Mod {0} ({1}) PlaceRoundSystem early exit -- no round queue | index: {2}/{3:F3}s",
						ModLink.modIndex,
						ModLink.modID,
						ekTime.currentTimeSlice.i,
						ekTime.currentTimeSlice.i * ekTime.timeStep.f);
				}
				ekPrediction.isPendingPlaceRound = false;
				return;
			}

			if (ekPrediction.roundQueue.q.Count == 0)
			{
				if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.NewRound))
				{
					Debug.LogFormat(
						"Mod {0} ({1}) PlaceRoundSystem early exit -- empty round queue | index: {2}/{3:F3}s",
						ModLink.modIndex,
						ModLink.modID,
						ekTime.currentTimeSlice.i,
						ekTime.currentTimeSlice.i * ekTime.timeStep.f);
				}
				ekPrediction.isPendingPlaceRound = false;
				return;
			}

			lastDelta = float.MaxValue;

			SchedulePlaceRound();
		}

		private void SchedulePlaceRound()
		{
			var turnStart = combat.currentTurn.i * combat.turnLength.i;
			if (ekPrediction.roundQueue.q.Count == 0)
			{
				round = null;
				if (ekTime.hasCurrentTimeTarget)
				{
					if (ekTime.currentTimeTarget.f < turnStart)
					{
						if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.NewRound))
						{
							Debug.LogFormat(
								"Mod {0} ({1}) PlaceRoundSystem -- current time target in previous turn | turn start: {2:F3}s | time target: {3:F3}s",
								ModLink.modIndex,
								ModLink.modID,
								turnStart,
								ekTime.currentTimeTarget.f);
						}
						ekTime.ReplaceCurrentTimeTarget(turnStart);
					}
					combat.ReplacePredictionTimeTarget(ekTime.currentTimeTarget.f);
					ekTime.RemoveCurrentTimeTarget();
				}
				ekPrediction.isPendingPlaceRound = false;
				if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.NewRound)
					&& ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Trace))
				{
					Debug.LogFormat(
						"Mod {0} ({1}) PlaceRoundSystem schedule round -- restore combat sensor | index: {2}/{3:F3}s",
						ModLink.modIndex,
						ModLink.modID,
						ekTime.currentTimeSlice.i,
						ekTime.currentTimeSlice.i * ekTime.timeStep.f);
				}
				combat.ReplaceSensor(Vector3.zero);
				return;
			}

			round = ekPrediction.roundQueue.q.Dequeue();
			if (round.StartTime < turnStart)
			{
				// XXX round should have been place last turn.
				Debug.LogWarningFormat(
					"Mod {0} ({1}) PlaceRoundSystem -- skipping round that should have been placed in previous turn | ID: {2}{3} | round seqno: {4} | round start: {5:F3}s | turn start: {6:F3}s",
					ModLink.modIndex,
					ModLink.modID,
					round.Predicted.projectileLink.combatID < 0 ? "LIT" : "C-",
					round.Predicted.projectileLink.combatID,
					round.SequenceNumber,
					round.StartTime,
					turnStart);
				round.Predicted.isPlaced = true;
				SchedulePlaceRound();
				return;
			}

			var turnEnd = turnStart + combat.turnLength.i;
			if (round.StartTime >= turnEnd)
			{
				var sorted = new List<RoundInfo>(ekPrediction.roundQueue.q)
				{
					round
				};
				sorted.Sort(RoundInfo.Compare);
				if (sorted[0].StartTime < turnEnd)
				{
					ekPrediction.ReplaceRoundQueue(new Queue<RoundInfo>(sorted));
					SchedulePlaceRound();
				}
				else
				{
					ReindexStartTimes(sorted, turnEnd);
					ekPrediction.ReplaceRoundQueue(new Queue<RoundInfo>(sorted));
					if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.NewRound))
					{
						Debug.LogFormat(
							"Mod {0} ({1}) PlaceRoundSystem -- round placement deferred to next turn | count: {2}",
							ModLink.modIndex,
							ModLink.modID,
							ekPrediction.roundQueue.q.Count);
					}

					ekPrediction.isPendingPlaceRound = false;
					if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.NewRound)
						&& ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Trace))
					{
						Debug.LogFormat(
							"Mod {0} ({1}) PlaceRoundSystem schedule round -- restore combat sensor | index: {2}/{3:F3}s",
							ModLink.modIndex,
							ModLink.modID,
							ekTime.currentTimeSlice.i,
							ekTime.currentTimeSlice.i * ekTime.timeStep.f);
					}
					combat.ReplaceSensor(Vector3.zero);
				}
				return;
			}

			Co.DelayFrames(delay, PlaceRoundDelayed);
			if (combat.hasSensor)
			{
				if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.NewRound)
					&& ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Trace))
				{
					Debug.LogFormat(
						"Mod {0} ({1}) PlaceRoundSystem schedule round -- remove combat sensor | index: {2}/{3:F3}s",
						ModLink.modIndex,
						ModLink.modID,
						ekTime.currentTimeSlice.i,
						ekTime.currentTimeSlice.i * ekTime.timeStep.f);
				}
				combat.RemoveSensor();
			}

			if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.NewRound)
				&& ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Trace))
			{
				Debug.LogFormat(
					"Mod {0} ({1}) PlaceRoundSystem schedule round -- setting prediction time target | current time: {2:F3}s | target time: {3:F3}s | ID: {4}{5} | round seqno: {6}",
					ModLink.modIndex,
					ModLink.modID,
					combat.predictionTime.f,
					round.StartTime,
					round.Predicted.projectileLink.combatID < 0 ? "LIT" : "C-",
					round.Predicted.projectileLink.combatID,
					round.SequenceNumber);
			}
			combat.ReplacePredictionTimeTarget(round.StartTime);
		}

		private void ReindexStartTimes(List<RoundInfo> rounds, float turnEnd)
		{
			foreach (var round in rounds)
			{
				var relativeStartTime = round.StartTime - turnEnd;
				round.StartIndex = Mathf.FloorToInt(relativeStartTime * ekTime.slicesPerSecond.i);
				if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.NewRound))
				{
					Debug.LogFormat(
						"Mod {0} ({1}) PlaceRoundSystem -- reindex | ID: {2}{3} | round seqno: {4} | start time: {5:F3}s | index: {6}",
						ModLink.modIndex,
						ModLink.modID,
						round.Predicted.projectileLink.combatID < 0 ? "LIT" : "C-",
						round.Predicted.projectileLink.combatID,
						round.SequenceNumber,
						round.StartTime,
						round.StartIndex);
				}
			}
		}

		private void PlaceRoundDelayed()
		{
			if (PlaceRound())
			{
				lastDelta = float.MaxValue;
				SchedulePlaceRound();
			}
		}

		public bool PlaceRound()
		{
			if (!round.Predicted.isEnabled)
			{
				return true;
			}

			if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.NewRound)
				&& ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Trace))
			{
				Debug.LogFormat(
					"Mod {0} ({1}) PlaceRoundSystem -- placing round | ID: {2} | round seqno: {3} | prediction time: {4:F3}s | round time: {5:F3}s",
					ModLink.modIndex,
					ModLink.modID,
					"LIT" + round.Predicted.projectileLink.combatID,
					round.SequenceNumber,
					combat.predictionTime.f,
					round.StartTime);
			}

			var delta = Mathf.Abs(round.StartTime - combat.predictionTime.f);
			if (delta > lastDelta)
			{
				if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.NewRound))
				{
					Debug.LogWarningFormat(
						"Mod {0} ({1}) PlaceRoundSystem -- round skipped, not converging on launch time | ID: {2} | round seqno: {3}",
						ModLink.modIndex,
						ModLink.modID,
						"LIT" + round.Predicted.projectileLink.combatID,
						round.SequenceNumber);
				}
				round.Predicted.Destroy();
				return true;
			}
			lastDelta = delta;

			if (!combat.predictionTime.f.RoughlyEqual(round.StartTime, timeTargetThreshold))
			{
				if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.NewRound)
					&& ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Trace))
				{
					Debug.LogFormat(
						"Mod {0} ({1}) PlaceRoundSystem -- reschedule because prediction time not at launch time | ID: {2} | round seqno: {3} | prediction: {4:F3}s | launch: {5:F3}s",
						ModLink.modIndex,
						ModLink.modID,
						"LIT" + round.Predicted.projectileLink.combatID,
						round.SequenceNumber,
						combat.predictionTime.f,
						round.StartTime);
				}
				Co.DelayFrames(delay, PlaceRoundDelayed);
				return false;
			}

			var projectedUnitVelocity = GetUnitVelocityAtTime(round.StartTime, round.CombatSource);
			var targetInfo = AcquireTarget(
				round.StartTime,
				round.Action,
				round.CombatSource,
				round.Part,
				round.Subsystem);

			AddPositions(
				targetInfo.CombatTargetID,
				targetInfo.FiringPoint,
				targetInfo.TargetedPosition,
				round.Predicted);
			round.Predicted.ReplaceFlightInfo(0f, 0f, targetInfo.FiringPoint, targetInfo.FiringPoint);
			CreateRigidbody(
				round.Part.dataKeyPartPreset.s,
				round.Predicted.guidanceData.data,
				projectedUnitVelocity,
				targetInfo.FiringPoint,
				targetInfo.FiringDirection,
				targetInfo.WeaponSpeed,
				round.Predicted);
			InitializeMotionSlices(round.Predicted, round.StartIndex);
			UpdateMotion(
				targetInfo.TargetedPosition,
				round.Predicted,
				round.StartIndex);
			round.Predicted.isPlaced = true;

			if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Link))
			{
				Debug.LogFormat(
					"Mod {0} ({1}) placed prediction double | time: {2}/{3:F3}s | ID: LIT{4} | source unit: C-{5} | firing point: {6:F1} | targeted unit: {7} | targeted position: {8:F1}",
					ModLink.modIndex,
					ModLink.modID,
					round.StartIndex,
					round.StartTime,
					round.Predicted.projectileLink.combatID,
					round.CombatSource.id.id,
					targetInfo.FiringPoint,
					round.Predicted.hasTargetEntityLink ? ("C-" + round.Predicted.targetEntityLink.combatID) : "<none>",
					round.Predicted.hasTargetPosition ? round.Predicted.targetPosition.v : Vector3.zero);
			}

			return true;
		}

		private TargetInfo AcquireTarget(
			float roundStartTime,
			ActionEntity action,
			CombatEntity combatSource,
			EquipmentEntity part,
			EquipmentEntity subsystem)
		{
			PathUtility.GetProjectedTransformAtTime(combatSource, roundStartTime, out var position, out _);
			position += combatSource.hasLocalCenterPoint
				? combatSource.localCenterPoint.v
				: DataShortcuts.anim.firingCenterOffset;
			var targetedPosition = action.hasTargetedPoint
				? action.targetedPoint.v + DataShortcuts.anim.firingCenterOffset
				: Vector3.zero;
			var direction = Utilities.GetDirection(position, targetedPosition);
			var adjustDirection = true;
			var visual = subsystem.dataLinkSubsystem.data.activationProcessed?.visual;
			if (visual != null)
			{
				var socket = !string.IsNullOrEmpty(visual.localSocketOverride)
					? visual.localSocketOverride
					: part.partParentUnit.socket;
				var hardpoint = !string.IsNullOrEmpty(visual.localHardpointOverride)
					? visual.localHardpointOverride
					: subsystem.subsystemParentPart.hardpoint;
				var go = combatSource.projectionView.view.gameObject;
				var uvm = go.GetComponentInChildren<IUnitVisualManager>();
				var fxTransform = uvm?.GetFXTransform(socket, hardpoint);
				if (fxTransform != null)
				{
					position = fxTransform.position;
					direction = fxTransform.forward;
					adjustDirection = false;
				}
			}

			var combatTargetID = IDUtility.invalidID;
			var combatTarget = action.hasTargetedEntity
				? IDUtility.GetCombatEntity(action.targetedEntity.combatID)
				: null;
			var wpnSpeed = Mathf.Max(1f, DataHelperStats.GetCachedStatForPart(UnitStats.weaponProjectileSpeed, part));
			if (action.hasTargetedEntity && combatTarget != null)
			{
				targetedPosition = AdjustTargetedPoint(
					roundStartTime,
					combatTarget,
					wpnSpeed,
					position,
					targetedPosition);
				if (combatTarget.hasPosition)
				{
					combatTargetID = combatTarget.id.id;
				}
				if (adjustDirection)
				{
					direction = Utilities.GetDirection(position, targetedPosition);
				}
			}

			return new TargetInfo()
			{
				CombatTargetID = combatTargetID,
				FiringPoint = position,
				FiringDirection = direction,
				TargetedPosition = targetedPosition,
				WeaponSpeed = wpnSpeed,
			};
		}

		private Vector3 GetUnitVelocityAtTime(float time, CombatEntity unit)
		{
			var startTime = Mathf.FloorToInt(time / ekTime.slicesPerSecond.i) * ekTime.timeStep.f;
			var endTime = Mathf.CeilToInt(time / ekTime.slicesPerSecond.i) * ekTime.timeStep.f;
			PathUtility.GetProjectedTransformAtTime(unit, startTime, out var startPosition, out _);
			PathUtility.GetProjectedTransformAtTime(unit, endTime, out var endPosition, out _);
			return (endPosition - startPosition) / ekTime.timeStep.f;
		}

		private Vector3 AdjustTargetedPoint(
			float roundStartTime,
			CombatEntity combatTarget,
			float wpnSpeed,
			Vector3 firingPoint,
			Vector3 targetedPoint)
		{
			if (DataShortcuts.sim.pauseFiringOnMelee
				&& combatTarget.hasCurrentMeleeAction
				&& IDUtility.GetActionEntity(combatTarget.currentMeleeAction.actionID) != null)
			{
				return targetedPoint;
			}

			var targetVelocity = Vector3.zero;
			if (combatTarget.hasPosition)
			{
				PathUtility.GetProjectedTransformAtTime(combatTarget, roundStartTime, out var projectedPosition, out _);
				targetedPoint = projectedPosition;
				if (combatTarget.hasLocalCenterPoint)
				{
					targetedPoint += combatTarget.localCenterPoint.v;
				}
				targetVelocity = GetUnitVelocityAtTime(roundStartTime, combatTarget);
			}

			if (!combatTarget.hasVelocity)
			{
				return targetedPoint;
			}
			if (targetVelocity.sqrMagnitude.RoughlyEqual(0f))
			{
				return targetedPoint;
			}
			if (wpnSpeed < 50f)
			{
				return targetedPoint;
			}

			var originalPoint = targetedPoint;
			var v = targetVelocity * DataLinkerSettingsSimulation.data.targetVelocityModifier;
			var trackingIterations = DataLinkerSettingsSimulation.data.targetTrackingIterations;
			for (int i = 0; i < trackingIterations; i += 1)
			{
				var distance = targetedPoint - firingPoint;
				var dt = distance.magnitude / wpnSpeed;
				targetedPoint = originalPoint + v * dt;
			}

			return targetedPoint;
		}

		private static void AddPositions(
			int combatTargetID,
			Vector3 firingPoint,
			Vector3 targetedPosition,
			ECS.EkPredictionEntity predicted)
		{
			predicted.ReplaceStartPosition(firingPoint);
			predicted.ReplaceTargetOffset(Vector2.zero);
			if (combatTargetID != IDUtility.invalidID)
			{
				predicted.ReplaceTargetEntityLink(combatTargetID);
			}
			predicted.ReplaceTargetPosition(targetedPosition);
		}

		private static void CreateRigidbody(
			string partKey,
			DataBlockGuidanceData guidanceData,
			Vector3 sourceVelocity,
			Vector3 firingPoint,
			Vector3 firingDirection,
			float wpnSpeed,
			ECS.EkPredictionEntity predicted)
		{
			var velocity = sourceVelocity + firingDirection * wpnSpeed;
			if (predicted.hasAuthoritativeRigidbody)
			{
				predicted.authoritativeRigidbody.rb.transform.position = firingPoint;
				predicted.authoritativeRigidbody.rb.transform.forward = firingDirection;
				predicted.authoritativeRigidbody.rb.velocity = velocity;
				return;
			}

			var gameObject = new GameObject();
			var transform = gameObject.transform;
			transform.name = $"rb_projectile_({partKey})";
			transform.position = firingPoint;
			transform.forward = firingDirection;

			var rb = gameObject.AddComponent<Rigidbody>();
			rb.interpolation = RigidbodyInterpolation.Interpolate;
			rb.mass = guidanceData.rigidbodyMass;
			rb.drag = guidanceData.rigidbodyDrag;
			rb.angularDrag = guidanceData.rigidbodyAngularDrag;
			rb.velocity = velocity;
			rb.useGravity = false;
			rb.constraints = RigidbodyConstraints.FreezeRotationZ;
			predicted.AddAuthoritativeRigidbody(rb);
		}

		private void InitializeMotionSlices(ECS.EkPredictionEntity predicted, int startIndex)
		{
			if (!predicted.hasMotionTimeSlices)
			{
				var ary = new MotionTimeSlice[ekTime.sampleCount.i];
				predicted.AddMotionTimeSlices(ary);
				if (ModLink.Settings.extraMotionData)
				{
					predicted.AddMotionExtraData(new MotionExtraInfo[ekTime.sampleCount.i]);
				}
				return;
			}

			var timeSlices = predicted.motionTimeSlices.a;
			for (var i = 0; i < startIndex; i += 1)
			{
				var ts = timeSlices[i];
				ts.Status = TimeSliceStatus.Uninitialized;
				timeSlices[i] = ts;
			}

			if (!ModLink.Settings.extraMotionData)
			{
				if (predicted.hasMotionExtraData)
				{
					predicted.RemoveMotionExtraData();
				}
				return;
			}

			if (!predicted.hasMotionExtraData)
			{
				predicted.AddMotionExtraData(new MotionExtraInfo[ekTime.sampleCount.i]);
				return;
			}

			var extraData = predicted.motionExtraData.a;
			for (var i = 0; i < extraData.Length; i += 1)
			{
				var ed = extraData[i];
				ed.IsValid = false;
				extraData[i] = ed;
			}
		}

		private static void UpdateMotion(
			Vector3 targetedPosition,
			ECS.EkPredictionEntity predicted,
			int timeSlice)
		{
			if (timeSlice < 0 || timeSlice >= predicted.motionTimeSlices.a.Length)
			{
				return;
			}

			var position = predicted.authoritativeRigidbody.rb.transform.position;

			var ts = predicted.motionTimeSlices.a[timeSlice];
			ts.Status = TimeSliceStatus.Active;
			ts.Position = position;
			ts.Facing = predicted.authoritativeRigidbody.rb.transform.forward;
			ts.Rotation = predicted.authoritativeRigidbody.rb.transform.rotation;
			ts.Velocity = predicted.authoritativeRigidbody.rb.velocity;
			ts.DriverInputYaw = 0f;
			ts.DriverInputPitch = 0f;
			ts.DriverInputThrottle = 1f;
			ts.GuidanceSuspensionTime = 0f;
			ts.Progress = 0f;
			ts.TargetBlend = 0f;
			ts.TargetHeight = position.y;
			ts.TargetPosition = targetedPosition;
			ts.TimeToLive = predicted.timeToLive.f;
			ts.IsTrackingActive = true;
			ts.IsGuided = true;
			predicted.motionTimeSlices.a[timeSlice] = ts;
			predicted.ReplaceSliceIndex(timeSlice);
		}
	}
}
