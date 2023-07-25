// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;

using PhantomBrigade;
using PhantomBrigade.Data;

using UnityEngine;

namespace EchKode.PBMods.MissilePrediction
{
	static class PredictionDoubleFunctions
	{
		private static readonly List<RoundInfo> queuedRounds = new List<RoundInfo>();

		internal static AssetLinker AddAsset(ECS.EkPredictionEntity predicted)
		{
			if (!predicted.hasAssetKey)
			{
				return null;
			}

			var entry = DataMultiLinkerAssetPools.GetEntry(predicted.assetKey.key);
			if (entry == null)
			{
				if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Link))
				{
					var projectile = IDUtility.GetCombatEntity(predicted.projectileLink.combatID);
					var combatSource = projectile.hasSourceEntity
						? "C-" + projectile.sourceEntity.combatID
						: "<unknown>";
					Debug.LogFormat(
						"Mod {0} ({1}) asset not found | ID: {3}{4} | source unit: {2} | asset key: {5}",
						ModLink.modIndex,
						ModLink.modID,
						combatSource,
						predicted.projectileLink.combatID < 0 ? "LIT" : "C-",
						predicted.projectileLink.combatID,
						predicted.assetKey.key);
				}
				return null;
			}

			var instance = entry.GetInstance();
			if (instance == null)
			{
				if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Link))
				{
					var projectile = IDUtility.GetCombatEntity(predicted.projectileLink.combatID);
					var combatSource = projectile.hasSourceEntity
						? "C-" + projectile.sourceEntity.combatID
						: "<unknown>";
					Debug.LogFormat(
						"Mod {0} ({1}) failed to get asset instance from pool | ID: {3}{4} | source unit: {2} | asset key: {5}",
						ModLink.modIndex,
						ModLink.modID,
						combatSource,
						predicted.projectileLink.combatID < 0 ? "LIT" : "C-",
						predicted.projectileLink.combatID,
						predicted.assetKey.key);
				}
				return null;
			}
			predicted.AddAsset(instance);

			instance.fxHelperProjectile?.SetAll(0.0f, 1f, 1f);
			if (instance.trail != null)
			{
				instance.trail.Clear();
				instance.trail.emit = false;
			}
			foreach (var collisionSystem in instance.collisionSystems)
			{
				var c = collisionSystem.collision;
				c.enabled = false;
			}
			foreach (var noiseSystem in instance.noiseSystems)
			{
				var n = noiseSystem.noise;
				n.enabled = false;
			}
			instance.Play(true);

			return instance;
		}

		internal enum RecalculationReason
		{
			Cancelled,
			Repath,
		}

		internal static void MarkForRecalculation(ECS.EkPredictionEntity predicted, ActionEntity action)
		{
			if (predicted.isLaunchedInTurn && !predicted.isPlaced)
			{
				return;
			}

			if (!predicted.hasMotionTimeSlices)
			{
				Debug.LogWarningFormat(
					"Mod {0} ({1}) recalc -- no motion time slice array | projectile: {2}{3}",
					ModLink.modIndex,
					ModLink.modID,
					predicted.projectileLink.combatID < 0 ? "LIT" : "C-",
					predicted.projectileLink.combatID);
				return;
			}

			var sliceIndex = 0;
			for (; sliceIndex < predicted.motionTimeSlices.a.Length; sliceIndex += 1)
			{
				if (predicted.motionTimeSlices.a[sliceIndex].Status != TimeSliceStatus.Uninitialized)
				{
					break;
				}
			}

			if (sliceIndex == predicted.motionTimeSlices.a.Length)
			{
				return;
			}

			ReleaseAsset(predicted);
			ResetRigidbody(predicted, sliceIndex);
			for (var i = sliceIndex + 1; i < predicted.motionTimeSlices.a.Length; i += 1)
			{
				var ts = predicted.motionTimeSlices.a[i];
				ts.Status = TimeSliceStatus.Recalculate;
				predicted.motionTimeSlices.a[i] = ts;
			}
			predicted.ReplaceSliceIndex(sliceIndex);
			predicted.isPredictionMotionReady = false;

			if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Recalc))
			{
				Debug.LogFormat(
					"Mod {0} ({1}) marked for recalc | projectile: {2}{3} | start: {4} | source: C-{5} | target: {6} | action ID: {7} | action owner: C-{8} | reason: {9}",
					ModLink.modIndex,
					ModLink.modID,
					predicted.projectileLink.combatID < 0 ? "LIT" : "C-",
					predicted.projectileLink.combatID,
					sliceIndex,
					predicted.combatSourceLink.combatID,
					predicted.hasTargetEntityLink
						? "C-" + predicted.targetEntityLink.combatID
						: "<position>",
					action.id.id,
					action.actionOwner.combatID,
					action.isDisposed
						? "Cancelled"
						: action.isMovementPathChanged
							? "Repath"
							: "Other");
			}

			if (!predicted.isLaunchedInTurn)
			{
				return;
			}

			var attackAction = IDUtility.GetActionEntity(predicted.actionLink.actionID);
			if (attackAction == null)
			{
				return;
			}
			if (action.startTime.f > attackAction.startTime.f + attackAction.duration.f)
			{
				return;
			}
			QueueRoundForPlacement(predicted);
		}

		internal static void ResetRoundPlacement(ECS.EkPredictionEntity predicted)
		{
			if (!predicted.isLaunchedInTurn)
			{
				return;
			}

			if (!predicted.isPlaced)
			{
				ResetQueuedStartTime(predicted);
				return;
			}

			for (var i = 0; i < predicted.motionTimeSlices.a.Length; i += 1)
			{
				var ts = predicted.motionTimeSlices.a[i];
				ts.Status = TimeSliceStatus.Recalculate;
				predicted.motionTimeSlices.a[i] = ts;
			}
			predicted.isPredictionMotionReady = false;
			ReleaseAsset(predicted);

			if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Recalc))
			{
				Debug.LogFormat(
					"Mod {0} ({1}) placement reset | projectile: {2}{3} | reason: Action Drag",
					ModLink.modIndex,
					ModLink.modID,
					predicted.projectileLink.combatID < 0 ? "LIT" : "C-",
					predicted.projectileLink.combatID);
			}

			QueueRoundForPlacement(predicted);
		}

		static void ResetQueuedStartTime(ECS.EkPredictionEntity predicted)
		{
			queuedRounds.Clear();
			var queue = ECS.Contexts.sharedInstance.ekPrediction.roundQueue.q;
			var found = false;
			while (queue.Count != 0)
			{
				var elem = queue.Dequeue();
				if (predicted == elem.Predicted)
				{
					found = true;
					elem.StartTime = predicted.roundStartTime.f;
					elem.StartIndex = Mathf.FloorToInt(
						(predicted.roundStartTime.f - Contexts.sharedInstance.combat.simulationTime.f)
							* ECS.Contexts.sharedInstance.ekTime.slicesPerSecond.i);
				}
				queuedRounds.Add(elem);
			}
			if (found)
			{
				queuedRounds.Sort(RoundInfo.Compare);
			}
			ECS.Contexts.sharedInstance.ekPrediction.ReplaceRoundQueue(new Queue<RoundInfo>(queuedRounds));
		}

		static void QueueRoundForPlacement(ECS.EkPredictionEntity predicted)
		{
			if (predicted.hasSliceIndex)
			{
				predicted.RemoveSliceIndex();
			}
			if (predicted.hasAuthoritativeRigidbody)
			{
				Object.Destroy(predicted.authoritativeRigidbody.rb.gameObject);
				predicted.RemoveAuthoritativeRigidbody();
			}
			predicted.isPlaced = false;

			var ekTime = ECS.Contexts.sharedInstance.ekTime;
			var attackAction = IDUtility.GetActionEntity(predicted.actionLink.actionID);
			var combatSource = IDUtility.GetCombatEntity(attackAction.actionOwner.combatID);
			var part = IDUtility.GetEquipmentEntity(attackAction.activeEquipmentPart.equipmentID);
			var subsystem = IDUtility.GetEquipmentEntity(part.primaryActivationSubsystem.equipmentID);
			var roundInfo = new RoundInfo()
			{
				Predicted = predicted,
				Action = attackAction,
				CombatSource = combatSource,
				Part = part,
				Subsystem = subsystem,
				SequenceNumber = predicted.roundSequenceNumber.i,
				StartTime = predicted.roundStartTime.f,
				StartIndex = Mathf.FloorToInt(
					(predicted.roundStartTime.f - Contexts.sharedInstance.combat.simulationTime.f)
						* ekTime.slicesPerSecond.i),
			};

			if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Recalc))
			{
				Debug.LogFormat(
					"Mod {0} ({1}) recalc -- re-queuing for placement | projectile: {2}{3} | start: {4}/{5:F3}s",
					ModLink.modIndex,
					ModLink.modID,
					predicted.projectileLink.combatID < 0 ? "LIT" : "C-",
					predicted.projectileLink.combatID,
					roundInfo.StartIndex,
					roundInfo.StartTime);
			}

			var ekPrediction = ECS.Contexts.sharedInstance.ekPrediction;
			if (ekPrediction.isPendingPlaceRound)
			{
				ekPrediction.roundQueue.q.Enqueue(roundInfo);
				return;
			}

			if (!ekPrediction.hasRoundQueue)
			{
				ekPrediction.SetRoundQueue(new Queue<RoundInfo>());
			}
			ekPrediction.roundQueue.q.Enqueue(roundInfo);
			ekPrediction.isPendingPlaceRound = true;

			if (!ekTime.hasCurrentTimeTarget)
			{
				ekTime.ReplaceCurrentTimeTarget(Contexts.sharedInstance.combat.predictionTimeTarget.f);
			}
		}

		static void ResetRigidbody(ECS.EkPredictionEntity predicted, int sliceIndex)
		{
			var timeSlice = predicted.motionTimeSlices.a[sliceIndex];
			var rb = predicted.authoritativeRigidbody.rb;
			rb.transform.position = timeSlice.Position;
			rb.transform.rotation = timeSlice.Rotation;
			rb.velocity = timeSlice.Velocity;
		}

		internal static void DestroyPredicted(ECS.EkPredictionEntity predicted)
		{
			DestroyRigidbody(predicted);
			ReleaseAsset(predicted);
			predicted.Destroy();
		}

		static void DestroyRigidbody(ECS.EkPredictionEntity predicted)
		{
			if (!predicted.hasAuthoritativeRigidbody)
			{
				return;
			}

			var go = predicted.authoritativeRigidbody.rb?.gameObject;
			predicted.RemoveAuthoritativeRigidbody();

			if (go == null)
			{
				return;
			}

			Object.Destroy(go);
		}

		internal static void ReleaseAsset(ECS.EkPredictionEntity predicted)
		{
			if (!predicted.hasAsset)
			{
				return;
			}

			var instance = predicted.asset.instance;
			predicted.RemoveAsset();

			if (instance == null)
			{
				return;
			}

			instance.ReturnToPool(forceParentDeactivation: true);
		}

		internal static int Compare(ECS.EkPredictionEntity x, ECS.EkPredictionEntity y) =>
			x.isLaunchedInTurn && !y.isLaunchedInTurn
				? 1
				: !x.isLaunchedInTurn && y.isLaunchedInTurn
					? -1
					: x.isLaunchedInTurn && y.isLaunchedInTurn
						? x.roundStartTime.f.CompareTo(y.roundStartTime.f) != 0
							? x.roundStartTime.f.CompareTo(y.roundStartTime.f)
							: -x.projectileLink.combatID.CompareTo(y.projectileLink.combatID)
						: x.projectileLink.combatID.CompareTo(y.projectileLink.combatID);
	}
}
