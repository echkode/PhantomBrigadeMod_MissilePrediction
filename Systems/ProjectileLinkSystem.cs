using System.Collections.Generic;

using Entitas;

using PhantomBrigade;
using PhantomBrigade.Data;

using UnityEngine;

namespace EchKode.PBMods.MissilePrediction
{
	sealed class ProjectileLinkSystem : ReactiveSystem<CombatEntity>, ITearDownSystem
	{
		private readonly bool trackFriendlyMissiles;
		private readonly CombatContext combat;
		private readonly ECS.EkTimeContext ekTime;
		private readonly ECS.EkPredictionContext prediction;
		private readonly Dictionary<int, ECS.EkPredictionEntity> predictedProjectiles;

		internal ProjectileLinkSystem(Contexts contexts, ECS.Contexts ekContexts, bool trackFriendlyMissiles)
			: base(contexts.combat)
		{
			this.trackFriendlyMissiles = trackFriendlyMissiles;
			combat = contexts.combat;
			ekTime = ekContexts.ekTime;
			prediction = ekContexts.ekPrediction;
			predictedProjectiles = new Dictionary<int, ECS.EkPredictionEntity>();
		}

		protected override bool Filter(CombatEntity entity) => !entity.Simulating;

		protected override ICollector<CombatEntity> GetTrigger(IContext<CombatEntity> context) =>
			context.CreateCollector(CombatMatcher.Simulating.Removed());

		protected override void Execute(List<CombatEntity> entities)
		{
			foreach (var entity in prediction.GetEntities())
			{
				if (entity.isLaunchedInTurn)
				{
					continue;
				}
				if (!entity.hasProjectileLink)
				{
					continue;
				}

				var projectile = IDUtility.GetCombatEntity(entity.projectileLink.combatID);
				if (projectile == null)
				{
					PredictionDoubleFunctions.DestroyPredicted(entity);
					continue;
				}

				if (projectile.isDestroyed)
				{
					PredictionDoubleFunctions.DestroyPredicted(entity);
				}
			}

			foreach (var entity in combat.GetEntities())
			{
				if (!entity.hasDataLinkSubsystemProjectile)
				{
					continue;
				}
				if (entity.dataLinkSubsystemProjectile?.data?.guidanceData == null)
				{
					continue;
				}
				if (!entity.hasSourceEntity)
				{
					if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Link))
					{
						Debug.LogFormat(
							"Mod {0} ({1}) projectile not tracked -- no source entity | projectile: C-{2}",
							ModLink.modIndex,
							ModLink.modID,
							entity.id.id);
					}
					continue;
				}

