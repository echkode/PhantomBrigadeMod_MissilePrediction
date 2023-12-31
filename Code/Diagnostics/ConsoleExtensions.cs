﻿// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using QFSW.QC;

namespace EchKode.PBMods.MissilePrediction.Diagnostics
{
	static class ConsoleExtensions
	{
		public static void LogAllToConsole(this QuantumConsole instance, string message)
		{
			while (message.Length > Constants.ConsoleOutputLengthMax)
			{
				var pos = message.LastIndexOf('\n', Constants.ConsoleOutputLengthMax - 1);
				instance.LogToConsole(message.Substring(0, pos));
				message = message.Substring(pos + 1);
			}
			instance.LogToConsole(message);
		}
	}
}
