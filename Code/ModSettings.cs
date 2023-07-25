// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.IO;

using UnityEngine;

namespace EchKode.PBMods.MissilePrediction
{
	partial class ModLink
	{
		internal sealed class ModSettings
		{
			[System.Flags]
			internal enum LoggingFlag
			{
				None = 0,
				System = 0x1,
				Time = 0x2,
				Link = 0x4,
				Recalc = 0x8,
				Guidance = 0x10,
				Asset = 0x20,
				NewRound = 0x40,
				Trace = 0x80,
				All = 0xFF,
			}

#pragma warning disable CS0649
			public LoggingFlag logging;
			public bool extraMotionData;
			public int slicesPerSecond = 60;
			public int computeSlicesPerFrame = 20;
			public float chaseDistance = 10f;
			public float triggerDistance = 2f;
			public int animatorDelay = 5;
			public float timeTargetThreshold = 0.01f;
			public bool trackFriendlyMissiles = false;
#pragma warning restore CS0649

			internal bool IsLoggingEnabled(LoggingFlag flag) => (logging & flag) == flag;
		}

		internal static ModSettings Settings;

		static void LoadSettings()
		{
			var settingsPath = Path.Combine(modPath, "settings.yaml");
			Settings = UtilitiesYAML.ReadFromFile<ModSettings>(settingsPath, false);
			if (Settings == null)
			{
				Settings = new ModSettings();

				Debug.LogFormat(
					"Mod {0} ({1}) no settings file found, using defaults | path: {2}",
					modIndex,
					modID,
					settingsPath);
			}

			if (Settings.logging != ModSettings.LoggingFlag.None)
			{
				Debug.LogFormat(
					"Mod {0} ({1}) diagnostic logging is: {2}\n  extraMotionData: {3}\n  slicesPerSecond: {4}\n  computeSlicesPerFrame: {5}\n  chaseDistance: {6}\n  triggerDistance: {7}\n  animatorDelay: {8}\n  timeTargetThreshold: {9}\n  trackFriendlyMissiles: {10}",
					modIndex,
					modID,
					Settings.logging,
					Settings.extraMotionData,
					Settings.slicesPerSecond,
					Settings.computeSlicesPerFrame,
					Settings.chaseDistance,
					Settings.triggerDistance,
					Settings.animatorDelay,
					Settings.timeTargetThreshold,
					Settings.trackFriendlyMissiles);
			}
		}
	}
}
