using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization; // добавить ссылку System.Web.Extensions

namespace Maxon_messenger
{
    // Класс сообщения
    class ChatMessage
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Text { get; set; }
        public string Time { get; set; }
    }

    internal class Program
    {
        private static HttpListener _listener;
        private const string Url = "http://localhost:8080/";
        private const string WebRoot = "web";

        // ═══════════════════════════════════════════════════════
        //  Хранилище сообщений (в памяти)
        // ═══════════════════════════════════════════════════════
        private static readonly List<ChatMessage> _messages = new List<ChatMessage>();
        private static readonly object _lock = new object();
        private static int _nextId = 0;

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

            // ═══════════════════════════════════════════════════
            //  API: Получить сообщения (GET /api/messages?after=0)
            // ═══════════════════════════════════════════════════
            if (path == "/api/messages" && request.HttpMethod == "GET")
            {
                // Параметр after — ID, после которого отдавать
                string afterParam = request.QueryString["after"] ?? "0";
                int afterId = 0;
                int.TryParse(afterParam, out afterId);

                List<ChatMessage> result = new List<ChatMessage>();

                lock (_lock)
                {
                    foreach (var msg in _messages)
                    {
                        if (msg.Id > afterId)
                            result.Add(msg);
                    }
                }

                string json = new JavaScriptSerializer().Serialize(result);
                byte[] data = Encoding.UTF8.GetBytes(json);

                response.ContentType = "application/json; charset=utf-8";
                response.StatusCode = 200;

                // Разрешаем CORS (на всякий случай)
                response.Headers.Add("Access-Control-Allow-Origin", "*");

                response.ContentLength64 = data.Length;
                using (Stream output = response.OutputStream)
                {
                    output.Write(data, 0, data.Length);
                }
                return;
            }

            // ═══════════════════════════════════════════════════
            //  API: Отправить сообщение (POST /api/send)
            //  Тело: { "name": "Вася", "text": "Привет" }
            // ═══════════════════════════════════════════════════
            if (path == "/api/send" && request.HttpMethod == "POST")
            {
                string body;
                using (StreamReader reader = new StreamReader(request.InputStream, request.ContentEncoding))
                {
                    body = reader.ReadToEnd();
                }

                var serializer = new JavaScriptSerializer();
                var incoming = serializer.Deserialize<Dictionary<string, object>>(body);

                string name = incoming.ContainsKey("name") ? incoming["name"].ToString() : "аноним";
                string text = incoming.ContainsKey("text") ? incoming["text"].ToString() : "";

                if (!string.IsNullOrWhiteSpace(text))
                {
                    lock (_lock)
                    {
                        _nextId++;
                        _messages.Add(new ChatMessage
                        {
                            Id = _nextId,
                            Name = name,
                            Text = text,
                            Time = DateTime.Now.ToString("HH:mm")
                        });
                    }

                    Console.WriteLine($"  💬 {name}: {text}");
                }

                // Ответ OK
                byte[] ok = Encoding.UTF8.GetBytes("{\"status\":\"ok\"}");
                response.ContentType = "application/json; charset=utf-8";
                response.StatusCode = 200;
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.ContentLength64 = ok.Length;

                using (Stream output = response.OutputStream)
                {
                    output.Write(ok, 0, ok.Length);
                }
                return;
            }

            // ═══════════════════════════════════════════════════
            //  API: Онлайн-счётчик (GET /api/online)
            //  (простой вариант — по кол-ву уникальных IP за минуту)
            // ═══════════════════════════════════════════════════

            // ═══════════════════════════════════════════════════
            //  Статические файлы
            // ═══════════════════════════════════════════════════
            if (path == "/")
                path = "/index.html";

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

        private static string GetContentType(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            switch (ext)
            {
                case ".html": return "text/html; charset=utf-8";
                case ".css": return "text/css; charset=utf-8";
                case ".js": return "application/javascript; charset=utf-8";
                case ".png": return "image/png";
                case ".jpg": return "image/jpeg";
                case ".jpeg": return "image/jpeg";
                case ".gif": return "image/gif";
                case ".svg": return "image/svg+xml";
                case ".ico": return "image/x-icon";
                case ".json": return "application/json";
                default: return "application/octet-stream";
            }
        }
    }
}