				var combatSource = IDUtility.GetCombatEntity(entity.sourceEntity.combatID);
				if (combatSource == null)
				{
					if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Link))
					{
						Debug.LogFormat(
							"Mod {0} ({1}) projectile not tracked -- no source unit | projectile: C-{2} | unit ID: C-{3}",
							ModLink.modIndex,
							ModLink.modID,
							entity.id.id,
							entity.sourceEntity.combatID);
					}
					continue;
				}
				var sourceUnit = IDUtility.GetLinkedPersistentEntity(combatSource);
				if (sourceUnit == null)
				{
					if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Link))
					{
						Debug.LogFormat(
							"Mod {0} ({1}) projectile not tracked -- no persistent entity for source | projectile: C-{2} | unit ID: C-{3}",
							ModLink.modIndex,
							ModLink.modID,
							entity.id.id,
							entity.sourceEntity.combatID);
					}
					continue;
				}
				if (CombatUIUtility.IsUnitFriendly(sourceUnit) && !trackFriendlyMissiles)
				{
					if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Link))
					{
						Debug.LogFormat(
							"Mod {0} ({1}) prediction skipped -- not tracking friendly projectiles | projectile: C-{2}",
							ModLink.modIndex,
							ModLink.modID,
							entity.id.id);
					}
					continue;
				}

				var viable = !entity.isDestroyed && !entity.hasProjectileDestructionPosition;
				var found = predictedProjectiles.TryGetValue(entity.id.id, out var predicted);

				if (found)
				{
					if (viable)
					{
						UpdatePredicted(entity, predicted);
						entity.authoritativeRigidbody.rb.Sleep();
						ReturnAttachedInstance(entity);
					}
					else
					{
						predictedProjectiles.Remove(entity.id.id);
						PredictionDoubleFunctions.DestroyPredicted(predicted);
					}
					continue;
				}

				if (!viable)
				{
					if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Link))
					{
						Debug.LogFormat(
							"Mod {0} ({1}) prediction skipped -- projectile not viable | projectile: C-{2}",
							ModLink.modIndex,
							ModLink.modID,
							entity.id.id);
					}
					continue;
				}

				CreatePredictedProjectile(entity);
				entity.authoritativeRigidbody.rb.Sleep();
				ReturnAttachedInstance(entity);
			}
		}

		private void UpdatePredicted(CombatEntity projectile, ECS.EkPredictionEntity predicted)
		{
			ResetTimeSlices(predicted);
			ResetRigidbody(projectile, predicted);
			UpdateFlightTracking(projectile, predicted);
			UpdateMotion(projectile, predicted, ekTime.currentTimeSlice.i);
			if (projectile.hasAssetLink)
			{
				predicted.ReplaceColors(projectile.assetLink.instance.hueOffsetLast, projectile.assetLink.instance.colorOverrideLast);
			}

			if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Link))
			{
				Debug.LogFormat(
					"Mod {0} ({1}) reset prediction on update | id: C-{2} | targeted unit: {3} | targeted position: {4:F1}",
					ModLink.modIndex,
					ModLink.modID,
					projectile.id.id,
					predicted.hasTargetEntityLink ? "C-" + predicted.targetEntityLink.combatID : "<none>",
					predicted.hasTargetPosition
						? predicted.targetPosition.v
						: predicted.hasTargetEntityLink
							? IDUtility.GetCombatEntity(predicted.targetEntityLink.combatID) != null
								? IDUtility.GetCombatEntity(predicted.targetEntityLink.combatID).position.v
								: Vector3.zero
							: Vector3.zero);
			}
		}

		private void ResetTimeSlices(ECS.EkPredictionEntity predicted)
		{
			var timeSlices = predicted.motionTimeSlices.a;
			for (var i = 0; i < timeSlices.Length; i += 1)
			{
				var ts = timeSlices[i];
				ts.Status = TimeSliceStatus.Uninitialized;
				timeSlices[i] = ts;
			}
			predicted.isPredictionMotionReady = false;

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

		private static void ResetRigidbody(CombatEntity projectile, ECS.EkPredictionEntity predicted)
		{
			var rb = projectile.authoritativeRigidbody.rb;
			var prb = predicted.authoritativeRigidbody.rb;
			var t = rb.transform;
			var pt = prb.transform;
			pt.position = t.position;
			pt.rotation = t.rotation;
			prb.velocity = rb.velocity;
		}

		private static void UpdateFlightTracking(CombatEntity projectile, ECS.EkPredictionEntity predicted)
		{
			if (projectile.hasTimeToLive)
			{
				predicted.ReplaceTimeToLive(projectile.timeToLive.f);
			}
			if (projectile.hasFlightInfo)
			{
				var flightInfo = projectile.flightInfo;
				predicted.ReplaceFlightInfo(
					flightInfo.time,
					flightInfo.distance,
					flightInfo.origin,
					flightInfo.positionLast);
			}
		}

		private static void UpdateMotion(CombatEntity projectile, ECS.EkPredictionEntity predicted, int timeSlice)
		{
			if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Link)
				&& ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Trace))
			{
				Debug.LogFormat(
					"Mod {0} ({1}) update motion on link | id: C-{2} | start index: {3}",
					ModLink.modIndex,
					ModLink.modID,
					projectile.id.id,
					timeSlice);
			}

			var ts = predicted.motionTimeSlices.a[timeSlice];
			ts.Status = TimeSliceStatus.Active;
			ts.Position = projectile.position.v;
			ts.Facing = projectile.facing.v;
			ts.Rotation = projectile.rotation.q;
			ts.Velocity = projectile.authoritativeRigidbody.rb.velocity;
			ts.DriverInputYaw = projectile.projectileRigidbodyDriverInput.yaw;
			ts.DriverInputPitch = projectile.projectileRigidbodyDriverInput.pitch;
			ts.DriverInputThrottle = projectile.projectileRigidbodyDriverInput.throttle;
			ts.GuidanceSuspensionTime = projectile.hasProjectileGuidanceSuspended ? projectile.projectileGuidanceSuspended.timeLeft : 0f;
			ts.Progress = projectile.projectileGuidanceProgress.f;
			ts.TargetBlend = projectile.projectileGuidanceTargetBlend.f;
			ts.TargetHeight = projectile.projectileTargetHeight.f;
			ts.TargetPosition = projectile.projectileGuidanceTargetPosition.v;
			ts.TimeToLive = projectile.timeToLive.f;
			ts.IsTrackingActive = true;
			ts.IsGuided = true;
			predicted.motionTimeSlices.a[timeSlice] = ts;
			predicted.ReplaceSliceIndex(timeSlice);
		}

		private void CreatePredictedProjectile(CombatEntity projectile)
		{
			var predicted = prediction.CreateEntity();
			predicted.AddCombatSourceLink(projectile.sourceEntity.combatID);
			predicted.AddProjectileLink(projectile.id.id);
			predicted.AddGuidanceData(projectile.dataLinkSubsystemProjectile.data.guidanceData);
			predicted.AddGuidancePID(projectile.projectileGuidancePID.steeringPID, projectile.projectileGuidancePID.pitchPID);
			predicted.AddStartPosition(projectile.projectileStartPosition.v);
			predicted.AddTargetOffset(projectile.projectileGuidanceTargetOffset.v);
			if (projectile.hasProjectileTargetEntity)
			{
				predicted.AddTargetEntityLink(projectile.projectileTargetEntity.combatID);
			}
			if (projectile.hasProjectileTargetPosition)
			{
				predicted.AddTargetPosition(projectile.projectileTargetPosition.v);
			}
			AddAsset(projectile, predicted);
			AddProjectileProperties(projectile, predicted);
			AddLifetime(projectile, predicted);
			CreateRigidbody(projectile, predicted);
			InitializeMotionSlices(predicted);
			UpdateMotion(projectile, predicted, ekTime.currentTimeSlice.i);
			predictedProjectiles.Add(projectile.id.id, predicted);

			if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Link))
			{
				Debug.LogFormat(
					"Mod {0} ({1}) linked prediction double | id: C-{2} | targeted unit: {3} | targeted position: {4:F1}",
					ModLink.modIndex,
					ModLink.modID,
					projectile.id.id,
					predicted.hasTargetEntityLink ? ("C-" + predicted.targetEntityLink.combatID) : "<none>",
					predicted.hasTargetPosition
						? predicted.targetPosition.v
						: predicted.hasTargetEntityLink
							? IDUtility.GetCombatEntity(predicted.targetEntityLink.combatID) != null
								? IDUtility.GetCombatEntity(predicted.targetEntityLink.combatID).position.v
								: Vector3.zero
							: Vector3.zero);
			}
		}

		private void InitializeMotionSlices(ECS.EkPredictionEntity predicted)
		{
			var ary = new MotionTimeSlice[ekTime.sampleCount.i];
			predicted.AddMotionTimeSlices(ary);
			if (ModLink.Settings.extraMotionData)
			{
				predicted.AddMotionExtraData(new MotionExtraInfo[ekTime.sampleCount.i]);
			}
		}

		private static void AddAsset(CombatEntity projectile, ECS.EkPredictionEntity predicted)
		{
			if (projectile.hasAssetKey)
			{
				predicted.AddAssetKey(projectile.assetKey.key);
			}
			if (projectile.hasAssetLink)
			{
				predicted.AddColors(projectile.assetLink.instance.hueOffsetLast, projectile.assetLink.instance.colorOverrideLast);
			}
			if (projectile.hasScale)
			{
				predicted.AddScale(projectile.scale.v);
			}
		}

		private static void AddProjectileProperties(CombatEntity projectile, ECS.EkPredictionEntity predicted)
		{
			if (projectile.dataLinkSubsystemProjectile.data.fuseProximity == null)
			{
				return;
			}
			predicted.AddFuseProximityDistance(projectile.dataLinkSubsystemProjectile.data.fuseProximity.distance);
		}

		private static void AddLifetime(CombatEntity projectile, ECS.EkPredictionEntity predicted)
		{
			var lifetime = DataHelperStats.GetCachedStatForPart(UnitStats.weaponProjectileLifetime, IDUtility.GetEquipmentEntity(projectile.parentPart.equipmentID));
			predicted.AddProjectileLifetime(lifetime);
			predicted.AddTimeToLive(lifetime);
			UpdateFlightTracking(projectile, predicted);
		}

		private static void CreateRigidbody(CombatEntity projectile, ECS.EkPredictionEntity predicted)
		{
			var go = Object.Instantiate(projectile.authoritativeRigidbody.rb.gameObject);
			var rb = go.GetComponent<Rigidbody>();
			rb.interpolation = RigidbodyInterpolation.Interpolate;
			rb.velocity = projectile.authoritativeRigidbody.rb.velocity;
			predicted.AddAuthoritativeRigidbody(rb);
		}

		internal static void ReturnAttachedInstance(CombatEntity entity)
		{
			if (!entity.hasAssetLink)
			{
				return;
			}

			var entry = DataMultiLinkerAssetPools.GetEntry(entity.assetLink.key);
			if (entry == null)
			{
				return;
			}

			var instance = entity.assetLink.instance;
			if (instance == null)
			{
				return;
			}

			entry.ReturnInstance(instance, reinsertAsNext: false, forceParentDeactivation: true);
			entity.RemoveAssetLink();
		}

		public void TearDown()
		{
			predictedProjectiles.Clear();
		}
	}
}
