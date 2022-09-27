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

		public ProjectileLinkComponent projectileLink { get { return (ProjectileLinkComponent)GetComponent(EkPredictionComponentsLookup.ProjectileLink); } }
		public bool hasProjectileLink { get { return HasComponent(EkPredictionComponentsLookup.ProjectileLink); } }

		public void AddProjectileLink(int newCombatID)
		{
			var index = EkPredictionComponentsLookup.ProjectileLink;
			var component = (ProjectileLinkComponent)CreateComponent(index, typeof(ProjectileLinkComponent));
			component.combatID = newCombatID;
			AddComponent(index, component);
		}

		public void ReplaceProjectileLink(int newCombatID)
		{
			var index = EkPredictionComponentsLookup.ProjectileLink;
			var component = (ProjectileLinkComponent)CreateComponent(index, typeof(ProjectileLinkComponent));
			component.combatID = newCombatID;
			ReplaceComponent(index, component);
		}

		public void RemoveProjectileLink()
		{
			RemoveComponent(EkPredictionComponentsLookup.ProjectileLink);
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

		static Entitas.IMatcher<EkPredictionEntity> _matcherProjectileLink;

		public static Entitas.IMatcher<EkPredictionEntity> ProjectileLink
		{
			get
			{
				if (_matcherProjectileLink == null)
				{
					var matcher = (Entitas.Matcher<EkPredictionEntity>)Entitas.Matcher<EkPredictionEntity>.AllOf(EkPredictionComponentsLookup.ProjectileLink);
					matcher.componentNames = EkPredictionComponentsLookup.componentNames;
					_matcherProjectileLink = matcher;
				}

				return _matcherProjectileLink;
			}
		}
	}
}
