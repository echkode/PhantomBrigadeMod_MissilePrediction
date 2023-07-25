// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;

using Entitas;

using HarmonyLib;

using UnityEngine;

namespace EchKode.PBMods.MissilePrediction
{
	internal static class SystemInstaller
	{
		internal static void InstallAtEnd(Systems feature, ISystem installee)
		{
			feature.Add(installee);
			if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.System))
			{
				Debug.LogFormat(
					"Mod {0} ({1}) installed system {3} | feature: {2}",
					ModLink.modIndex,
					ModLink.modID,
					feature.GetType().Name,
					installee.GetType().FullName);
			}
		}

		internal static void InstallBefore<T>(Systems feature, ISystem installee)
			where T : ISystem
		{
			Install<T>(feature, installee);
		}

		internal static void InstallAfter<T>(Systems feature, ISystem installee)
			where T : ISystem
		{
			Install<T>(feature, installee, 1);
		}

		private static void Install<T>(Systems feature, ISystem installee, int at = 0)
			where T : ISystem
		{
			var installed = false;

			if (installee is IInitializeSystem init)
			{
				Install<IInitializeSystem, T>(feature, "initialize", init, at);
				installed = true;
			}
			if (installee is IExecuteSystem exec)
			{
				Install<IExecuteSystem, T>(feature, "execute", exec, at);
				installed = true;
			}
			if (installee is ICleanupSystem cleanup)
			{
				Install<ICleanupSystem, T>(feature, "cleanup", cleanup, at);
				installed = true;
			}
			if (installee is ITearDownSystem tearDown)
			{
				Install<ITearDownSystem, T>(feature, "tearDown", tearDown, at);
				installed = true;
			}
			if (installee is IEnableSystem enable)
			{
				Install<IEnableSystem, T>(feature, "enable", enable, at);
				installed = true;
			}
			if (installee is IDisableSystem disable)
			{
				Install<IDisableSystem, T>(feature, "disable", disable, at);
				installed = true;
			}

			if (!installed)
			{
				Debug.LogWarningFormat(
					"Mod {0} ({1}) Install unable to install system -- new system doesn't implement any installable interface | feature: {2} | installee: {3}",
					ModLink.modIndex,
					ModLink.modID,
					feature.GetType().Name,
					installee.GetType().FullName);
			}
		}

		static void Install<S, T>(Systems feature, string kind, S installee, int at = 0)
			where S : ISystem
			where T : ISystem
		{
			var fi = AccessTools.Field(feature.GetType(), $"_{kind}Systems");
			if (fi == null)
			{
				Debug.LogWarningFormat(
					"Mod {0} ({1}) Install attempted to install a system kind that the feature doesn't support | feature: {2} | kind: {3} | installee: {4}",
					ModLink.modIndex,
					ModLink.modID,
					feature.GetType().Name,
					kind,
					installee.GetType().FullName);
				return;
			}

			var systems = (List<S>)fi.GetValue(feature);
			var i = 0;
			for (; i < systems.Count; i += 1)
			{
				if (systems[i] is T)
				{
					break;
				}
			}

			fi = AccessTools.Field(feature.GetType(), $"_{kind}SystemNames");
			var names = fi != null
				? (List<string>)fi.GetValue(feature)
				: null;

			var insert = i != systems.Count;
			if (insert)
			{
				systems.Insert(i + at, installee);
				names?.Insert(i + at, installee.GetType().FullName);
				if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.System))
				{
					Debug.LogFormat(
						"Mod {0} ({1}) Install found {5} ({4}) at {6} | feature: {2} | inserted system {3} at {7}",
						ModLink.modIndex,
						ModLink.modID,
						feature.GetType().Name,
						installee.GetType().FullName,
						typeof(S).Name,
						typeof(T).Name,
						i,
						i + at);
				}
			}
			else
			{
				systems.Add(installee);
				names?.Add(installee.GetType().FullName);
				if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.System))
				{
					Debug.LogFormat(
						"Mod {0} ({1}) Install did not find system {5} ({4}) | feature: {2} | appended system {3}",
						ModLink.modIndex,
						ModLink.modID,
						feature.GetType().Name,
						installee.GetType().FullName,
						typeof(S).Name,
						typeof(T).Name);
				}
			}
		}

		internal static void Replace<T, U>(Systems feature, U replacement)
			where T : ISystem
			where U : ISystem
		{
			var installed = false;

			if (replacement is IInitializeSystem init)
			{
				ReplaceSystem<IInitializeSystem, T>(feature, "initialize", init);
				installed = true;
			}
			if (replacement is IExecuteSystem exec)
			{
				ReplaceSystem<IExecuteSystem, T>(feature, "execute", exec);
				installed = true;
			}
			if (replacement is ICleanupSystem cleanup)
			{
				ReplaceSystem<ICleanupSystem, T>(feature, "cleanup", cleanup);
				installed = true;
			}
			if (replacement is ITearDownSystem tearDown)
			{
				ReplaceSystem<ITearDownSystem, T>(feature, "tearDown", tearDown);
				installed = true;
			}
			if (replacement is IEnableSystem enable)
			{
				ReplaceSystem<IEnableSystem, T>(feature, "enable", enable);
				installed = true;
			}
			if (replacement is IDisableSystem disable)
			{
				ReplaceSystem<IDisableSystem, T>(feature, "disable", disable);
				installed = true;
			}

			if (!installed)
			{
				Debug.LogWarningFormat(
					"Mod {0} ({1}) unable to replace system -- new system doesn't implement any installable interface | feature: {2} | replacement: {3}",
					ModLink.modIndex,
					ModLink.modID,
					feature.GetType().Name,
					replacement.GetType().FullName);
			}
		}

		private static void ReplaceSystem<S, T>(Systems feature, string kind, S replacement)
			where S : ISystem
			where T : ISystem
		{
			var fi = AccessTools.Field(feature.GetType(), $"_{kind}Systems");
			var systems = (List<S>)fi.GetValue(feature);
			var i = 0;
			for (; i < systems.Count; i += 1)
			{
				if (systems[i] is T)
				{
					systems[i] = replacement;
					if (ModLink.Settings.IsLoggingEnabled(ModLink.ModSettings.LoggingFlag.System))
					{
						Debug.LogFormat(
							"Mod {0} ({1}) replaced system {5} ({4}) with {3} | feature: {2}",
							ModLink.modIndex,
							ModLink.modID,
							feature.GetType().Name,
							replacement.GetType().FullName,
							typeof(S).Name,
							typeof(T).Name);
					}
					return;
				}
			}

			Debug.LogWarningFormat(
				"Mod {0} ({1}) unable to replace system -- {5} ({4}) was not found | feature: {2} | replacement: {3}",
				ModLink.modIndex,
				ModLink.modID,
				feature.GetType().Name,
				replacement.GetType().FullName,
				typeof(S).Name,
				typeof(T).Name);
		}
	}
}
