using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CmdPrompt
{
    class Program
    {
        static void Main(string[] args)
        {
            var cmdWatcher = new Cartomatic.CmdPrompt.Core.CmdWatcher();

            //setup if needed
            cmdWatcher.Prompt = "CustomPrompt>";
            cmdWatcher.PromptColor = ConsoleColor.DarkCyan;

            
            //print handler info - by default it is on handler's discretion to print whatever seems suitable...
            cmdWatcher.Init(true);


            Console.ReadLine();
        }
    }
}
