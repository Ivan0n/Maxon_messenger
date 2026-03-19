using System;
using System.IO;
using System.Net;
using System.Text;

namespace Maxon_messenger
{
    internal class Program
    {
        private static HttpListener _listener;
        private const string Url = "http://localhost:8080/";
        private const string WebRoot = "web"; // папка с файлами

        static void Main(string[] args)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(Url);
            _listener.Start();

            Console.WriteLine($"Сервер запущен: {Url}");
            Console.WriteLine($"Корневая папка: {Path.GetFullPath(WebRoot)}");
            Console.WriteLine("Нажмите Ctrl+C для остановки.\n");

            while (true)
            {
                HttpListenerContext context = _listener.GetContext();
                HandleRequest(context);
            }
        }

        private static void HandleRequest(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            string path = request.Url.AbsolutePath;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {request.HttpMethod} {path}");

            // Если запрос "/" — отдаём index.html
            if (path == "/")
                path = "/index.html";

            // Собираем путь к файлу: web/index.html, web/style.css и т.д.
            string filePath = Path.Combine(WebRoot, path.TrimStart('/'));

            if (File.Exists(filePath))
            {
                byte[] fileBytes = File.ReadAllBytes(filePath);
                response.ContentType = GetContentType(filePath);
                response.ContentLength64 = fileBytes.Length;
                response.StatusCode = (int)HttpStatusCode.OK;

                using (Stream output = response.OutputStream)
                {
                    output.Write(fileBytes, 0, fileBytes.Length);
                }

                Console.WriteLine($"  → 200 OK | {filePath} ({fileBytes.Length} байт)");
            }
            else
            {
                // Файл не найден — 404
                string errorHtml = "<h1>404 — Файл не найден</h1>";
                byte[] errorBytes = Encoding.UTF8.GetBytes(errorHtml);
                response.ContentType = "text/html; charset=utf-8";
                response.ContentLength64 = errorBytes.Length;
                response.StatusCode = (int)HttpStatusCode.NotFound;

                using (Stream output = response.OutputStream)
                {
                    output.Write(errorBytes, 0, errorBytes.Length);
                }

                Console.WriteLine($"  → 404 Not Found | {filePath}");
            }
        }

        // Определяем Content-Type по расширению файла
        private static string GetContentType(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            switch (ext)
            {
                case ".html": return "text/html; charset=utf-8";
                case ".css":  return "text/css; charset=utf-8";
                case ".js":   return "application/javascript; charset=utf-8";
                case ".png":  return "image/png";
                case ".jpg":  return "image/jpeg";
                case ".jpeg": return "image/jpeg";
                case ".gif":  return "image/gif";
                case ".svg":  return "image/svg+xml";
                case ".ico":  return "image/x-icon";
                case ".json": return "application/json";
                default:      return "application/octet-stream";
            }
        }
    }
}