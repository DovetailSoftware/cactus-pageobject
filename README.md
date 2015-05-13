# cactus-pageobject
.NET/C# PageObject framework for Cactus/Selenium

There are many Webdriver based frameworks.  The key is to start with the ones that are right for your work environment.  This will save you time and money for your business.
What we have tried to do here with Cactus is show a slimmed down, working version of what we use at Dovetail for our UI/UX automation testing needs. 
Within this codeset is Nunit, Selenium, a few logging examples, and the ability for you to hook up what you need beyond that.
For instance, with only a couple lines of code you can install log4net into this code base.

The key to this framework is the PageObject pattern you will find on the Selenium's homepage. 
If you take nothing else from reviewing this framework, it is the extra functionality built on top of the Selenium base.
Multiple kinds of waits, PageObject style Asserts, easily jump between iFrames and Tabs, enable more readable code through the Control class.   

If you have ever got frustrated with trying to do something simple in Selenium and scoured the web to find the code chunks, you might take a look at our Control.cs file.  In that class file is a gold mine of hard to find functionality.



Examples of test cases and how to build them are in the Solution file. 

To get started.  
  Download the code, convert to whatever Visual Studio version you have.
  Restore the Nuget Files to your solution. 
  Go to the TestCases folder and run your choice of tests in either Testrunner or Resharper.
  
