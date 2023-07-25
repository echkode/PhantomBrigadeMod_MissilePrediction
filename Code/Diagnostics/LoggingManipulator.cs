// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;
using System.Reflection;

using HarmonyLib;

using QFSW.QC;

namespace EchKode.PBMods.MissilePrediction.Diagnostics
{
	using CommandList = List<(string QCName, string Description, MethodInfo Method)>;

	static class LoggingManipulator
	{
		internal static CommandList Commands() => new CommandList()
		{
			("show-logging", "Show all logging flag names and currently set logging flags for MissilePrediction mod", AccessTools.DeclaredMethod(typeof(LoggingManipulator), nameof(ShowLogging))),
			("change-logging", "Change logging flags for MissilePrediction mod", AccessTools.DeclaredMethod(typeof(LoggingManipulator), nameof(ChangeLogging))),
			("add-logging-flags", "Add logging flags for MissilePrediction mod", AccessTools.DeclaredMethod(typeof(LoggingManipulator), nameof(AddLoggingFlags))),
			("remove-logging-flags", "Remove logging flags for MissilePrediction mod", AccessTools.DeclaredMethod(typeof(LoggingManipulator), nameof(RemoveLoggingFlags))),
		};

		static void ShowLogging()
		{
			QuantumConsole.Instance.LogToConsole("Logging flags for MissilePrediction mod");
			foreach (var name in System.Enum.GetNames(typeof(ModLink.ModSettings.LoggingFlag)))
			{
				QuantumConsole.Instance.LogToConsole($"  {name}");
			}
			QuantumConsole.Instance.LogToConsole("Current logging flags: " + ModLink.Settings.logging);
		}

		static void ChangeLogging(string loggingFlags)
		{
			if (!System.Enum.TryParse<ModLink.ModSettings.LoggingFlag>(loggingFlags, true, out var logSetting))
			{
				QuantumConsole.Instance.LogToConsole("Unable to parse logging flags: " + loggingFlags);
				return;
			}

			var oldSetting = ModLink.Settings.logging;
			ModLink.Settings.logging = logSetting;
			QuantumConsole.Instance.LogToConsole($"Changed logging flags: {oldSetting} --> {logSetting}");
		}

		static void AddLoggingFlags(string loggingFlags)
		{
			if (!System.Enum.TryParse<ModLink.ModSettings.LoggingFlag>(loggingFlags, true, out var extraFlags))
			{
				QuantumConsole.Instance.LogToConsole("Unable to parse logging flags: " + loggingFlags);
				return;
			}

			var oldSetting = ModLink.Settings.logging;
			var newSetting = oldSetting | extraFlags;
			if (oldSetting == newSetting)
			{
				return;
			}

			ModLink.Settings.logging = newSetting;
			QuantumConsole.Instance.LogToConsole($"Changed logging flags: {oldSetting} --> {newSetting}");
		}

		static void RemoveLoggingFlags(string loggingFlags)
		{
			if (!System.Enum.TryParse<ModLink.ModSettings.LoggingFlag>(loggingFlags, true, out var extraFlags))
			{
				QuantumConsole.Instance.LogToConsole("Unable to parse logging flags: " + loggingFlags);
				return;
			}

			var oldSetting = ModLink.Settings.logging;
			var newSetting = oldSetting & ~extraFlags;
			if (oldSetting == newSetting)
			{
				return;
			}

			ModLink.Settings.logging = newSetting;
			QuantumConsole.Instance.LogToConsole($"Changed logging flags: {oldSetting} --> {newSetting}");
		}
	}
}
