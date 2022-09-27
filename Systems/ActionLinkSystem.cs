using System.Collections.Generic;

using Entitas;

using PhantomBrigade;
using PhantomBrigade.AI.Components;
using PhantomBrigade.Data;

using UnityEngine;

namespace EchKode.PBMods.MissilePrediction
{
	sealed class ActionLinkSystem : ReactiveSystem<ActionEntity>, ITearDownSystem
	{
		private readonly bool trackFriendlyMissiles;
		private readonly CombatContext combat;
		private readonly ECS.EkPredictionContext ekPrediction;
		private readonly ECS.EkTimeContext ekTime;
		private readonly HashSet<int> linkedActions;
		private readonly List<RoundInfo> roundCollector;
		private int nextID;

		internal ActionLinkSystem(Contexts contexts, ECS.Contexts ekContexts, bool trackFriendlyMissiles)
			: base(contexts.action)
		{
			this.trackFriendlyMissiles = trackFriendlyMissiles;
			combat = contexts.combat;
			ekPrediction = ekContexts.ekPrediction;
			ekTime = ekContexts.ekTime;
			linkedActions = new HashSet<int>();
			roundCollector = new List<RoundInfo>();
			nextID = IDUtility.invalidID - 1;
		}

		protected override bool Filter(ActionEntity entity) => !entity.isDisposed && !entity.isDestroyed;

		protected override ICollector<ActionEntity> GetTrigger(IContext<ActionEntity> context) =>
			context.CreateCollector(ActionMatcher.AllOf(ActionMatcher.ActionOwner, ActionMatcher.Duration, ActionMatcher.StartTime, ActionMatcher.DataLinkActionEquipment, ActionMatcher.ActiveEquipmentPart).NoneOf(ActionMatcher.CompletedAction, ActionMatcher.MovementMeleeAttacker, ActionMatcher.MovementDash));

		protected override void Execute(List<ActionEntity> entities)
		{
			var turnStartTime = combat.simulationTime.f;
			var targetedActionBuffer = DataShortcuts.anim.targetedActionBuffer;

			if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Link))
			{
				Debug.LogFormat(
					"Mod {0} ({1}) ActionLinkSystem triggered | turn: {2}",
					ModLink.modIndex,
					ModLink.modID,
					combat.currentTurn.i);
			}

			linkedActions.Clear();
			foreach (var entity in ekPrediction.GetEntities())
			{
				if (!entity.hasActionLink)
				{
					continue;
				}
				linkedActions.Add(entity.actionLink.actionID);
			}

