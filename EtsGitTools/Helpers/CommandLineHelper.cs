using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtsGitTools
{
    public class CommandLineHelper
    {
        public static Process ExecuteCommand(string Command)
        {
            ProcessStartInfo ProcessInfo;

            ProcessInfo = new ProcessStartInfo("cmd.exe", "/K " + Command);
            ProcessInfo.CreateNoWindow = true;
            ProcessInfo.UseShellExecute = false;
            ProcessInfo.RedirectStandardOutput = true;
            ProcessInfo.RedirectStandardError = true;
            var proc = Process.Start(ProcessInfo);
            return proc;
        }
    }
}
