using System.Collections.Generic;
using System.Reflection;
using System.Text;

using HarmonyLib;

using PhantomBrigade;
using PhantomBrigade.Data;

using QFSW.QC;

using UnityEngine;

namespace EchKode.PBMods.MissilePrediction.Diagnostics
{
	using CommandList = List<(string QCName, string Description, MethodInfo Method)>;

	static partial class PredictionInspector
	{
		internal static CommandList Commands() => new CommandList()
		{
			("list-projectiles", "List active projectiles", AccessTools.DeclaredMethod(typeof(PredictionInspector), nameof(ListProjectiles))),
			("list-predictions", "List prediction doubles", AccessTools.DeclaredMethod(typeof(PredictionInspector), nameof(ListPredictedProjectiles))),
			("show-motion-prediction", "Show motion table for a prediction double", AccessTools.DeclaredMethod(typeof(PredictionInspector), nameof(ShowTable), new System.Type[] { typeof(string) })),
			("show-motion-prediction", "Show motion table for a prediction double", AccessTools.DeclaredMethod(typeof(PredictionInspector), nameof(ShowTable), new System.Type[] { typeof(string), typeof(bool) })),
			("show-motion-prediction", "Show motion table for a prediction double", AccessTools.DeclaredMethod(typeof(PredictionInspector), nameof(ShowTable), new System.Type[] { typeof(string), typeof(int), typeof(int) })),
			("show-motion-prediction", "Show motion table for a prediction double", AccessTools.DeclaredMethod(typeof(PredictionInspector), nameof(ShowTable), new System.Type[] { typeof(string), typeof(int), typeof(int), typeof(bool) })),
			("show-prediction-details", "Show details about a prediction double", AccessTools.DeclaredMethod(typeof(PredictionInspector), nameof(ShowPredictionDetails), new System.Type[] { typeof(string) })),
			("toggle-extra-motion-data", "Toggle setting for tracking extra motion data", AccessTools.DeclaredMethod(typeof(PredictionInspector), nameof(ToggleExtraMotionData))),
			("show-motion-data", "Show calculations for motion table", AccessTools.DeclaredMethod(typeof(PredictionInspector), nameof(ShowMotionCalculations))),
			("save-motion-data", "Save calculations for motion table", AccessTools.DeclaredMethod(typeof(PredictionInspector), nameof(SaveMotionCalculations))),
		};

		static void ListProjectiles()
		{
			if (!CombatStateCheck())
			{
				return;
			}

			foreach (var entity in Contexts.sharedInstance.combat.GetEntities())
			{
				if (entity.isDestroyed)
				{
					continue;
				}
				if (!entity.hasSourceEntity)
				{
					continue;
				}
				if (!entity.hasDataLinkSubsystemProjectile || entity.dataLinkSubsystemProjectile == null)
				{
					continue;
				}

				var viable = !entity.hasProjectileDestructionPosition;
				var ballistic = entity.dataLinkSubsystemProjectile.data.ballistics != null;
				var guided = entity.dataLinkSubsystemProjectile.data.guidanceData != null;
				var subsystem = entity.hasParentSubsystem
					? IDUtility.GetEquipmentEntity(entity.parentSubsystem.equipmentID)
					: null;
				var sskey = subsystem != null
					? subsystem.hasDataKeySubsystem
						? subsystem.dataKeySubsystem.s
						: "<unknown>"
					: "<unknown>";
				var s = string.Format(
					"C-{0}/C-{1}: {2}{3} ({4})",
					entity.id.id,
					entity.sourceEntity.combatID,
					ballistic
						? "B"
						: guided
							? "G"
							: "-",
					viable ? "L" : "D",
					sskey);
				QuantumConsole.Instance.LogToConsole(s);
			}
		}

