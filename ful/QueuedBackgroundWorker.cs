// QueuedBackgroundWorker
//
// QueuedBackgroundWorker is a component patterned after System.ComponentModel.BackgroundWorker,
// but supporting multiple calls to RunWorkerAsync.  Like BackgroundWorker, this component
// will setup the use of a separate thread to raise a DoWork event.  And just like BackgroundWorker,
// this component raises a ProgressChanged and RunWorkerCompleted event, using the synchronization
// context that was active when the caller invoked RunWorkerAsync.  The major difference is just
// that RunWorkerAsync queues up requests to fire the DoWork event, and the internal implementation
// then takes steps to do so sequentially; raising the next DoWork event only after the previous
// DoWork event handler has returned.
//
// When RunWorkerAsync is called by a client, the information needed to process that request is
// placed in a queue.  If, as a result, the pending request queue has gone from being empty to
// having one item in it, then the CLR thread pool is used (via Delegate.BeginInvoke) to initiate
// a call to the client's DoWork event handler.  If the request just enqueued is not the first
// request on the queue, nothing else happens.
//
// When the pooled thread calls this component back, it will raise the DoWork event, calling
// back into client code.  This call is made in the context of a pooled thread (and so
// possibly not in the synchronization context used for the associated call to RunWorkerAsync).
// When this code returns, the CLR's asynchronous delegate invocation implementation will then
// call this component back (via the AsyncCallback delegate that was originally passed to
// Delegate.BeginInvoke).
//
// When the threadpool subsequently calls this component back to tell it the DoWork event
// handler processing code has completed, this component will use the caller's original
// synchronization context to raise the RunWorkerCompleted event.
//
// After the client's RunWorkerCompleted event handler has been invoked in the correct
// context, this component will check to see if there are still operations queued up.
// If so, the next operation is started.
//
// A note regarding the RunWorkerCompleted event...  Although this component passes 
// what looks to be a RunWorkerCompletedEventArgs class as the 2nd parameter, it's
// really passing an instance of RunWorkerCompletedEventArgsWithUserState (which derives
// from RunWorkerCompletedEventArgs).  This class, unlike the base class RunWorkerCompletedEventArgs,
// *will* have a valid UserState property that reflects whatever was passed to RunWorkerAsync.
//
// BE SURE to refer to the implementation of the RunWorkerCompletedEventArgsWithUserState at
// the end of this file see how this is accomplished before using this code.
//
// As packaged in the original sample, the build action for this source file is set to be compile,
// while the build action for QueuedBackgroundWorkerWithoutAbusingConstructorInfo.cs is
// set to none.  So QueuedBackgroundWorkerWithoutAbusingConstructorInfo.cs is ignored by the
// compiler in the original sample project, and exists for your reference.
//
// Mike Woodring
// http://www.bearcanyon.com
// http://www.pluralsight.com/mike
//
using System;
using System.Collections;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading;
using System.Reflection;

namespace FUL
{
	public class QueuedBackgroundWorker : Component
	{
		protected Queue<OperationInfo> _operationQueue = new Queue<OperationInfo>();  // Holds pending (possibly canceled) operation requests.
		protected Hashtable _userStateToOperationMap = new Hashtable();               // Maps user-supplied keys onto pending OperationInfo.
		protected object _collectionsLock = new object();                             // Used to synchronize all access to both of the above two collections.

		protected bool _supportsProgress;                                             // Set at construction.  Indicates whether this instance supports calls to ReportProgress.
		protected bool _supportsCancellation;                                         // Set at construction.  Indicates whether this instance supports calls to CancelAsync/CancelAllAsync.
		protected bool _cancelAllPending = false;

		protected delegate RunWorkerCompletedEventArgsWithUserState OperationHandlerDelegate(OperationInfo opInfo);

		// OperationRequest
		//
		// This class represents everything this component needs to know about
		// in order to carry out a single operation as requested by a call to
		// RunWorkerAsync.
		//
		protected class OperationRequest
		{
			internal readonly object UserState;
			internal readonly AsyncOperation AsyncOperation;
			internal readonly OperationHandlerDelegate OperationHandler;
			bool _cancelPending = false;

