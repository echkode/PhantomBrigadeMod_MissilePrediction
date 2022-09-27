using Entitas;

namespace EchKode.PBMods.MissilePrediction.ECS
{
	[EkPrediction]
	public sealed class GuidancePIDComponent : IComponent
	{
		public SimplePID steeringPID;
		public SimplePID pitchPID;
	}
}
