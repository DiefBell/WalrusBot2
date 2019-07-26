using System;
using System.Diagnostics;

namespace WalrusProcessMonitor
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] user = System.Security.Principal.WindowsIdentity.GetCurrent().Name.Split('\\');
            string userName = user[user.Length - 1];
            Console.WriteLine(userName);
            Console.Read();
        }
    }
}
