// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;

using Entitas;

using UnityEngine;

namespace EchKode.PBMods.MissilePrediction
{
	sealed class LaunchedInTurnCleanupSystem : ReactiveSystem<CombatEntity>
	{
		private readonly ECS.EkPredictionContext prediction;

		internal LaunchedInTurnCleanupSystem(Contexts contexts, ECS.Contexts ekContexts)
			: base(contexts.combat)
		{
			prediction = ekContexts.ekPrediction;
		}

		protected override ICollector<CombatEntity> GetTrigger(IContext<CombatEntity> context) =>
			context.CreateCollector(CombatMatcher.CurrentTurn.Added());

		protected override bool Filter(CombatEntity entity) => true;

		protected override void Execute(List<CombatEntity> entities)
		{
			foreach (var predicted in prediction.GetEntities())
			{
				if (!predicted.isLaunchedInTurn)
				{
					continue;
				}
				if (!predicted.isPlaced)
				{
					continue;
				}

				var id = predicted.projectileLink.combatID;
				PredictionDoubleFunctions.DestroyPredicted(predicted);

				if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.Link))
				{
					Debug.LogFormat(
						"Mod {0} ({1}) predicted projectile launched in turn destroyed | ID: LIT{2}",
						ModLink.modIndex,
						ModLink.modID,
						id);
				}
			}
		}
	}
}
