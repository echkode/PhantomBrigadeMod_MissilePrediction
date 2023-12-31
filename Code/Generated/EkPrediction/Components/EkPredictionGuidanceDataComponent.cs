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

		public GuidanceDataComponent guidanceData { get { return (GuidanceDataComponent)GetComponent(EkPredictionComponentsLookup.GuidanceData); } }
		public bool hasGuidanceData { get { return HasComponent(EkPredictionComponentsLookup.GuidanceData); } }

		public void AddGuidanceData(PhantomBrigade.Data.DataBlockGuidanceData newData)
		{
			var index = EkPredictionComponentsLookup.GuidanceData;
			var component = (GuidanceDataComponent)CreateComponent(index, typeof(GuidanceDataComponent));
			component.data = newData;
			AddComponent(index, component);
		}

		public void ReplaceGuidanceData(PhantomBrigade.Data.DataBlockGuidanceData newData)
		{
			var index = EkPredictionComponentsLookup.GuidanceData;
			var component = (GuidanceDataComponent)CreateComponent(index, typeof(GuidanceDataComponent));
			component.data = newData;
			ReplaceComponent(index, component);
		}

		public void RemoveGuidanceData()
		{
			RemoveComponent(EkPredictionComponentsLookup.GuidanceData);
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

		static Entitas.IMatcher<EkPredictionEntity> _matcherGuidanceData;

		public static Entitas.IMatcher<EkPredictionEntity> GuidanceData
		{
			get
			{
				if (_matcherGuidanceData == null)
				{
					var matcher = (Entitas.Matcher<EkPredictionEntity>)Entitas.Matcher<EkPredictionEntity>.AllOf(EkPredictionComponentsLookup.GuidanceData);
					matcher.componentNames = EkPredictionComponentsLookup.componentNames;
					_matcherGuidanceData = matcher;
				}

				return _matcherGuidanceData;
			}
		}
	}
}

