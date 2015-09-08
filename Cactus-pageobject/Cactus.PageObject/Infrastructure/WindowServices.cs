using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;

namespace Cactus.Infrastructure
{
    public static class WindowsService
    {
        public static void Stop(params string[] serviceNames)
        {
            OperateServices(serviceNames, s => s.Stop(), ServiceControllerStatus.Stopped);
        }

        public static void Start(params string[] serviceNames)
        {
            OperateServices(serviceNames, s => s.Start(), ServiceControllerStatus.Running);
        }

        public static void ReStart(params string[] serviceNames)
        {
            OperateServices(serviceNames, s => s.Stop(), ServiceControllerStatus.Stopped);
            OperateServices(serviceNames, s => s.Start(), ServiceControllerStatus.Running);
        }

        private static void OperateServices(IEnumerable<string> serviceNames, Action<ServiceController> action, ServiceControllerStatus checkStatus)
        {
            var services = new List<ServiceController>();
            foreach (var name in serviceNames)
            {
                var service = new ServiceController(name);
                services.Add(service);

                if (service.Status != checkStatus)
                {
                    action(service);
                }
            }

            foreach (var service in services)
            {
                service.WaitForStatus(checkStatus, TimeSpan.FromSeconds(30));
                if (service.Status == checkStatus) continue;

                action(service);
                service.WaitForStatus(checkStatus, TimeSpan.FromSeconds(30));
            }

        }

        public static string Status(string serviceName)
        {
            ServiceController service = new ServiceController(serviceName);
            return service.Status.ToString();
        }

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
