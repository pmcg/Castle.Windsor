// Copyright 2004-2011 Castle Project - http://www.castleproject.org/
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Castle.Windsor.Tests
{
	using System;
	using System.Linq;

	using Castle.MicroKernel;
	using Castle.MicroKernel.Registration;

	using NUnit.Framework;

	[TestFixture]
	public class HandlerFilterTestCase
	{
		[Test]
		public void HandlerFilterGetsCalledLikeExpected()
		{
			var container = new WindsorContainer();

			container.Register(Component.For<ISomeService>().ImplementedBy<FirstImplementation>(),
			                   Component.For<ISomeService>().ImplementedBy<SecondImplementation>(),
			                   Component.For<ISomeService>().ImplementedBy<ThirdImplementation>());

			var filter = new TestHandlerFilter();
			container.Kernel.AddHandlerFilter(filter);

			var services = container.ResolveAll(typeof(ISomeService));

			Assert.IsTrue(filter.OpinionWasChecked, "Filter's opinion should have been checked once for each handler");
		}

		[Test]
		public void HandlerFiltersPrioritizationAndOrderingIsRespected()
		{
			var container = new WindsorContainer();

			container.Register(Component.For<ISomeTask>().ImplementedBy<Task5>(),
			                   Component.For<ISomeTask>().ImplementedBy<Task3>(),
			                   Component.For<ISomeTask>().ImplementedBy<Task2>(),
			                   Component.For<ISomeTask>().ImplementedBy<Task4>(),
			                   Component.For<ISomeTask>().ImplementedBy<Task1>());

			container.Kernel.AddHandlerFilter(new FilterThatRemovedFourthTaskAndOrdersTheRest());

			var instances = container.ResolveAll(typeof(ISomeTask));

			Assert.That(instances, Has.Length.EqualTo(4));
		}

		[Test]
		public void SelectionMethodIsNeverCalledOnFilterWhenItDoesNotHaveAnOpinionForThatService()
		{
			var container = new WindsorContainer();

			container.Register(Component.For<IUnimportantService>().ImplementedBy<UnimportantImpl>());

			container.Kernel.AddHandlerFilter(new FailIfCalled());

			container.ResolveAll(typeof(IUnimportantService));
		}

		private class FailIfCalled : IHandlerFilter
		{
			public bool HasOpinionAbout(Type service)
			{
				return false;
			}

			public IHandler[] SelectHandlers(Type service, IHandler[] handlers)
			{
				Assert.Fail("SelectHandlers was called with {0}", service);
				return null; //< could not compile without returning anything
			}
		}

		private class FilterThatRemovedFourthTaskAndOrdersTheRest : IHandlerFilter
		{
			public bool HasOpinionAbout(Type service)
			{
				return service == typeof(ISomeTask);
			}

			public IHandler[] SelectHandlers(Type service, IHandler[] handlers)
			{
				return handlers
					.Where(h => h.ComponentModel.Implementation != typeof(Task4))
					.OrderBy(h => h.ComponentModel.Implementation.Name)
					.ToArray();
			}
		}

		private class FirstImplementation : ISomeService
		{
		}

		private interface ISomeService
		{
		}

		private interface ISomeTask
		{
		}

		private interface IUnimportantService
		{
		}

		private class SecondImplementation : ISomeService
		{
		}

		private class Task1 : ISomeTask
		{
		}

		private class Task2 : ISomeTask
		{
		}

		private class Task3 : ISomeTask
		{
		}

		private class Task4 : ISomeTask
		{
		}

		private class Task5 : ISomeTask
		{
		}

		private class TestHandlerFilter : IHandlerFilter
		{
			public bool OpinionWasChecked { get; set; }

			public bool HasOpinionAbout(Type service)
			{
				Assert.That(OpinionWasChecked, Is.False, "Opinion should not be checked more than once");

				var wasExpectedService = service == typeof(ISomeService);
				Assert.That(wasExpectedService, Is.True, "Did not expect {0} to be checked with this handler filter");

				OpinionWasChecked = true;

				return wasExpectedService;
			}

			public IHandler[] SelectHandlers(Type service, IHandler[] handlers)
			{
				return handlers;
			}
		}

		private class ThirdImplementation : ISomeService
		{
		}

		private class UnimportantImpl : IUnimportantService
		{
		}
	}
}