			internal OperationRequest(object userState, OperationHandlerDelegate operationHandler)
			{
				UserState = userState;
				OperationHandler = operationHandler;
				AsyncOperation = AsyncOperationManager.CreateOperation(this);
			}

			internal bool CancelPending
			{
				get { return (_cancelPending); }
			}

			internal void Cancel()
			{
				_cancelPending = true;
			}
		}

		// OperationInfo
		//
		// This class combines a request (OperationRequest) with a result
		// (RunWorkerCompletedEventArgsWithUserState), and is what's queued up
		// and processed by this component.
		//
		protected class OperationInfo
		{
			OperationRequest _request;
			RunWorkerCompletedEventArgsWithUserState _result;

			internal OperationInfo(OperationRequest request)
			{
				_request = request;
				_result = null;
			}

			internal OperationRequest OperationRequest
			{
				get { return (_request); }
			}

			internal RunWorkerCompletedEventArgsWithUserState OperationResult
			{
				get
				{
					if (_result == null)
					{
						throw new InvalidOperationException("The operation result has not been set yet.");
					}

					return (_result);
				}

				set
				{
					if (_result != null)
					{
						throw new InvalidOperationException("The operation result has already been set.");
					}

					_result = value;
				}
			}
		}

		// Context: client.
		//
		public QueuedBackgroundWorker()
			: this(true, true)
		{
		}

        public QueuedBackgroundWorker(DoWorkEventHandler doWork) : this()
        {
            DoWork += doWork;
        }

        public QueuedBackgroundWorker(bool supportsProgress, bool supportsCancellation)
		{
			_supportsProgress = supportsProgress;
			_supportsCancellation = supportsCancellation;
		}

		// Context: client.
		//
		public void RunWorkerAsync(object userState)
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

		// Context: client | async.
		//
		public void ReportProgress(int percentComplete, object userState)
		{
			if (!_supportsProgress)
			{
				throw new InvalidOperationException("This instance of the QueuedBackgroundWorker does not support progress notification.");
			}

			OperationInfo opInfo;

			lock (_collectionsLock)
			{
				opInfo = _userStateToOperationMap[userState] as OperationInfo;
			}

			if (opInfo != null)
			{
				RaiseProgressChangedEventFromAsyncContext(percentComplete, userState, opInfo);
			}
		}

		public bool SupportsProgressReports
		{
			get { return (_supportsProgress); }
		}

		public bool SupportsCancellation
		{
			get { return (_supportsCancellation); }
		}

		// DoWork event support.
		//
		DoWorkEventHandler _doWork;
		object _doWorkLock = new object();

		public event DoWorkEventHandler DoWork
		{
			add
			{
				lock (_doWorkLock)
				{
					_doWork += value;
				}
			}

			remove
			{
				lock (_doWorkLock)
				{
					_doWork -= value;
				}
			}
		}

		void RaiseDoWorkEventFromAsyncContext(DoWorkEventArgs eventArgs)
		{
			Delegate[] targets;

			lock (_doWorkLock)
			{
				targets = _doWork.GetInvocationList();
			}

			foreach (DoWorkEventHandler handler in targets)
			{
				handler(this, eventArgs);
			}
		}

		// ProgressChanged event support.
		//
		ProgressChangedEventHandler _operationProgressChanged;
		object _operationProgressChangedLock = new object();

		public event ProgressChangedEventHandler ProgressChanged
		{
			add
			{
				lock (_operationProgressChangedLock)
				{
					_operationProgressChanged += value;
				}
			}

			remove
			{
				lock (_operationProgressChangedLock)
				{
					_operationProgressChanged -= value;
				}
			}
		}

		void RaiseProgressChangedEventFromAsyncContext(int percentComplete, object userState, OperationInfo opInfo)
		{
			ProgressChangedEventArgs eventArgs = new ProgressChangedEventArgs(percentComplete, userState);
			opInfo.OperationRequest.AsyncOperation.Post(RaiseProgressChangedEventFromClientContext, eventArgs);
		}

