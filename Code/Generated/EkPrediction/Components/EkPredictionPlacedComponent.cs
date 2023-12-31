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

		static readonly PlacedComponent placedComponent = new PlacedComponent();

		public bool isPlaced
		{
			get { return HasComponent(EkPredictionComponentsLookup.Placed); }
			set
			{
				if (value != isPlaced)
				{
					var index = EkPredictionComponentsLookup.Placed;
					if (value)
					{
						var componentPool = GetComponentPool(index);
						var component = componentPool.Count > 0
								? componentPool.Pop()
								: placedComponent;

						AddComponent(index, component);
					}
					else
					{
						RemoveComponent(index);
					}
				}
			}
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

		static Entitas.IMatcher<EkPredictionEntity> _matcherPlaced;

		public static Entitas.IMatcher<EkPredictionEntity> Placed
		{
			get
			{
				if (_matcherPlaced == null)
				{
					var matcher = (Entitas.Matcher<EkPredictionEntity>)Entitas.Matcher<EkPredictionEntity>.AllOf(EkPredictionComponentsLookup.Placed);
					matcher.componentNames = EkPredictionComponentsLookup.componentNames;
					_matcherPlaced = matcher;
				}

				return _matcherPlaced;
			}
		}
	}
}
