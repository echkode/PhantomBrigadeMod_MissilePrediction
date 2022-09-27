using System.Collections.Generic;

using Entitas;

using PhantomBrigade;
using PhantomBrigade.Data;

using UnityEngine;

namespace EchKode.PBMods.MissilePrediction
{
	sealed class ProjectileVisibilitySystem : ReactiveSystem<CombatEntity>
	{
		private enum VisibilityMode
		{
			Simulation,
			Prediction,
			Replay,
		}

		private readonly ECS.EkPredictionContext prediction;
		private readonly ECS.EkTimeContext ekTime;
		private bool hiddenForReplay;

		internal ProjectileVisibilitySystem(Contexts contexts, ECS.Contexts ekContexts)
			: base(contexts.combat)
		{
			prediction = ekContexts.ekPrediction;
			ekTime = ekContexts.ekTime;
		}

		public new void Execute()
		{
			if (CombatReplayHelper.IsReplayActive())
			{
				if (hiddenForReplay)
				{
					return;
				}

				ChangeVisibility(VisibilityMode.Replay);
				hiddenForReplay = true;
				return;
			}

			if (hiddenForReplay)
			{
				ChangeVisibility(VisibilityMode.Prediction);
				hiddenForReplay = false;
				return;
			}

			base.Execute();
		}

		protected override ICollector<CombatEntity> GetTrigger(IContext<CombatEntity> context) =>
			context.CreateCollector(CombatMatcher.CurrentTurn.Added());

		protected override bool Filter(CombatEntity entity) => true;

		protected override void Execute(List<CombatEntity> entities)
		{
			if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Asset)
				&& ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Trace))
			{
				Debug.LogFormat(
				"Mod {0} ({1}) ProjectileVisibilitySystem -- triggered on turn change | turn: {2}",
				ModLink.modIndex,
				ModLink.modID,
				Contexts.sharedInstance.combat.currentTurn.i);
			}
			ChangeVisibility(VisibilityMode.Simulation);
		}

		private void ChangeVisibility(VisibilityMode mode)
		{
			foreach (var predicted in prediction.GetEntities())
			{
				if (!predicted.hasProjectileLink)
				{
					continue;
				}

				if (mode == VisibilityMode.Prediction)
				{
					ekTime.ReplaceCurrentTimeSlice(ekTime.currentTimeSlice.i);
				}
				else
				{
					PredictionDoubleFunctions.ReleaseAsset(predicted);
					if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Asset)
						&& ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Trace))
					{
						Debug.LogFormat(
							"Mod {0} ({1}) hiding prediction double | turn: {4} | ID: {2}{3} | viz mode: {5}",
							ModLink.modIndex,
							ModLink.modID,
							predicted.projectileLink.combatID < 0 ? "LIT" : "C-",
							predicted.projectileLink.combatID,
							Contexts.sharedInstance.combat.currentTurn.i,
							mode);
					}
				}

				if (mode == VisibilityMode.Replay)
				{
					continue;
				}

				if (predicted.isLaunchedInTurn)
				{
					continue;
				}

				var projectile = IDUtility.GetCombatEntity(predicted.projectileLink.combatID);
				if (projectile == null)
				{
					continue;
				}
				if (projectile.isDestroyed)
				{
					continue;
				}
				if (projectile.hasProjectileDestructionPosition)
				{
					continue;
				}

				if (mode == VisibilityMode.Simulation)
				{
					projectile.authoritativeRigidbody.rb.WakeUp();

					if (!predicted.hasAssetKey)
					{
						continue;
					}
					if (projectile.hasAssetLink && projectile.assetLink.instance != null)
					{
						continue;
					}

					var playFromStart = !predicted.hasProjectileLifetime || !predicted.hasTimeToLive;
					AssetPoolUtility.AttachInstance(predicted.assetKey.key, projectile, playFromStart);
					if (predicted.hasColors)
					{
						projectile.assetLink.instance.UpdateColors(predicted.colors.hueOffset, predicted.colors.colorOverride);
					}
					if (!playFromStart)
					{
						var timeOffset = predicted.projectileLifetime.f - predicted.timeToLive.f;
						projectile.assetLink.instance.PlayPastReplay(timeOffset);
					}

					if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Asset))
					{
						Debug.LogFormat(
							"Mod {0} ({1}) projectile attached asset | turn: {3} | ID: C-{2} | key: {4}",
							ModLink.modIndex,
							ModLink.modID,
							predicted.projectileLink.combatID,
							Contexts.sharedInstance.combat.currentTurn.i,
							predicted.assetKey.key);
					}
					continue;
				}

				if (!projectile.hasAssetLink)
				{
					continue;
				}
				if (projectile.assetLink.instance == null)
				{
					continue;
				}
				ProjectileLinkSystem.ReturnAttachedInstance(projectile);

				if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Asset))
				{
					Debug.LogFormat(
						"Mod {0} ({1}) projectile removed asset | turn: {3} | ID: C-{2} | key: {4}",
						ModLink.modIndex,
						ModLink.modID,
						predicted.projectileLink.combatID,
						Contexts.sharedInstance.combat.currentTurn.i,
						projectile.assetKey.key);
				}
			}
		}
	}
}