		void RaiseProgressChangedEventFromClientContext(object state)
		{
			ProgressChangedEventArgs eventArgs = (ProgressChangedEventArgs)state;
			Delegate[] targets;

			lock (_operationProgressChangedLock)
			{
				targets = _operationProgressChanged.GetInvocationList();
			}

			foreach (ProgressChangedEventHandler handler in targets)
			{
				try
				{
					handler(this, eventArgs);
				}
				catch
				{
				}
			}
		}

		// RunWorkerCompleted event support.
		//
		RunWorkerCompletedEventHandler _operationCompleted;
		object _operationCompletedLock = new object();

		public event RunWorkerCompletedEventHandler RunWorkerCompleted
		{
			add
			{
				lock (_operationCompletedLock)
				{
					_operationCompleted += value;
				}
			}

			remove
			{
				lock (_operationCompletedLock)
				{
					_operationCompleted -= value;
				}
			}
		}

		void RaiseWorkCompletedEventFromAsyncContext(OperationInfo opInfo)
		{
			opInfo.OperationRequest.AsyncOperation.PostOperationCompleted(RaiseWorkCompletedEventFromClientContext, opInfo);
		}

		void RaiseWorkCompletedEventFromClientContext(object state)
		{
			OperationInfo opInfo = (OperationInfo)state;
			RunWorkerCompletedEventArgsWithUserState eventArgs = opInfo.OperationResult;
			Delegate[] targets = null;

			lock (_operationCompletedLock)
			{
				if (_operationCompleted != null)
					targets = _operationCompleted.GetInvocationList();
			}

			if (targets != null)
			{
				foreach (RunWorkerCompletedEventHandler handler in targets)
				{
					try
					{
						handler(this, eventArgs);
					}
					catch
					{
					}
				}
			}

			// Now that we're done calling back to the client to let them know
			// that the operation has completed, remove this operation from the
			// queue and check to see if we need to start another operation.
			//
			OperationRequest opRequest = opInfo.OperationRequest;
			OperationInfo nextOp = null;

			lock (_collectionsLock)
			{
				if ((_operationQueue.Peek() != opInfo) || !_userStateToOperationMap.ContainsKey(opRequest.UserState))
				{
					throw new InvalidOperationException("Something freaky happened.");
				}

				_operationQueue.Dequeue();
				_userStateToOperationMap.Remove(opRequest.UserState);

				if (_operationQueue.Count > 0)
				{
					nextOp = _operationQueue.Peek();
				}
				else
				{
					_cancelAllPending = false;
				}
			}

			if (nextOp != null)
			{
				// We have more work items pending.  Kick off another operation.
				//
				nextOp.OperationRequest.OperationHandler.BeginInvoke(nextOp, OperationHandlerDone, nextOp);
			}
		}

		// Context: client | async.
		//
		public void CancelAsync(object userState)
		{
			if (!_supportsCancellation)
			{
				throw new InvalidOperationException("This instance of the QueuedBackgroundWorker does not support cancellation.");
			}

			OperationInfo opInfo = GetOperationForUserKey(userState);

			if (opInfo != null)
			{
				opInfo.OperationRequest.Cancel();
			}
		}

		// Context: client | async.
		//
		public void CancelAllAsync()
		{
			if (!_supportsCancellation)
			{
				throw new InvalidOperationException("This instance of the QueuedBackgroundWorker does not support cancellation.");
			}

			lock (_collectionsLock)
			{
				_cancelAllPending = true;

				foreach (object key in _userStateToOperationMap.Keys)
				{
					OperationInfo opInfo = _userStateToOperationMap[key] as OperationInfo;

					if (opInfo != null)
					{
						opInfo.OperationRequest.Cancel();
					}
				}

				_cancelAllPending = false;
			}
		}

		// Context: client | async.
		//
		public bool IsCancellationPending(object userState)
		{
			if (!_supportsCancellation)
			{
				return (false);
			}

			if (_cancelAllPending)
			{
				return (true);
			}

			OperationInfo opInfo = GetOperationForUserKey(userState);
			return (opInfo != null ? opInfo.OperationRequest.CancelPending : false);
		}

