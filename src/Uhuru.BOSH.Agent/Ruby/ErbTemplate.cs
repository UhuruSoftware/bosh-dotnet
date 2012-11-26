using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Scripting.Hosting;
using System.IO;

namespace Uhuru.BOSH.Agent.Ruby
{
    public class ErbTemplate
    {
        ScriptEngine engine;
        public ErbTemplate()
        {
            engine = IronRuby.Ruby.CreateEngine();
        }

        public string Execute(string templatePath, string binding)
        {
           
            ScriptScope currentScope = engine.CreateScope();
            string templateText = File.ReadAllText(templatePath);


            //foreach (var variable in vars)
            //{
            //    currentScope.SetVariable(variable.Key, variable.Value.ToString());
            //}
            currentScope.SetVariable("templateText", templateText);
            currentScope.SetVariable("currentspec", binding);
           //TODO Improve this
            
            dynamic result = engine.Execute(string.Format(@"
            require '{0}\Ruby\ostruct.rb'
            require '{0}\Ruby\erb.rb'      
            require '{0}\Ruby\ext.rb'
            
            spec = eval(currentspec.to_s).to_openstruct
            properties = spec.properties
            
            template = ERB.new(templateText.to_s)
            result = template.result(binding)
            result
            ", Path.GetDirectoryName(typeof(ScriptEngine).Assembly.Location)), currentScope);

            return result.ToString().Trim();
        }

    }
}
