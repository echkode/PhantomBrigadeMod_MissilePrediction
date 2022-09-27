using Entitas;
using Entitas.CodeGeneration.Attributes;

namespace EchKode.PBMods.MissilePrediction.ECS
{
	[EkTime]
	[Unique]
	public sealed class CurrentTimeSliceComponent : IComponent
	{
		public int i;
	}
}
