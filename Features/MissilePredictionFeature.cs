using System.Collections.Generic;

using PhantomBrigade.Data;
using PhantomBrigade.Input.Components;
using PhantomBrigade.Overworld;

using UnityEngine;

namespace EchKode.PBMods.MissilePrediction
{
	public sealed partial class MissilePredictionFeature : Feature
	{
		private static readonly HashSet<CombatUIModes> excludedModes = new HashSet<CombatUIModes>()
		{
			CombatUIModes.Time_Placement,
			CombatUIModes.Targeting_Units,
			CombatUIModes.Targeting_Locations,
		};

		private readonly CombatContext combat;
		private readonly ECS.EkTimeContext ekTime;
		private readonly PredictionTimeSliceSystem tss;
		private readonly LaunchedInTurnCleanupSystem litcs;
		private readonly ProjectileVisibilitySystem pvs;

		private MissilePredictionFeature(
			Contexts contexts,
			ECS.Contexts ekContexts,
			Options options)
		{
			combat = contexts.combat;
			ekTime = ekContexts.ekTime;
			tss = new PredictionTimeSliceSystem(contexts, ekContexts, options.SlicesPerSecond);
			litcs = new LaunchedInTurnCleanupSystem(contexts, ekContexts);
			pvs = new ProjectileVisibilitySystem(contexts, ekContexts);

			Add(new ActionDisposedSystem(contexts, ekContexts));
			Add(new ActionLinkSystem(contexts, ekContexts, options.TrackFriendlyMissiles));
			Add(new ProjectileLinkSystem(contexts, ekContexts, options.TrackFriendlyMissiles));
			Add(new AIPhaseTrackingSystem(contexts, ekContexts));
			Add(new RoundPlacementSystem(contexts, ekContexts, options.AnimatorDelay, options.TimeTargetThreshold));
			Add(new UnitTrackingSystem(contexts, ekContexts));
			Add(new ActionDragSystem(contexts, ekContexts));
			// XXX Add(new ModeTrackingSystem(contexts, ekContexts));
			Add(new MotionComputeFeature(
				contexts,
				ekContexts,
				new MotionComputeFeature.Options()
				{
					SlicesPerFrame = options.ComputeSlicesPerFrame,
					ChaseDistance = options.ChaseDistance,
					TriggerDistance = options.TriggerDistance,
				}));
			Add(new PredictionMotionSystem(ekContexts));
			Add(new PredictionTearDownSystem(ekContexts));
		}

		public override void Initialize()
		{
			if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.System))
			{
				Debug.LogFormat(
					"Mod {0} ({1}) initializing feature",
					ModLink.modIndex,
					ModLink.modID);
			}
			tss.Initialize();
			base.Initialize();
		}

		public override void Execute()
		{
			if (combat.Simulating)
			{
				return;
			}

			if (!OverworldUtility.IsFeatureUnlocked(EventMemoryFeatureFlag.CombatPrediction, out var memoryValue) || memoryValue == 0f)
			{
				return;
			}

			if (!ekTime.hasSampleCount)
			{
				ekTime.ReplaceSampleCount(combat.turnLength.i * ekTime.slicesPerSecond.i + 1);
			}

			litcs.Execute();
			pvs.Execute();

			if (CombatReplayHelper.IsReplayActive())
			{
				return;
			}

			tss.Execute();

			var input = Contexts.sharedInstance.input;
			if (input.hasCombatUIMode && excludedModes.Contains(input.combatUIMode.e))
			{
				return;
			}

			base.Execute();
		}

		public override void TearDown()
		{
			if (ekTime.hasSampleCount)
			{
				ekTime.RemoveSampleCount();
			}
			base.TearDown();
		}
	}
}
