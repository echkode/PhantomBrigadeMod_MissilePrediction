namespace EchKode.PBMods.MissilePrediction.ECS
{
	//------------------------------------------------------------------------------
	// <auto-generated>
	//     This code was generated by Entitas.CodeGeneration.Plugins.ComponentContextApiGenerator.
	//
	//     Changes to this file may cause incorrect behavior and will be lost if
	//     the code is regenerated.
	// </auto-generated>
	//------------------------------------------------------------------------------
	public partial class EkPredictionContext
	{

		public EkPredictionEntity pendingPlaceRoundEntity { get { return GetGroup(EkPredictionMatcher.PendingPlaceRound).GetSingleEntity(); } }

		public bool isPendingPlaceRound
		{
			get { return pendingPlaceRoundEntity != null; }
			set
			{
				var entity = pendingPlaceRoundEntity;
				if (value != (entity != null))
				{
					if (value)
					{
						CreateEntity().isPendingPlaceRound = true;
					}
					else
					{
						entity.Destroy();
					}
				}
			}
		}
	}

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

		static readonly PendingPlaceRoundComponent pendingPlaceRoundComponent = new PendingPlaceRoundComponent();

		public bool isPendingPlaceRound
		{
			get { return HasComponent(EkPredictionComponentsLookup.PendingPlaceRound); }
			set
			{
				if (value != isPendingPlaceRound)
				{
					var index = EkPredictionComponentsLookup.PendingPlaceRound;
					if (value)
					{
						var componentPool = GetComponentPool(index);
						var component = componentPool.Count > 0
								? componentPool.Pop()
								: pendingPlaceRoundComponent;

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

		static Entitas.IMatcher<EkPredictionEntity> _matcherPendingPlaceRound;

		public static Entitas.IMatcher<EkPredictionEntity> PendingPlaceRound
		{
			get
			{
				if (_matcherPendingPlaceRound == null)
				{
					var matcher = (Entitas.Matcher<EkPredictionEntity>)Entitas.Matcher<EkPredictionEntity>.AllOf(EkPredictionComponentsLookup.PendingPlaceRound);
					matcher.componentNames = EkPredictionComponentsLookup.componentNames;
					_matcherPendingPlaceRound = matcher;
				}

				return _matcherPendingPlaceRound;
			}
		}
	}
}
