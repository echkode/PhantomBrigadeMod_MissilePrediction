// Copyright (c) 2022 EchKode
// SPDX-License-Identifier: BSD-3-Clause

﻿using Entitas;

using PhantomBrigade.Data;

namespace EchKode.PBMods.MissilePrediction.ECS
{
	[EkPrediction]
	public sealed class ColorsComponent : IComponent
	{
		public DataBlockFloat01 hueOffset;
		public DataBlockColorInterpolated colorOverride;
	}
}