		static void ListPredictedProjectiles()
		{
			if (!CombatStateCheck())
			{
				return;
			}

			var predictedDoubles = new List<ECS.EkPredictionEntity>();
			foreach (var predicted in ECS.Contexts.sharedInstance.ekPrediction.GetEntities())
			{
				if (!predicted.isEnabled)
				{
					return;
				}
				if (!predicted.hasProjectileLink)
				{
					continue;
				}
				predictedDoubles.Add(predicted);
			}

			predictedDoubles.Sort(PredictionDoubleFunctions.Compare);
			foreach (var predicted in predictedDoubles)
			{
				var combatID = predicted.projectileLink.combatID;
				var projectile = IDUtility.GetCombatEntity(combatID);
				if (projectile == null && !predicted.isLaunchedInTurn)
				{
					QuantumConsole.Instance.LogToConsole($"C-{combatID}/C-{predicted.combatSourceLink.combatID}: prediction double without matching projectile");
					continue;
				}
				if (predicted.isLaunchedInTurn)
				{
					var startIndex = Mathf.FloorToInt(
						(predicted.roundStartTime.f - Contexts.sharedInstance.combat.simulationTime.f)
							* ECS.Contexts.sharedInstance.ekTime.slicesPerSecond.i);
					if (!predicted.hasMotionTimeSlices)
					{
						QuantumConsole.Instance.LogToConsole($"LIT{combatID}/C-{predicted.combatSourceLink.combatID} ({startIndex}/{predicted.roundStartTime.f:F3}s): prediction double without motion time slices");
						continue;
					}
					var placed = predicted.isPlaced ? ": placed" : "";
					QuantumConsole.Instance.LogToConsole($"LIT{combatID}/C-{predicted.combatSourceLink.combatID} ({startIndex}/{predicted.roundStartTime.f:F3}s){placed}");
					continue;
				}
				if (!predicted.hasMotionTimeSlices)
				{
					QuantumConsole.Instance.LogToConsole($"C-{combatID}/C-{predicted.combatSourceLink.combatID}: prediction double without motion time slices");
					continue;
				}
				QuantumConsole.Instance.LogToConsole($"C-{combatID}/C-{predicted.combatSourceLink.combatID}");
			}
		}

		static void ShowTable(string predictionReference)
		{
			ShowTable(predictionReference, 0, ECS.Contexts.sharedInstance.ekTime.sampleCount.i - 1);
		}

		static void ShowTable(string predictionReference, bool activeOnly)
		{
			ShowTable(predictionReference, 0, ECS.Contexts.sharedInstance.ekTime.sampleCount.i - 1, activeOnly);
		}

		static void ShowTable(string predictionReference, int startIndex, int endIndex)
		{
			ShowTable(predictionReference, startIndex, endIndex, false);
		}

		static void ShowTable(string predictionReference, int startIndex, int endIndex, bool activeOnly)
		{
			var (ok, predicted) = PredictionCheck(predictionReference);
			if (!ok)
			{
				return;
			}
			if (!predicted.hasMotionTimeSlices)
			{
				QuantumConsole.Instance.LogToConsole("No motion time slices for prediction double " + predictionReference);
				return;
			}

			var turnStart = Contexts.sharedInstance.combat.simulationTime.f;
			QuantumConsole.Instance.LogToConsole($"Turn start: {turnStart:F3}s");
			QuantumConsole.Instance.LogToConsole("I: S P TP TH TTL PG IP IY IT V F");
			var sb = new StringBuilder();
			startIndex = Mathf.Clamp(startIndex, 0, predicted.motionTimeSlices.a.Length - 1);
			endIndex = Mathf.Clamp(endIndex, startIndex, predicted.motionTimeSlices.a.Length - 1);
			for (var i = startIndex; i <= endIndex; i += 1)
			{
				var slice = predicted.motionTimeSlices.a[i];
				if (slice.Status != TimeSliceStatus.Active && activeOnly)
				{
					endIndex = System.Math.Min(endIndex + 1, predicted.motionTimeSlices.a.Length - 1);
					continue;
				}

				sb.AppendFormat("{0:000}/{1:F3}s:", i, i * ECS.Contexts.sharedInstance.ekTime.timeStep.f)
					.AppendFormat(" {0}", slice.Status)
					.AppendFormat(" {0:F1}", slice.Position)
					.AppendFormat(" {0:F1}", slice.TargetPosition)
					.AppendFormat(" {0:F1}", slice.TargetHeight)
					.AppendFormat(" {0:F3}s", slice.TimeToLive)
					.AppendFormat(" {0:F2}", slice.Progress)
					.AppendFormat(" {0}", slice.IsTrackingActive ? "T" : "-")
					.AppendFormat("{0}", slice.IsGuided ? "G" : "-")
					.AppendFormat("{0}", slice.DestroyedBy == DestructionReason.Grounded
						? "g"
						: slice.DestroyedBy == DestructionReason.Expired
						? "x"
						: slice.DestroyedBy == DestructionReason.Proximity
							? "p"
							: "-")
					.AppendFormat(" {0:F2}", slice.DriverInputPitch)
					.AppendFormat(" {0:F2}", slice.DriverInputYaw)
					.AppendFormat(" {0:F2}", slice.DriverInputThrottle)
					.AppendFormat(" {0:F1}", slice.Velocity)
					.AppendFormat(" {0:F1}", slice.Facing)
					.AppendLine();
			}
			QuantumConsole.Instance.LogAllToConsole(sb.ToString());
		}

