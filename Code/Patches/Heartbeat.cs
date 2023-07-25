// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System;
using System.Collections.Generic;

using HarmonyLib;

using PhantomBrigade;

namespace EchKode.PBMods.MissilePrediction
{
	static class Heartbeat
	{
		internal static readonly List<Action<GameController>> SystemInstalls = new List<Action<GameController>>()
		{
			MissilePredictionFeature.Install,
		};

		public static void Start()
		{
			var fi = AccessTools.DeclaredField(typeof(PhantomBrigade.Heartbeat), "_gameController");
			if (fi == null)
			{
				return;
			}

			var gameController = (GameController)fi.GetValue(null);
			SystemInstalls.ForEach(load => load(gameController));
		}
	}
}
