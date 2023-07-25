// Copyright (c) 2022 EchKode
// SPDX-License-Identifier: BSD-3-Clause

﻿using Entitas;
using Entitas.CodeGeneration.Attributes;

namespace EchKode.PBMods.MissilePrediction.ECS
{
	[EkTime]
	[Unique]
	public sealed class SlicesPerSecondComponent : IComponent
	{
		public int i;
	}
}
