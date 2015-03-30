# Specify

## What is it?
Specify is a .Net context-specification testing library that builds on top of BDDfy from [TestStack](http://teststack.net/). While BDDfy is primarily intended for BDD testing, it is beautifully designed to be very easy to customize and extend.

When I first started using BDDfy for acceptance testing, I would use a different framework for unit testing, but I didn't like the context switching between different frameworks, syntaxes and testing styles. Specify provides a base fixture which gives a consistent experience for all types of tests (or specifications). Why not have the fantastic BDDfy reports for all of your different test types?

[![Build status](https://ci.appveyor.com/api/projects/status/vj6ec2yubg8ii9sn?svg=true)](https://ci.appveyor.com/project/mwhelan/specify)

## Overview of Features
* Tests use a context-specification style, with a class per scenario.
* SUT factory with pluggable automocking or Ioc containers.
* Tests can be resolved from your IoC container. Or use the built-in TinyIoc container.
* BDDfy Reports are produced for all of your test types

## Context-Specification Style
With context-specification you have a class per scenario, with each step having its own method and state being shared between methods in fields. This means that the setup and execution only happen once (the context), and then each `Then` method is a specification that asserts against the result of the execution.

Specify provides two generic base classes that your test class can inherit from:
 
* `SpecificationFor<TSut>` is for unit and integration tests and would normally be used with an automocking container. Reports show *Specifications For: [SUT Name]* instead of a user story.
* `SpecificationFor<TSut, TStory>` is for the typical BDDfy user story tests and would normally be used with an Inversion of Control container. Reports show the user story or business value story.

These classes follow the `Given When Then` syntax (though there is nothing to stop you from [customizing BDDfy](http://www.michael-whelan.net/roll-your-own-testing-framework/) to use a different syntax if you want). 
 
    public class DetailsForExistingStudent : SpecificationFor<StudentController>
    {
        ViewResult _result;
        private Student _student = new Student { ID = 1 };

        public void Given_an_existing_student()
        {
            Get<ISchoolRepository>()
                .FindStudentById(_student.ID)
                .Returns(_student);
        }

        public void When_the_details_are_requested_for_that_Student()
        {
            _result = SUT.Details(_student.ID) as ViewResult;
        }

        public void Then_the_details_view_is_displayed()
        {
            _result.ViewName.Should().Be(string.Empty);
        }

        public void AndThen_the_details_are_of_the_requested_student()
        {
            _result.Model.Should().Be(_student);
        }
    }


### System Under Test
The `TSut` type parameter represents the [System Under Test](http://xunitpatterns.com/SUT.html), which is the class that is being tested. Each class has a `SUT` property, which is instantiated for you by the automocking or IoC container. You can override this too, if you want.

### User Story or Value Story
The `TStory` type parameter represents the user story. Specify provides two Story classes, but BDDfy lets you create additional formats if you wish:

* User Story: The original user story, in the form `As a <type of user> I want <some functionality> so that <some benefit>`.
* [Value Story](http://www.infoq.com/news/2008/06/new-user-story-format): The business value story, with the emphasis on the business value. `In order to <achieve some value>, as a <type of user>, I want <some functionality>`.

## SUT Factory
Specify can create the SUT for you, using either an automocking container or an IoC container as a [SUT factory](http://blog.ploeh.dk/2009/02/13/SUTFactory/). This is configurable, on a per test assembly basis. A fresh new container is provided for each specification, so you can change the configuration for each test without impacting other tests. Most IoC containers provide this child container functionality (though they might call it by different names).

The Specification classes allow you to interact with the `SutFactory` via a `Container` property. The Get methods allow you to retrieve SUT dependencies. The Register methods allow you to provide implementations that will be used in the creation of the SUT. You can also set the SUT directly if you need to override the creation for a particular scenario. The SUT is lazily created the first time it is requested, so registering types and setting the SUT need to happen before the first request to the SUT property.

## Test Lifecycle
Specify uses BDDfy's Reflective API to scan its classes for methods. By default, BDDfy recognises the standard BDD methods, as well as Setup and TearDown. You can read more about them [here](http://www.mehdi-khalili.com/bddify-in-action/method-name-conventions) and you can always [customize them](http://www.michael-whelan.net/roll-your-own-testing-framework/) if you have your own preferences by creating a new BDDfy `MethodNameStepScanner`. The method name:

* Ending with `Context` is considered as a setup method (not reported).
* `Setup` is considered as as setup method (not reported).
* Starting with `Given` is considered as a setup method (reported).
* Starting with `AndGiven` is considered as a setup method that runs after Context, Setup and Given steps (reported).
* Starting with `When` is considered as a transition method (reported).
* Starting with `AndWhen` is considered as a transition method that runs after When steps (reported).
* Starting with `Then` is considered as an asserting method (reported).
* Starting with `And` is considered as an asserting method (reported).
* Starting with `TearDown` is considered as a finally method which is run after all the other steps (not reported).
 
### Auto Mocking and IoC Adapters
To use a particular mocking framework or IoC container you just have to implement the Specify IContainer interface. I have created a number of adapters for some of the popular mocking frameworks and IoC containers in the [Specify.Examples project](https://github.com/mwhelan/Specify/tree/master/src/Specify.Examples). I'm not creating NuGet packages at this stage as it would be a bit of a maintenance headache, though this might change. 

At the moment, I've written the following adapters:

* Autofac
* Autofac NSubstitute
* Autofac FakeItEasy
* Autofac Moq

The containers are largely based on [Chill's containers](https://github.com/Erwinvandervalk/Chill), by [Erwin van der Valk](http://www.erwinvandervalk.net/). Chill is a great framework, which I recommend you check out.

## Running the tests
One of the things I've always liked about this approach is not having test framework attributes, such as [Test] and [Fact] on all of my test methods. Once you put these attributes on the base class the inheriting classes don't need them - all the main test frameworks are clever enough to discover them on base classes. This poses a bit of a challenge with a library like this. Thankfully, newer frameworks, like Fixie, don't force you to use attributes for test discoverability. Just use this Fixie convention (available in the Specify.Examples project) to run Specify tests:

	public class FixieSpecifyConvention : Convention
    {
        public FixieSpecifyConvention()
        {
            Classes
                .Where(type => type.IsSpecificationFor() || type.IsScenarioFor());

            Methods
                .Where(method => method.Name == "Specify");
        }
    }

Unfortunately, even the newer versions of xUnit and NUnit seem to still require attributes (please let me know if I'm wrong on this score). Again, I've gone for the copy/paste solution. Just create a base class to extend the two Specify classes. Here is an example with NUnit. I've adopted the convention of prefixing the class with the letter of the test framework so as to remove any ambiguity between the two:

 	[TestFixture]
    public abstract class NSpecificationFor<TSut, TStory> : Specify.SpecificationFor<TSut, TStory>
        where TSut : class
        where TStory : Story, new()
    {
        [Test]
        public override void Specify()
        {
            base.Specify();
        }
    }

    [TestFixture]
    public abstract class NSpecificationFor<TSut> : Specify.SpecificationFor<TSut> 
        where TSut : class
    {
        [Test]
        public override void Specify()
        {
            base.Specify();
        }
    }

## Configuration
Just create a class in your test assembly that inherits from the SpecifyConfiguration class. The main thing to configure is the container that the SUT factory will use:

    public class SpecifyConfig : SpecifyConfiguration
    {
        public override IContainer GetSpecifyContainer()
        {
            return new AutofacNSubstituteContainer();
        }
    }

## Unit Tests
You can read more about the unit testing approach [here](http://www.michael-whelan.net/using-bddfy-for-unit-tests/):
