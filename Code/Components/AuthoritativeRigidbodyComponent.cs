// Copyright (c) 2022 EchKode
// SPDX-License-Identifier: BSD-3-Clause

﻿using Entitas;

using UnityEngine;

namespace EchKode.PBMods.MissilePrediction.ECS
{
	[EkPrediction]
	public sealed class AuthoritativeRigidbodyComponent : IComponent
	{
		public Rigidbody rb;
	}
}
