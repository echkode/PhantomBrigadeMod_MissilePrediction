using Entitas;

using UnityEngine;

namespace EchKode.PBMods.MissilePrediction.ECS
{
	[EkPrediction]
	public sealed class AuthoritativeRigidbodyComponent : IComponent
	{
		public Rigidbody rb;
	}
}
