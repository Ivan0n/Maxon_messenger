using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Text;


namespace Maxon_messenger
{
    internal class Program
    {
        private static HttpListener _listener;
        private const string Url = "http://localhost:8080/";
        static void Main(string[] args)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(Url);
            _listener.Start();

            Console.WriteLine($"Сервер запущен: {Url}");
            Console.WriteLine("Нажмите Ctrl+C для остановки.\n");
            while (true)
            {
                HttpListenerContext context = _listener.GetContext();
                HandleRequest(context);
            }
        }
    }
}
