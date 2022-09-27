using Entitas;

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
