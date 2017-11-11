using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace R6S_ESP
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (args.Length > 0)
            {
                Stuff.Data.GameHandle = new IntPtr(Convert.ToInt64(args[0]));
                Application.Run(new Menu(true));
            }
            else
                Application.Run(new Menu(false));
        }
    }
}