			foreach (var action in entities)
			{
				if (!DataHelperAction.IsValid(action))
				{
					if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Link)
						&& ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Trace))
					{
						Debug.LogFormat(
							"Mod {0} ({1}) invalid action | action ID: {2}",
							ModLink.modIndex,
							ModLink.modID,
							action.id.id);
					}
					continue;
				}

				if (linkedActions.Contains(action.id.id))
				{
					if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Link))
					{
						Debug.LogFormat(
							"Mod {0} ({1}) action already linked to prediction | action ID: {2}",
							ModLink.modIndex,
							ModLink.modID,
							action.id.id);
					}
					continue;
				}

				var part = IDUtility.GetEquipmentEntity(action.activeEquipmentPart.equipmentID);
				if (part == null)
				{
					if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Link))
					{
						Debug.LogFormat(
							"Mod {0} ({1}) active equipment part is null | action ID: {2}",
							ModLink.modIndex,
							ModLink.modID,
							action.id.id);
					}
					continue;
				}
				if (!part.hasPrimaryActivationSubsystem)
				{
					if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Link))
					{
						Debug.LogFormat(
							"Mod {0} ({1}) active equipment part does not have a primary activation subsystem | action ID: {2} | part ID: {3} | part key: {4}",
							ModLink.modIndex,
							ModLink.modID,
							action.id.id,
							part.id.id,
							part.dataKeyPartPreset.s);
					}
					continue;
				}

				var primaryActivationSubsystem = IDUtility.GetEquipmentEntity(part.primaryActivationSubsystem.equipmentID);
				var subsystem = primaryActivationSubsystem?.dataLinkSubsystem?.data;
				if (subsystem == null)
				{
					if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Link))
					{
						Debug.LogFormat(
							"Mod {0} ({1}) active equipment part missing link to primary activation subsystem | action ID: {2} | part ID: {3} | part key: {4}",
							ModLink.modIndex,
							ModLink.modID,
							action.id.id,
							part.id.id,
							part.dataKeyPartPreset.s);
					}
					continue;
				}
				var activationProcessed = subsystem.activationProcessed;
				if (activationProcessed == null)
				{
					if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Link))
					{
						Debug.LogFormat(
							"Mod {0} ({1}) primary activation subsystem doesn't have an activation field | action ID: {2} | part ID: {3} | part key: {4} | subsystem: {5}",
							ModLink.modIndex,
							ModLink.modID,
							action.id.id,
							part.id.id,
							part.dataKeyPartPreset.s,
							primaryActivationSubsystem.dataKeySubsystem.s);
					}
					continue;
				}
				var projectileProcessed = subsystem.projectileProcessed;
				if (projectileProcessed == null)
				{
					if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Link))
					{
						Debug.LogFormat(
							"Mod {0} ({1}) primary activation subsystem doesn't have a projectile field | action ID: {2} | part ID: {3} | part key: {4} | subsystem: {5}",
							ModLink.modIndex,
							ModLink.modID,
							action.id.id,
							part.id.id,
							part.dataKeyPartPreset.s,
							primaryActivationSubsystem.dataKeySubsystem.s);
					}
					continue;
				}
				var guidanceData = projectileProcessed.guidanceData;
				if (guidanceData == null)
				{
					if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Link))
					{
						Debug.LogFormat(
							"Mod {0} ({1}) primary activation subsystem doesn't have a guidance data field | action ID: {2} | part ID: {3} | part key: {4} | subsystem: {5}",
							ModLink.modIndex,
							ModLink.modID,
							action.id.id,
							part.id.id,
							part.dataKeyPartPreset.s,
							primaryActivationSubsystem.dataKeySubsystem.s);
					}
					continue;
				}

				var rounds = Mathf.RoundToInt(DataHelperStats.GetCachedStatForPart(UnitStats.activationCount, part));
				if (rounds == 0)
				{
					if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Link))
					{
						Debug.LogFormat(
							"Mod {0} ({1}) missile not tracked -- act_count field is zero | action ID: {2} | part ID: {3} | part key: {4}",
							ModLink.modIndex,
							ModLink.modID,
							action.id.id,
							part.id.id,
							part.dataKeyPartPreset.s);
					}
					continue;
				}

				var combatSource = IDUtility.GetCombatEntity(action.actionOwner.combatID);
				if (combatSource == null)
				{
					if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Link))
					{
						Debug.LogFormat(
							"Mod {0} ({1}) missile not tracked -- no action owner | action ID: {2}",
							ModLink.modIndex,
							ModLink.modID,
							action.id.id);
					}
					continue;
				}
				var sourceUnit = IDUtility.GetLinkedPersistentEntity(combatSource);
				if (sourceUnit == null)
				{
					if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Link))
					{
						Debug.LogFormat(
							"Mod {0} ({1}) missile not tracked -- no persistent entity for combat unit | action ID: {2} | unit ID: C-{3}",
							ModLink.modIndex,
							ModLink.modID,
							action.id.id,
							combatSource.id.id);
					}
					continue;
				}

				if (CombatUIUtility.IsUnitFriendly(sourceUnit) && !trackFriendlyMissiles)
				{
					if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Link))
					{
						Debug.LogFormat(
							"Mod {0} ({1}) missile not tracked -- fired by friendly unit | action ID: {2} | unit ID: P-{3}/C-{4}",
							ModLink.modIndex,
							ModLink.modID,
							action.id.id,
							sourceUnit.id.id,
							combatSource.id.id);
					}
					continue;
				}

				if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Link))
				{
					Debug.LogFormat(
						"Mod {0} ({1}) missile attack action detected | source unit: P-{2}/C-{3} ({4}) | action ID: {5} | subsystem: {6}",
						ModLink.modIndex,
						ModLink.modID,
						sourceUnit.id.id,
						combatSource.id.id,
						CombatUIUtility.IsUnitFriendly(sourceUnit) ? "friendly" : "enemy",
						action.id.id,
						primaryActivationSubsystem.dataKeySubsystem.s);
				}

				CreateRounds(
					turnStartTime,
					targetedActionBuffer,
					action,
					combatSource,
					part,
					primaryActivationSubsystem,
					activationProcessed,
					projectileProcessed,
					guidanceData,
					rounds);
			}
		}

		private void CreateRounds(
			float turnStartTime,
			float targetedActionBuffer,
			ActionEntity action,
			CombatEntity combatSource,
			EquipmentEntity part,
			EquipmentEntity subsystem,
			DataBlockSubsystemActivation_V2 activationProcessed,
			DataBlockSubsystemProjectile_V2 projectileProcessed,
			DataBlockGuidanceData guidanceData,
			int rounds)
		{

			var startTime = action.startTime.f + targetedActionBuffer;
			var duration = action.duration.f - targetedActionBuffer * 2f;
			var endTime = startTime + duration;
			var adjustedTiming = false;
			if (activationProcessed.timing != null)
			{
				var adjustedStartTime = Mathf.Lerp(startTime, endTime, activationProcessed.timing.timeFrom);
				endTime = Mathf.Lerp(startTime, endTime, activationProcessed.timing.timeTo);
				startTime = adjustedStartTime;
				adjustedTiming = !activationProcessed.timing.exponent.RoughlyEqual(1f);
			}

			roundCollector.Clear();
			for (var i = 0; i < rounds; i += 1)
			{
				var roundSpacing = rounds > 1 ? (float)i / (rounds - 1) : 1f;
				if (adjustedTiming)
				{
					roundSpacing = Mathf.Pow(roundSpacing, activationProcessed.timing.exponent);
				}
				var roundStartTime = Mathf.Lerp(startTime, endTime, roundSpacing);
				var startIndex = Mathf.FloorToInt((roundStartTime - turnStartTime) * ekTime.slicesPerSecond.i);
				var round = CreatePredictedProjectile(
					i,
					roundStartTime,
					startIndex,
					action,
					combatSource,
					part,
					subsystem,
					projectileProcessed,
					guidanceData);
				roundCollector.Add(round);
				round.Predicted.AddActivationCount(rounds);
				if (adjustedTiming)
				{
					round.Predicted.AddActivationTiming(activationProcessed.timing);
				}
			}

			if (ekPrediction.isPendingPlaceRound)
			{
				roundCollector.Sort(RoundInfo.Compare);
				foreach (var round in roundCollector)
				{
					ekPrediction.roundQueue.q.Enqueue(round);
				}
				return;
			}

			if (ekPrediction.hasRoundQueue)
			{
				roundCollector.AddRange(ekPrediction.roundQueue.q);
			}
			roundCollector.Sort(RoundInfo.Compare);
			ekPrediction.ReplaceRoundQueue(new Queue<RoundInfo>(roundCollector));

			if (Contexts.sharedInstance.aI.hasAIPlanningRequest
				&& Contexts.sharedInstance.aI.aIPlanningRequest.phase == AIPhase.ActionCreation)
			{
				return;
			}

			ekPrediction.isPendingPlaceRound = true;
			if (!ekTime.hasCurrentTimeTarget)
			{
				ekTime.ReplaceCurrentTimeTarget(combat.predictionTimeTarget.f);
			}
		}

		private RoundInfo CreatePredictedProjectile(
			int round,
			float roundStartTime,
			int startIndex,
			ActionEntity action,
			CombatEntity combatSource,
			EquipmentEntity part,
			EquipmentEntity subsystem,
			DataBlockSubsystemProjectile_V2 projectileProcessed,
			DataBlockGuidanceData guidanceData)
		{
			var predicted = ekPrediction.CreateEntity();
			predicted.isLaunchedInTurn = true;
			predicted.AddActionLink(action.id.id);
			predicted.AddActionStartTime(action.startTime.f);
			predicted.AddRoundSequenceNumber(round);
			predicted.AddCombatSourceLink(action.actionOwner.combatID);
			predicted.AddProjectileLink(nextID);
			nextID -= 1;
			predicted.AddRoundStartTime(roundStartTime);
			AddProjectileProperties(projectileProcessed, predicted);
			AddGuidance(guidanceData, predicted);
			AddLifetime(part, predicted);
			AttachAsset(
				combatSource,
				projectileProcessed,
				predicted);

			if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Link))
			{
				Debug.LogFormat(
					"Mod {0} ({1}) created prediction double | ID: LIT{5} | source unit: C-{2} | action ID: {3} | round: {4}",
					ModLink.modIndex,
					ModLink.modID,
					combatSource.id.id,
					action.id.id,
					round,
					predicted.projectileLink.combatID);
			}

			return new RoundInfo()
			{
				Predicted = predicted,
				Action = action,
				CombatSource = combatSource,
				Part = part,
				Subsystem = subsystem,
				SequenceNumber = round,
				StartTime = roundStartTime,
				StartIndex = startIndex,
			};
		}

		private static void AddProjectileProperties(DataBlockSubsystemProjectile_V2 projectileProcessed, ECS.EkPredictionEntity predicted)
		{
			if (projectileProcessed.fuseProximity == null)
			{
				return;
			}
			predicted.AddFuseProximityDistance(projectileProcessed.fuseProximity.distance);
		}

		private static void AddGuidance(DataBlockGuidanceData guidanceData, ECS.EkPredictionEntity predicted)
		{
			predicted.AddGuidanceData(guidanceData);
			var steeringPID = new SimplePID()
			{
				settings = guidanceData.steeringPID,
			};
			var pitchPID = new SimplePID()
			{
				settings = guidanceData.pitchPID ?? guidanceData.steeringPID,
			};
			predicted.AddGuidancePID(steeringPID, pitchPID);
		}

		private static void AddLifetime(
			EquipmentEntity part,
			ECS.EkPredictionEntity predicted)
		{
			var lifetime = DataHelperStats.GetCachedStatForPart(UnitStats.weaponProjectileLifetime, part);
			predicted.AddProjectileLifetime(lifetime);
			predicted.AddTimeToLive(lifetime);
		}

		private static void AttachAsset(
			CombatEntity combatSource,
			DataBlockSubsystemProjectile_V2 projectileProcessed,
			ECS.EkPredictionEntity predicted)
		{
			var body = projectileProcessed.visual?.body;
			if (body == null)
			{
				if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Link))
				{
					Debug.LogFormat(
						"Mod {0} ({1}) no visual body for subsystem | ID: LIT{3} | source unit: C-{2}",
						ModLink.modIndex,
						ModLink.modID,
						combatSource.id.id,
						predicted.projectileLink.combatID);
				}
				return;
			}

			var friendly = CombatUIUtility.IsUnitFriendly(combatSource);
			var assetKey = friendly || string.IsNullOrEmpty(body.keyEnemy) ? body.key : body.keyEnemy;
			if (string.IsNullOrEmpty(assetKey))
			{
				if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Link))
				{
					Debug.LogFormat(
						"Mod {0} ({1}) no asset on body field | ID: LIT{3} | source unit: C-{2}",
						ModLink.modIndex,
						ModLink.modID,
						combatSource.id.id,
						predicted.projectileLink.combatID);
				}
				return;
			}
			predicted.AddAssetKey(assetKey);

			var scale = body.scale;
			scale = new Vector3(Mathf.Clamp(scale.x, 0.75f, 4f), Mathf.Clamp(scale.y, 0.75f, 4f), Mathf.Clamp(scale.z, 0.5f, 4f));
			predicted.AddScale(scale);

			var colorOverride = friendly || body.colorOverrideEnemy == null
				? body.colorOverride
				: body.colorOverrideEnemy;
			predicted.AddColors(null, colorOverride);
		}

		public void TearDown()
		{
			roundCollector.Clear();
		}
	}
}
