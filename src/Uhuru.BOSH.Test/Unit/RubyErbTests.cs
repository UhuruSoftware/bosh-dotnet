using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IronRuby;
using System.IO;
using Uhuru.BOSH.Agent.Ruby;

namespace Uhuru.BOSH.Test.Unit
{
    [TestClass]
    public class RubyErbTests
    {
        [TestMethod]
        //[DeploymentItem(@"Ruby\erb.rb")]
        [DeploymentItem(@"Ruby\erb.rb", @"Ruby")]
        public void TC001_TestRuby()
        {
            //Arrange
            var engine = Ruby.CreateEngine();
            
            //Act
            dynamic result = engine.Execute(" 'test' ");

            //Assert
            Assert.AreEqual(result.ToString(), "test");
        }

        [TestMethod]
        public void TC002_TestRubyVars()
        {
            //Arrange
            var engine = Ruby.CreateEngine();
            var scope = engine.CreateScope();
            scope.SetVariable("myVar", "test");

            //Act
            dynamic result = engine.Execute(" myVar ", scope);

            //Assert
            Assert.AreEqual(result.ToString(), "test");
        }

        [TestMethod]
        public void TC003_TestERB()
        {
            //Arrange
            string template = "<%= x %>";

            var engine = Ruby.CreateEngine();
            var scope = engine.CreateScope();
            scope.SetVariable("templateText", template);
            
            //Act
            dynamic result = engine.Execute(@"
            require 'Ruby\erb.rb'
    
            x = 'test'
            
            template = ERB.new(templateText.to_s)
            res = template.result(binding)
            ", scope);

            //Assert
            Assert.AreEqual(result.ToString().Trim(), "test");
        }

        [TestMethod]
        [DeploymentItem(@"Ruby", @"Ruby")]
        [DeploymentItem(@"Resources\WinServBosh.exe.config.erb")]
        public void TC004_TestERBWithFile()
        {
            //Arrange
            ErbTemplate erbTemplate = new ErbTemplate();
            string currentProp = @"{boshtest: {outputfile: ""c:\\test.txt"",testmessage: ""this is a test""}}";

            //Act
            string result = erbTemplate.Execute("WinServBosh.exe.config.erb", currentProp);
            
            //Assert
            Assert.AreNotEqual(result, string.Empty);
        }
    }
}
