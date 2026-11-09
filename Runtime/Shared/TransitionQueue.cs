using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace com.ktgame.manager.ui
{
	internal sealed class TransitionQueue
	{
		private readonly List<string> _pendingKeys = new();
		private int _nextTicket;
		private int _servingTicket;
		
		public string RunningKey { get; private set; }

		public bool HasPending => _pendingKeys.Count > 0;

		public bool IsDuplicate(string key, bool isBusy)
		{
			return (isBusy && RunningKey == key) || _pendingKeys.Contains(key);
		}

		public void MarkRunning(string key)
		{
			RunningKey = key;
		}

		public async UniTask<bool> WaitForTurnAsync(string key, Func<bool> isBusy, CancellationToken cancellationToken)
		{
			var ticket = _nextTicket++;
			_pendingKeys.Add(key);

			try
			{
				var canceled = await UniTask
					.WaitUntil(() => _servingTicket == ticket && isBusy() == false, cancellationToken: cancellationToken)
					.SuppressCancellationThrow();

				return canceled == false;
			}
			finally
			{
				_pendingKeys.Remove(key);

				if (_servingTicket == ticket)
				{
					_servingTicket++;
				}
			}
		}
	}
}
