using System;
using System.Diagnostics;
using System.Linq;

namespace Cactus.Infrastructure
{
    public class ProcessExtensions
    {
        public static bool IsRunning(string processName)
        {

            Process[] pname = Process.GetProcessesByName(processName);
            // only return the first process info
            if (pname == null || pname.Count() == 0)
                return false;
            try
            {
                Process.GetProcessById(pname[0].Id);
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
            return true;
        }
    }
}
