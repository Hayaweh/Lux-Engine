using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lux.Core
{
    public class Logger
    {
        private string m_category = "";

        public Logger(string category)
        {
            m_category = category;
        }

        public void Write(string message, params object[] args)
        {
            Console.Write("[" + DateTime.UtcNow + "] " + m_category + ": " + message, args);
        }

        public void Write(string message)
        {
            Write(message, null);
        }

        public void WriteLine(string message, params object[] args)
        {
            Console.WriteLine("[" + DateTime.UtcNow + "] " + m_category + ": " + message, args);
        }

        public void WriteLine(string message)
        {
            WriteLine(message, null);
        }
    }
}