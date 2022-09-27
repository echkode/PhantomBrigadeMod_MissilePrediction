using Entitas;

using PhantomBrigade.Data;

namespace EchKode.PBMods.MissilePrediction.ECS
{
	[EkPrediction]
	public sealed class ActivationTimingComponent : IComponent
	{
		public DataBlockSubsystemActivationTiming instance;
	}
}
