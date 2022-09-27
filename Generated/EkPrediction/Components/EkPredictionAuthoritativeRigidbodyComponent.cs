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

		public AuthoritativeRigidbodyComponent authoritativeRigidbody { get { return (AuthoritativeRigidbodyComponent)GetComponent(EkPredictionComponentsLookup.AuthoritativeRigidbody); } }
		public bool hasAuthoritativeRigidbody { get { return HasComponent(EkPredictionComponentsLookup.AuthoritativeRigidbody); } }

		public void AddAuthoritativeRigidbody(UnityEngine.Rigidbody newRb)
		{
			var index = EkPredictionComponentsLookup.AuthoritativeRigidbody;
			var component = (AuthoritativeRigidbodyComponent)CreateComponent(index, typeof(AuthoritativeRigidbodyComponent));
			component.rb = newRb;
			AddComponent(index, component);
		}

		public void ReplaceAuthoritativeRigidbody(UnityEngine.Rigidbody newRb)
		{
			var index = EkPredictionComponentsLookup.AuthoritativeRigidbody;
			var component = (AuthoritativeRigidbodyComponent)CreateComponent(index, typeof(AuthoritativeRigidbodyComponent));
			component.rb = newRb;
			ReplaceComponent(index, component);
		}

		public void RemoveAuthoritativeRigidbody()
		{
			RemoveComponent(EkPredictionComponentsLookup.AuthoritativeRigidbody);
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

		static Entitas.IMatcher<EkPredictionEntity> _matcherAuthoritativeRigidbody;

		public static Entitas.IMatcher<EkPredictionEntity> AuthoritativeRigidbody
		{
			get
			{
				if (_matcherAuthoritativeRigidbody == null)
				{
					var matcher = (Entitas.Matcher<EkPredictionEntity>)Entitas.Matcher<EkPredictionEntity>.AllOf(EkPredictionComponentsLookup.AuthoritativeRigidbody);
					matcher.componentNames = EkPredictionComponentsLookup.componentNames;
					_matcherAuthoritativeRigidbody = matcher;
				}

				return _matcherAuthoritativeRigidbody;
			}
		}
	}
}
