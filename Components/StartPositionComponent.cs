using Entitas;

using UnityEngine;

namespace EchKode.PBMods.MissilePrediction.ECS
{
	[EkPrediction]
	public sealed class StartPositionComponent : IComponent
	{
		public Vector3 v;
	}
}
