namespace com.ktgame.manager.ui
{
	public readonly struct SheetViewConfig
	{
		public readonly ViewConfig Config;

		public SheetViewConfig(in ViewConfig options)
		{
			Config = options;
		}

		public SheetViewConfig(string resourcePath
			, bool playAnimation = true
			, bool loadAsync = true
			, PoolingPolicy poolingPolicy = PoolingPolicy.UseSettings)
		{
			Config = new ViewConfig(resourcePath, playAnimation, loadAsync, poolingPolicy);
		}

		public static implicit operator SheetViewConfig(in ViewConfig config) => new(config);

		public static implicit operator SheetViewConfig(string assetPath) => new(new ViewConfig(assetPath));

		public static implicit operator ViewConfig(in SheetViewConfig config) => config.Config;
	}
}
