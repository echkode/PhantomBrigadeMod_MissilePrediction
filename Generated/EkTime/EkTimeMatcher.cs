namespace EchKode.PBMods.MissilePrediction.ECS
{
	//------------------------------------------------------------------------------
	// <auto-generated>
	//     This code was generated by Entitas.CodeGeneration.Plugins.ContextMatcherGenerator.
	//
	//     Changes to this file may cause incorrect behavior and will be lost if
	//     the code is regenerated.
	// </auto-generated>
	//------------------------------------------------------------------------------
	public sealed partial class EkTimeMatcher
	{

		public static Entitas.IAllOfMatcher<EkTimeEntity> AllOf(params int[] indices)
		{
			return Entitas.Matcher<EkTimeEntity>.AllOf(indices);
		}

		public static Entitas.IAllOfMatcher<EkTimeEntity> AllOf(params Entitas.IMatcher<EkTimeEntity>[] matchers)
		{
			return Entitas.Matcher<EkTimeEntity>.AllOf(matchers);
		}

		public static Entitas.IAnyOfMatcher<EkTimeEntity> AnyOf(params int[] indices)
		{
			return Entitas.Matcher<EkTimeEntity>.AnyOf(indices);
		}

		public static Entitas.IAnyOfMatcher<EkTimeEntity> AnyOf(params Entitas.IMatcher<EkTimeEntity>[] matchers)
		{
			return Entitas.Matcher<EkTimeEntity>.AnyOf(matchers);
		}
	}
}