		static void ShowPredictionDetails(string predictionReference)
		{
			var (ok, predicted) = PredictionCheck(predictionReference);
			if (!ok)
			{
				return;
			}
			var sb = new StringBuilder();
			sb.AppendFormat(
				"Projectile ID: {0}{1}",
				predicted.isLaunchedInTurn ? "LIT" : "C-",
				predicted.projectileLink.combatID).AppendLine();
			sb.AppendFormat("Combat source ID: C-{0}", predicted.combatSourceLink.combatID).AppendLine();
			sb.AppendFormat("Start position: {0:F1}", predicted.startPosition.v).AppendLine();
			if (predicted.hasTargetEntityLink)
			{
				sb.AppendFormat("Target entity ID: C-{0}", predicted.targetEntityLink.combatID);
			}
			else
			{
				sb.Append("Target entity: <none>");
			}
			sb.AppendLine();
			if (predicted.hasTargetPosition)
			{
				sb.AppendFormat("Target position: {0:F1}", predicted.targetPosition.v);
			}
			else
			{
				sb.Append("Target position: <none>");
			}
			sb.AppendLine();
			if (predicted.hasFuseProximityDistance)
			{
				sb.AppendFormat("Fuse proximity: {0}m", predicted.fuseProximityDistance.f).AppendLine();
			}
			sb.AppendFormat("Launched in turn: {0}", predicted.isLaunchedInTurn).AppendLine();
			if (predicted.isLaunchedInTurn)
			{
				sb.AppendFormat("Action ID: {0}", predicted.actionLink.actionID).AppendLine();
				sb.AppendFormat("Action start time: {0:F3}s", predicted.actionStartTime.f).AppendLine();
				sb.AppendFormat("Activation count: {0}", predicted.activationCount.i).AppendLine();
				sb.AppendFormat("Round seqno: {0}", predicted.roundSequenceNumber.i).AppendLine();
				sb.AppendFormat("Placed: {0}", predicted.isPlaced).AppendLine();
				if (!predicted.isPlaced)
				{
					QuantumConsole.Instance.LogToConsole(sb.ToString());
					return;
				}
			}
			if (!predicted.hasMotionTimeSlices)
			{
				QuantumConsole.Instance.LogToConsole(sb.ToString());
				QuantumConsole.Instance.LogToConsole("No motion time slices");
				return;
			}

			sb.AppendFormat("Motion ready: {0}", predicted.isPredictionMotionReady).AppendLine();
			var timeSlices = predicted.motionTimeSlices.a;
			var startIndex = -1;
			for (var i = 0; i < timeSlices.Length; i += 1)
			{
				if (timeSlices[i].Status != TimeSliceStatus.Uninitialized && timeSlices[i].Status != TimeSliceStatus.Recalculate)
				{
					startIndex = i;
					break;
				}
			}
			if (startIndex == -1)
			{
				QuantumConsole.Instance.LogToConsole(sb.ToString());
				QuantumConsole.Instance.LogToConsole("Motion has not been calculated");
				return;
			}

			var startTime = predicted.isLaunchedInTurn
				? predicted.roundStartTime.f
				: Contexts.sharedInstance.combat.simulationTime.f;
			var index = Mathf.FloorToInt(
					(startTime - Contexts.sharedInstance.combat.simulationTime.f)
						* ECS.Contexts.sharedInstance.ekTime.slicesPerSecond.i);
			sb.AppendFormat("Round start time: {0}/{1:F3}s", index, startTime).AppendLine();
			sb.AppendFormat(
				"Motion start index: {0}/{1:F3}s",
				startIndex,
				startIndex * ECS.Contexts.sharedInstance.ekTime.timeStep.f).AppendLine();

			var activeStart = -1;
			var activeEnd = -1;
			var activeCount = 0;
			var destroyedAt = timeSlices.Length;
			var reason = DestructionReason.Unknown;
			for (var i = 0; i < timeSlices.Length; i += 1)
			{
				var ts = timeSlices[i];
				if (ts.Status != TimeSliceStatus.Destroyed && i > destroyedAt)
				{
					sb.AppendFormat("  motion revives at {0} ({1}) after being destroyed at {2} ({3})", i, ts.Status, destroyedAt, reason).AppendLine();
					break;
				}
				if (ts.Status == TimeSliceStatus.Destroyed && i < destroyedAt)
				{
					destroyedAt = i;
					if (ts.DestroyedBy == DestructionReason.Unknown)
					{
						sb.AppendFormat("  motion destroyed at {0} but reason is not set", i).AppendLine();
					}
					else
					{
						reason = ts.DestroyedBy;
					}
				}
				if (ts.Status == TimeSliceStatus.Active)
				{
					if (activeStart == -1)
					{
						activeStart = i;
					}
					activeEnd = i;
					activeCount += 1;
				}
			}
			sb.AppendFormat("Active range: {0}/{1} ({2})", activeStart, activeEnd, activeCount).AppendLine();

			sb.AppendFormat("Asset key: {0}", predicted.hasAssetKey ? predicted.assetKey.key : "<none>").AppendLine();
			if (predicted.hasAsset)
			{
				var instance = predicted.asset.instance;
				if (instance != null)
				{
					sb.AppendFormat(
						"Asset: {0} ({1})",
						instance.name,
						instance.gameObject.activeSelf ? "active" : "inactive");
				}
				else
				{
					sb.AppendFormat("Asset: <null> ({0:F3}s)", Contexts.sharedInstance.combat.predictionTime.f);
				}
			}
			else
			{
				sb.AppendFormat("Asset: <none> ({0:F3}s)", Contexts.sharedInstance.combat.predictionTime.f);
			}
			sb.AppendLine();

			if (predicted.isLaunchedInTurn)
			{
				var combatSource = IDUtility.GetCombatEntity(predicted.combatSourceLink.combatID);
				if (combatSource == null)
				{
					QuantumConsole.Instance.LogAllToConsole(sb.ToString());
					QuantumConsole.Instance.LogToConsole($"Null combat source ({predicted.combatSourceLink.combatID})");
					return;
				}

				PathUtility.GetProjectedTransformAtTime(combatSource, startTime, out var unitPosition, out var _);
				unitPosition += combatSource.hasLocalCenterPoint
					? combatSource.localCenterPoint.v
					: DataShortcuts.anim.firingCenterOffset;
				var firingPoint = timeSlices[startIndex].Position;
				var distance = Vector3.Distance(unitPosition, firingPoint);
				sb.AppendFormat("Unit position: {0:F1}", unitPosition).AppendLine();
				sb.AppendFormat("Firing point: {0:F1}", firingPoint).AppendLine();
				sb.AppendFormat("Offset: {0:F2}m", distance).AppendLine();

				QuantumConsole.Instance.LogAllToConsole(sb.ToString());
				return;
			}

			var projectile = IDUtility.GetCombatEntity(predicted.projectileLink.combatID);
			if (projectile == null)
			{
				QuantumConsole.Instance.LogAllToConsole(sb.ToString());
				QuantumConsole.Instance.LogToConsole("No linked projectile");
				return;
			}
			var pdist = Vector3.Distance(projectile.position.v, timeSlices[0].Position);
			if (!pdist.RoughlyEqual(0f))
			{
				QuantumConsole.Instance.LogAllToConsole(sb.ToString());
				QuantumConsole.Instance.LogToConsole($"Prediction double is off from projectile position by {pdist}m");
				return;
			}

			QuantumConsole.Instance.LogAllToConsole(sb.ToString());
		}

