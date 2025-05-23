namespace com.ktgame.manager.ui
{
	public class TabDataSource : IViewDataSource
	{
		public int Index { get; }

		public TabDataSource(int index)
		{
			Index = index;
		}
	}
}
