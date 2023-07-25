// Copyright (c) 2022 EchKode
// SPDX-License-Identifier: BSD-3-Clause

﻿using Entitas;

using UnityEngine;

namespace EchKode.PBMods.MissilePrediction.ECS
{
	[EkPrediction]
	public sealed class FlightInfoComponent : IComponent
	{
		public float time;
		public float distance;
		public Vector3 origin;
		public Vector3 positionLast;
	}
}
