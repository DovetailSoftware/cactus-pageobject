// /////////////////////////////////////////////////////////////////////
//  This is free software licensed under the NUnit license. You
//  may obtain a copy of the license as well as information regarding
//  copyright ownership at http://nunit.org.    
// /////////////////////////////////////////////////////////////////////

using System;
using System.Threading;

namespace NUnit_retry
{
	using NUnit.Core;

	public class RetriedTestMethod : NUnitTestMethod
	{
		readonly int _requiredPassCount;

		readonly int _tryCount;

		public RetriedTestMethod(NUnitTestMethod test, int tryCount, int requiredPassCount)
			: base(test.Method)
		{
			_tryCount = tryCount;
			_requiredPassCount = requiredPassCount;
		}

		public override TestResult Run(EventListener listener, ITestFilter filter)
		{
			var successCount = 0;
			TestResult failureResult = null;

			for (var i = 0; i < _tryCount; i++)
			{
				if (i > 0)
				{
					Console.WriteLine(string.Format("ailed Retrying Test Case now...xRETRYx.. [{0}] of [{1}]", i + 1,  _tryCount));
				}
				if (i > 1)
				{
					Console.WriteLine(string.Format("Pausing for retry for {0} seconds", i));
					Thread.Sleep(i*2000);  // increase the pause on each run (reason to allow CPU/db, etc to die off and start the tests fresh.)
				}
				var result = base.Run(listener, filter);

				if (!TestFailed(result))
				{
					if (i == 0)
					{
						return result;
					}

					if (++successCount >= _requiredPassCount)
					{
						return result;
					}
				}
				else
				{
					failureResult = result;
				}
			}

			return failureResult;
		}

		private static bool TestFailed(TestResult result)
		{
			return result.ResultState == ResultState.Error || result.ResultState == ResultState.Failure;
		}

	}
}
