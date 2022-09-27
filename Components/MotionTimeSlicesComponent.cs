using Entitas;

namespace EchKode.PBMods.MissilePrediction.ECS
{
	[EkPrediction]
	public sealed class MotionTimeSlicesComponent : IComponent
	{
		public MotionTimeSlice[] a;
	}
}
