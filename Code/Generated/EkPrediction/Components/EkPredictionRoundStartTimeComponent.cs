namespace EchKode.PBMods.MissilePrediction.ECS
{
	//------------------------------------------------------------------------------
	// <auto-generated>
	//     This code was generated by Entitas.CodeGeneration.Plugins.ComponentEntityApiGenerator.
	//
	//     Changes to this file may cause incorrect behavior and will be lost if
	//     the code is regenerated.
	// </auto-generated>
	//------------------------------------------------------------------------------
	public partial class EkPredictionEntity
	{

		public RoundStartTimeComponent roundStartTime { get { return (RoundStartTimeComponent)GetComponent(EkPredictionComponentsLookup.RoundStartTime); } }
		public bool hasRoundStartTime { get { return HasComponent(EkPredictionComponentsLookup.RoundStartTime); } }

		public void AddRoundStartTime(float newF)
		{
			var index = EkPredictionComponentsLookup.RoundStartTime;
			var component = (RoundStartTimeComponent)CreateComponent(index, typeof(RoundStartTimeComponent));
			component.f = newF;
			AddComponent(index, component);
		}

		public void ReplaceRoundStartTime(float newF)
		{
			var index = EkPredictionComponentsLookup.RoundStartTime;
			var component = (RoundStartTimeComponent)CreateComponent(index, typeof(RoundStartTimeComponent));
			component.f = newF;
			ReplaceComponent(index, component);
		}

		public void RemoveRoundStartTime()
		{
			RemoveComponent(EkPredictionComponentsLookup.RoundStartTime);
		}
	}

	//------------------------------------------------------------------------------
	// <auto-generated>
	//     This code was generated by Entitas.CodeGeneration.Plugins.ComponentMatcherApiGenerator.
	//
	//     Changes to this file may cause incorrect behavior and will be lost if
	//     the code is regenerated.
	// </auto-generated>
	//------------------------------------------------------------------------------
	public sealed partial class EkPredictionMatcher
	{

		static Entitas.IMatcher<EkPredictionEntity> _matcherRoundStartTime;

		public static Entitas.IMatcher<EkPredictionEntity> RoundStartTime
		{
			get
			{
				if (_matcherRoundStartTime == null)
				{
					var matcher = (Entitas.Matcher<EkPredictionEntity>)Entitas.Matcher<EkPredictionEntity>.AllOf(EkPredictionComponentsLookup.RoundStartTime);
					matcher.componentNames = EkPredictionComponentsLookup.componentNames;
					_matcherRoundStartTime = matcher;
				}

				return _matcherRoundStartTime;
			}
		}
	}
}