		static void ToggleExtraMotionData()
		{
			ModLink.Settings.extraMotionData = !ModLink.Settings.extraMotionData;
			QuantumConsole.Instance.LogToConsole("Extra motion data tracking is "
				+ (ModLink.Settings.extraMotionData ? "on" : "off"));
		}

		static void ShowMotionCalculations(string predictionReference)
		{
			var (ok, predicted) = PredictionCheck(predictionReference);
			if (!ok)
			{
				return;
			}
			if (!ModLink.Settings.extraMotionData)
			{
				QuantumConsole.Instance.LogToConsole("Extra motion data is not being tracked");
				return;
			}
			if (!predicted.hasMotionTimeSlices)
			{
				QuantumConsole.Instance.LogToConsole("No motion data");
				return;
			}
			if (!predicted.hasMotionExtraData)
			{
				QuantumConsole.Instance.LogToConsole("No extra motion data");
				return;
			}

			var turnStart = Contexts.sharedInstance.combat.simulationTime.f;
			QuantumConsole.Instance.LogToConsole($"Turn start: {turnStart:F3}s");
			QuantumConsole.Instance.LogToConsole("I: S P V TP PG TH TB D TV CTV CP1 CA CP2 LC PE YE IP IY IT TQ TF");
			var sb = new StringBuilder();
			var timeSlices = predicted.motionTimeSlices.a;
			var extraData = (ModLink.Settings.extraMotionData && predicted.hasMotionExtraData)
				? predicted.motionExtraData.a
				: null;
			for (var i = 0; i < timeSlices.Length; i += 1)
			{
				var ed = extraData != null
					? extraData[i]
					: default;

				var ts = timeSlices[i];
				sb.AppendFormat("{0:000}/{1:F3}s:", i, i * ECS.Contexts.sharedInstance.ekTime.timeStep.f)
					.AppendFormat(" {0}", ts.Status)
					.AppendFormat(" {0:F1}", ts.Position)
					.AppendFormat(" {0:F1}", ts.Velocity)
					.AppendFormat(" {0:F1}", ts.TargetPosition)
					.AppendFormat(" {0:F2}", ts.Progress)
					.AppendFormat(" {0:F1}", ts.TargetHeight)
					.AppendFormat(" {0:F1}", ts.TargetBlend);
				if (ed.IsValid)
				{
					sb.AppendFormat(" {0:F1}", ed.ChaseDirection)
					.AppendFormat(" {0:F1}", ed.TargetVelocity)
					.AppendFormat(" {0:F1}", ed.AdjustedTargetVelocity)
					.AppendFormat(" {0:F1}", ed.ChasePosition1)
					.AppendFormat(" {0:F1}", ed.ChaseAltitude)
					.AppendFormat(" {0:F1}", ed.ChasePosition2)
					.AppendFormat(" {0:F1}", ed.LocalChase)
					.AppendFormat(" {0:F3}", ed.PitchError)
					.AppendFormat(" {0:F3}", ed.YawError);
				}
				else
				{
					sb.Append(string.Join("", System.Linq.Enumerable.Repeat(" -", 9)));
				}
				sb.AppendFormat(" {0:F2}", ts.DriverInputPitch)
					.AppendFormat(" {0:F2}", ts.DriverInputYaw)
					.AppendFormat(" {0:F2}", ts.DriverInputThrottle);
				if (ed.IsValid)
				{
					sb.AppendFormat("\t{0:F2}", ed.Torque)
						.AppendFormat("\t{0:F2}", ed.ThrottleForce);
				}
				else
				{
					sb.Append(" - -");
				}
				sb.AppendLine();
			}
			QuantumConsole.Instance.LogAllToConsole(sb.ToString());
		}

