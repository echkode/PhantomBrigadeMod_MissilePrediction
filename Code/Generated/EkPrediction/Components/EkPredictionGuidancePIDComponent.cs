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

		public GuidancePIDComponent guidancePID { get { return (GuidancePIDComponent)GetComponent(EkPredictionComponentsLookup.GuidancePID); } }
		public bool hasGuidancePID { get { return HasComponent(EkPredictionComponentsLookup.GuidancePID); } }

		public void AddGuidancePID(SimplePID newSteeringPID, SimplePID newPitchPID)
		{
			var index = EkPredictionComponentsLookup.GuidancePID;
			var component = (GuidancePIDComponent)CreateComponent(index, typeof(GuidancePIDComponent));
			component.steeringPID = newSteeringPID;
			component.pitchPID = newPitchPID;
			AddComponent(index, component);
		}

		public void ReplaceGuidancePID(SimplePID newSteeringPID, SimplePID newPitchPID)
		{
			var index = EkPredictionComponentsLookup.GuidancePID;
			var component = (GuidancePIDComponent)CreateComponent(index, typeof(GuidancePIDComponent));
			component.steeringPID = newSteeringPID;
			component.pitchPID = newPitchPID;
			ReplaceComponent(index, component);
		}

		public void RemoveGuidancePID()
		{
			RemoveComponent(EkPredictionComponentsLookup.GuidancePID);
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

		static Entitas.IMatcher<EkPredictionEntity> _matcherGuidancePID;

		public static Entitas.IMatcher<EkPredictionEntity> GuidancePID
		{
			get
			{
				if (_matcherGuidancePID == null)
				{
					var matcher = (Entitas.Matcher<EkPredictionEntity>)Entitas.Matcher<EkPredictionEntity>.AllOf(EkPredictionComponentsLookup.GuidancePID);
					matcher.componentNames = EkPredictionComponentsLookup.componentNames;
					_matcherGuidancePID = matcher;
				}

				return _matcherGuidancePID;
			}
		}
	}
}
