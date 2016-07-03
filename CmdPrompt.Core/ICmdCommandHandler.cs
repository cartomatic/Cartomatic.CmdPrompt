using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cartomatic.CmdPrompt.Core
{
    public interface ICmdCommandHandler
    {
        /// <summary>
        /// Prints handler specific info; used to give some init info like cmd version and stuff
        /// </summary>
        void PrintHandlerInfo();

        /// <summary>
        /// Handles a cmd command
        /// </summary>
        /// <param name="command"></param>
        void HandleCommand(string command);

        /// <summary>
        /// Indicates whether the state of the exit flag; client should exit if true
        /// </summary>
        /// <returns></returns>
        bool Exit();
    }
}