		static void SaveMotionCalculations(string predictionReference)
		{
			var (ok, predicted) = PredictionCheck(predictionReference);
			if (!ok)
			{
				return;
			}
			if (!predicted.hasMotionTimeSlices)
			{
				QuantumConsole.Instance.LogToConsole("No motion data"); 
				return;
			}

			var headers = new string[]
			{
				"index",
				"timestamp",
				"status",
				"position",
				"facing",
				"velocity",
				"target_position",
				"time_to_live",
				"progress",
				"target_height",
				"target_blend",
				"chase_direction",
				"target_velocity",
				"target_velocity_adj",
				"chase_position_1",
				"chase_altitude",
				"chase_position_2",
				"local_chase",
				"pitch_error",
				"yaw_error",
				"pitch_input",
				"yaw_input",
				"throttle_input",
				"angles",
				"torque",
				"throttle_force",
			};
			var sb = new StringBuilder(string.Join("\t", headers));
			var timeSlices = predicted.motionTimeSlices.a;
			var extraData = (ModLink.Settings.extraMotionData && predicted.hasMotionExtraData)
				? predicted.motionExtraData.a
				: null;
			var k = 0;
			sb.AppendLine();
			for (var i = 0; i < timeSlices.Length; i += 1)
			{
				var ed = extraData != null
					? extraData[i]
					: default;

				var ts = timeSlices[i];
				sb.AppendFormat("{0:000}/{1:F3}s", i, i * ECS.Contexts.sharedInstance.ekTime.timeStep.f);
				if (ed.IsValid)
				{
					sb.AppendFormat("\t{0:F3}s", ed.Realtime);
				}
				else
				{
					sb.Append("\t");
				}
				sb.AppendFormat("\t{0}", ts.Status)
					.AppendFormat("\t{0:F1}", ts.Position)
					.AppendFormat("\t{0:F1}", ts.Facing)
					.AppendFormat("\t{0:F1}", ts.Velocity)
					.AppendFormat("\t{0:F1}", ts.TargetPosition)
					.AppendFormat("\t{0:F3}s", ts.TimeToLive)
					.AppendFormat("\t{0:F2}", ts.Progress)
					.AppendFormat("\t{0:F1}", ts.TargetHeight)
					.AppendFormat("\t{0:F1}", ts.TargetBlend);
				if (ed.IsValid)
				{
					sb.AppendFormat("\t{0:F1}", ed.ChaseDirection)
					.AppendFormat("\t{0:F1}", ed.TargetVelocity)
					.AppendFormat("\t{0:F1}", ed.AdjustedTargetVelocity)
					.AppendFormat("\t{0:F1}", ed.ChasePosition1)
					.AppendFormat("\t{0:F1}", ed.ChaseAltitude)
					.AppendFormat("\t{0:F1}", ed.ChasePosition2)
					.AppendFormat("\t{0:F1}", ed.LocalChase)
					.AppendFormat("\t{0:F3}", ed.PitchError)
					.AppendFormat("\t{0:F3}", ed.YawError);
				}
				else
				{
					sb.Append(new string('\t', 9));
				}
				sb.AppendFormat("\t{0:F2}", ts.DriverInputPitch)
					.AppendFormat("\t{0:F2}", ts.DriverInputYaw)
					.AppendFormat("\t{0:F2}", ts.DriverInputThrottle);
				if (ed.IsValid)
				{
					sb.AppendFormat("\t{0:F1}", ed.Angles)
						.AppendFormat("\t{0:F2}", ed.Torque)
						.AppendFormat("\t{0:F2}", ed.ThrottleForce);
				}
				else
				{
					sb.Append(new string('\t', 3));
				}
				sb.AppendLine();
				k += 1;
			}
			var filename = "C:\\Projects\\PhantomBrigade\\Scratch\\motion_details.tsv";
			System.IO.File.WriteAllText(filename, sb.ToString());
			QuantumConsole.Instance.LogToConsole($"Wrote {k}/{extraData.Length} data points to {filename}");
		}
	}
}
