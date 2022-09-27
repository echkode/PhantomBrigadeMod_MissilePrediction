using System.Collections.Generic;

using Entitas;

using PhantomBrigade;
using PhantomBrigade.Combat.Systems;

using UnityEngine;

namespace EchKode.PBMods.MissilePrediction
{
	partial class MissilePredictionFeature
	{
		private sealed class Options
		{
			internal int SlicesPerSecond;
			internal int ComputeSlicesPerFrame;
			internal float ChaseDistance;
			internal float TriggerDistance;
			internal int AnimatorDelay;
			internal float TimeTargetThreshold;
			internal bool TrackFriendlyMissiles;
		}

		public static void Install(GameController gc)
		{
			var gcs = gc.m_stateDict[GameStates.combat];
			var (ok, combatFeature) = FindFeature<CombatSystems>(gcs.m_systems);
			if (!ok)
			{
				Debug.LogWarningFormat(
					"Mod {0} ({1}) Unable to install system -- can't find feature in GameController | state: {0} | feature: {1} | system: {2}",
					ModLink.modIndex,
					ModLink.modID,
					GameStates.combat,
					typeof(CombatSystems).Name,
					typeof(MissilePredictionFeature).FullName);
				return;
			}

			var options = new Options()
			{
				SlicesPerSecond = ModLink.Settings.slicesPerSecond,
				ComputeSlicesPerFrame = ModLink.Settings.computeSlicesPerFrame,
				ChaseDistance = ModLink.Settings.chaseDistance,
				TriggerDistance = ModLink.Settings.triggerDistance,
				AnimatorDelay = ModLink.Settings.animatorDelay,
				TimeTargetThreshold = ModLink.Settings.timeTargetThreshold,
				TrackFriendlyMissiles = ModLink.Settings.trackFriendlyMissiles,
			};
			var installee = new MissilePredictionFeature(
				Contexts.sharedInstance,
				ECS.Contexts.sharedInstance,
				options);
			SystemInstaller.InstallAfter<CombatUISystems>(combatFeature, installee);
		}

		static (bool, Systems) FindFeature<T>(List<Systems> systems)
			where T : Feature
		{
			foreach (var feature in systems)
			{
				if (typeof(T) == feature.GetType())
				{
					return (true, feature);
				}
			}
			return (false, null);
		}
	}
}
