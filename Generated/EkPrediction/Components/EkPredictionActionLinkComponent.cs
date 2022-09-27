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

		public ActionLinkComponent actionLink { get { return (ActionLinkComponent)GetComponent(EkPredictionComponentsLookup.ActionLink); } }
		public bool hasActionLink { get { return HasComponent(EkPredictionComponentsLookup.ActionLink); } }

		public void AddActionLink(int newActionID)
		{
			var index = EkPredictionComponentsLookup.ActionLink;
			var component = (ActionLinkComponent)CreateComponent(index, typeof(ActionLinkComponent));
			component.actionID = newActionID;
			AddComponent(index, component);
		}

		public void ReplaceActionLink(int newActionID)
		{
			var index = EkPredictionComponentsLookup.ActionLink;
			var component = (ActionLinkComponent)CreateComponent(index, typeof(ActionLinkComponent));
			component.actionID = newActionID;
			ReplaceComponent(index, component);
		}

		public void RemoveActionLink()
		{
			RemoveComponent(EkPredictionComponentsLookup.ActionLink);
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

		static Entitas.IMatcher<EkPredictionEntity> _matcherActionLink;

		public static Entitas.IMatcher<EkPredictionEntity> ActionLink
		{
			get
			{
				if (_matcherActionLink == null)
				{
					var matcher = (Entitas.Matcher<EkPredictionEntity>)Entitas.Matcher<EkPredictionEntity>.AllOf(EkPredictionComponentsLookup.ActionLink);
					matcher.componentNames = EkPredictionComponentsLookup.componentNames;
					_matcherActionLink = matcher;
				}

				return _matcherActionLink;
			}
		}
	}
}
