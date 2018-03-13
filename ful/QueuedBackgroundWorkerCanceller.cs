using System;

namespace FUL
{
	public class QueuedBackgroundWorkerCanceller : QueuedBackgroundWorker
	{
		public QueuedBackgroundWorkerCanceller()
			: this(true, true)
		{
		}

		public QueuedBackgroundWorkerCanceller(bool supportsProgress, bool supportsCancellation)
		{
			_supportsProgress = supportsProgress;
			_supportsCancellation = supportsCancellation;
		}

		public bool IsBusy
		{
			get
			{
				lock (_collectionsLock)
				{
					return _operationQueue.Count > 0 && !_cancelAllPending;
				}
			}
		}

		public new void RunWorkerAsync(object userState)
		{
			if (userState == null)
			{
				throw new ArgumentNullException("userState cannot be null.");
			}

			int prevCount;
			OperationRequest opRequest = new OperationRequest(userState, OperationHandler);
			OperationInfo opInfo = new OperationInfo(opRequest);

			lock (_collectionsLock)
			{
				if (_userStateToOperationMap.ContainsKey(userState))
				{
					throw new InvalidOperationException("The specified userKey has already been used to identify a pending operation.  Each userState parameter must be unique.");
				}

				// Cancel all existing queued threads.
				foreach (object key in _userStateToOperationMap.Keys)
				{
					OperationInfo cancelOpInfo = _userStateToOperationMap[key] as OperationInfo;

					if (cancelOpInfo != null)
					{
						cancelOpInfo.OperationRequest.Cancel();
					}
				}

				// Make a note of the current pending queue size.  If it's zero at this point,
				// we'll need to kick off an operation.
				//
				prevCount = _operationQueue.Count;

				// Place the new work item on the queue & also in the userState-to-OperationInfo map.
				//
				_operationQueue.Enqueue(opInfo);
				_userStateToOperationMap[userState] = opInfo;
			}

			if (prevCount == 0)
			{
				// We just queued up the first item - kick off the operation.
				//
				opRequest.OperationHandler.BeginInvoke(opInfo, OperationHandlerDone, opInfo);
			}
		}
	}
}