		protected OperationInfo GetOperationForUserKey(object userKey)
		{
			lock (_collectionsLock)
			{
				return (_userStateToOperationMap[userKey] as OperationInfo);
			}
		}

		// Context: async.
		//
		protected RunWorkerCompletedEventArgsWithUserState OperationHandler(OperationInfo opInfo)
		{
			object userState = opInfo.OperationRequest.UserState;
			DoWorkEventArgs eventArgs = new DoWorkEventArgs(userState);

			try
			{
				RaiseDoWorkEventFromAsyncContext(eventArgs);

				if (eventArgs.Cancel)
				{
					opInfo.OperationRequest.Cancel(); // For the sake of completeness.
					return new RunWorkerCompletedEventArgsWithUserState(null, null, true, userState);
				}
				else
				{
					return new RunWorkerCompletedEventArgsWithUserState(eventArgs.Result, null, false, userState);
				}
			}
			catch (Exception err)
			{
				return new RunWorkerCompletedEventArgsWithUserState(null, err, false, userState);
			}
		}

		// Context: async.
		//
		protected void OperationHandlerDone(IAsyncResult ar)
		{
			OperationInfo opInfo = (OperationInfo)ar.AsyncState;
			opInfo.OperationResult = opInfo.OperationRequest.OperationHandler.EndInvoke(ar);
			RaiseWorkCompletedEventFromAsyncContext(opInfo);
		}

		// Context: client.
		//
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				CancelAllAsync();
			}

			base.Dispose(disposing);
		}

		public bool Contains(object userState)
		{
			foreach (OperationInfo opInfo in _userStateToOperationMap.Values)
			{
				if (opInfo.OperationRequest.UserState.Equals(userState))
					return true;
			}
			return false;
		}
	}

	public class RunWorkerCompletedEventArgsWithUserState : RunWorkerCompletedEventArgs
	{
		// The constructor for this class accepts 4 pieces of information, 3 of which
		// are passed as is to the base class RunWorkerCompletedEventArgs constructor.
		// The constructor for RunWorkerCompletedEventArgs passes null to the constructor
		// for the userState parameter of *its* base class (AsyncCompletedEventArgs).
		//
		// The result is that after the call to the base class constructor returns,
		// the exception reference and cancelled flag have been stored in AsyncCompletedEventArgs's
		// private fields, and the result reference has been stored in RunWorkerCompletedEventArgs's
		// private fields.
		//
		// Once this process has finished, and the body of the constructor defined below starts
		// to execute, everything except the userState field of AsyncCompletedEventArgs has been
		// initialized.  To fix that, this constructor uses reflection to manually invoke
		// the AsyncCompletedEventArgs constructor, passing in the userState parameter that was
		// ignored by RunWorkerCompletedEventArgs's constructor.
		//
		// Note that this only works because the AsyncCompletedEventArgs constructor doesn't do
		// anything but assign the parameter values to corresponding fields.  So calling its constructor
		// a 2nd time doesn't cause any unwanted side effects.  But this is only the case because
		// we can see its implementation.  If that implementation were to change, then the following
		// code could introduce a bug (nobody expects the constructor to be called multiple times
		// for the same instance of an object).
		//
		// If you're uncomfortable with this approach, refer to QueuedBackgroundWorkerWithoutAbusingConstructorInfo.cs
		// which provides a solution that doesn't abuse ConstructorInfo in this fashion.
		//
		public RunWorkerCompletedEventArgsWithUserState(object result, Exception error, bool cancelled, object userState)
			: base(result, error, cancelled)
		{
			// Locate the AsyncCompletedEventArgs constructor we want to call.
			//
			Type[] ctorArgs = { typeof(Exception), typeof(bool), typeof(object) };
			ConstructorInfo ctor = typeof(AsyncCompletedEventArgs).GetConstructor(ctorArgs);

			// Call it, passing in the original error reference & cancelled flag,
			// and the all important userState reference.
			//
			ctor.Invoke(this, new object[] { error, cancelled, userState });
		}
	}
}
