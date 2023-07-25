// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using PhantomBrigade;

using QFSW.QC;

namespace EchKode.PBMods.MissilePrediction.Diagnostics
{
	static partial class PredictionInspector
	{
		private static bool CombatStateCheck()
		{
			if (IDUtility.IsGameState(GameStates.combat))
			{
				return true;
			}

			QuantumConsole.Instance.LogToConsole("Command only available from combat");
			return false;
		}

		static (bool, CombatEntity) ProjectileCheck(string projectileReference)
		{
			var (ok, id) = ParseProjectileReference(projectileReference);
			if (ok)
			{
				var p = IDUtility.GetCombatEntity(id);
				if (null != p)
				{
					return (true, p);
				}
			}
			QuantumConsole.Instance.LogToConsole("No projectile in combat has identifier: " + projectileReference);
			return (false, null);
		}

		static (bool, ECS.EkPredictionEntity) PredictionCheck(string projectileReference)
		{
			var (ok, id) = ParsePredictionReference(projectileReference);
			if (ok)
			{
				foreach (var p in ECS.Contexts.sharedInstance.ekPrediction.GetEntities())
				{
					if (!p.hasProjectileLink)
					{
						continue;
					}
					if (id == p.projectileLink.combatID)
					{
						return (true, p);
					}
				}
			}

			QuantumConsole.Instance.LogToConsole("No prediction double has projectile link: " + projectileReference);
			return (false, null);
		}

		static (bool, int) ParseProjectileReference(string projectileReference)
		{
			if (string.IsNullOrEmpty(projectileReference))
			{
				QuantumConsole.Instance.LogToConsole("Command requires a projectile ID argument");
				return (false, IDUtility.invalidID);
			}

			projectileReference = projectileReference.ToUpperInvariant();
			if (!projectileReference.StartsWith("C-"))
			{
				QuantumConsole.Instance.LogToConsole("Projectile ID should begin with C-");
				return (false, IDUtility.invalidID);
			}

			var prefix = projectileReference.Substring(0, 2);
			if (!int.TryParse(projectileReference.Substring(2).TrimEnd(), out var id))
			{
				QuantumConsole.Instance.LogToConsole($"Invalid projectile ID: the part after {prefix} should be an integer");
				return (false, IDUtility.invalidID);
			}

			return (true, id);
		}

		static (bool, int) ParsePredictionReference(string projectileReference)
		{
			if (string.IsNullOrEmpty(projectileReference))
			{
				QuantumConsole.Instance.LogToConsole("Command requires a projectile ID argument");
				return (false, IDUtility.invalidID);
			}

			projectileReference = projectileReference.ToUpperInvariant();
			if (!projectileReference.StartsWith("C-") && !projectileReference.StartsWith("LIT-"))
			{
				QuantumConsole.Instance.LogToConsole("Projectile link should begin with C- or LIT-");
				return (false, IDUtility.invalidID);
			}

			var pos = projectileReference.IndexOf("-");
			var prefix = projectileReference.Substring(0, pos);
			if (!int.TryParse(projectileReference.Substring(pos + 1).TrimEnd(), out var id))
			{
				QuantumConsole.Instance.LogToConsole($"Invalid projectile link: the part after {prefix} should be an integer");
				return (false, IDUtility.invalidID);
			}
			
			if (prefix == "LIT")
			{
				id = -id;
			}

			return (true, id);
		}
	}
}
