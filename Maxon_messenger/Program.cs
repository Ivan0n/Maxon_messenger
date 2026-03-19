using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace Maxon_messenger
{
    internal class Program
    {
        private static HttpListener _listener;
        private const string Url = "http://localhost:2222/";
        private const string WebRoot = "web";

        // Хранилище сообщений
        private static readonly List<Message> Messages = new List<Message>();
        private static int _nextId = 1;
        private static readonly object Lock = new object();

        static void Main(string[] args)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(Url);
            _listener.Start();

            Console.WriteLine($"Сервер запущен: {Url}");
            Console.WriteLine($"Корневая папка: {Path.GetFullPath(WebRoot)}");

            while (true)
            {
                HttpListenerContext context = _listener.GetContext();
                ThreadPool.QueueUserWorkItem(_ => HandleRequest(context));
            }
        }

        private static void HandleRequest(HttpListenerContext context)
        {
            try
            {
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;
                string path = request.Url.AbsolutePath;

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {request.HttpMethod} {path}");

                // ===== API: отправить сообщение =====
                if (path == "/api/send" && request.HttpMethod == "POST")
                {
                    HandleSend(request, response);
                    return;
                }

                // ===== API: получить сообщения =====
                if (path == "/api/messages" && request.HttpMethod == "GET")
                {
                    HandleGetMessages(request, response);
                    return;
                }
                
                if (path == "/")
                    path = "/index.html";

                string filePath = Path.Combine(WebRoot, path.TrimStart('/'));

                if (File.Exists(filePath))
                {
                    byte[] fileBytes = File.ReadAllBytes(filePath);
                    response.ContentType = GetContentType(filePath);
                    response.ContentLength64 = fileBytes.Length;
                    response.StatusCode = 200;
                    using (Stream output = response.OutputStream)
                        output.Write(fileBytes, 0, fileBytes.Length);
                }
                else
                {
                    byte[] err = Encoding.UTF8.GetBytes("<h1>404 — Файл не найден</h1>");
                    response.ContentType = "text/html; charset=utf-8";
                    response.StatusCode = 404;
                    response.ContentLength64 = err.Length;
                    using (Stream output = response.OutputStream)
                        output.Write(err, 0, err.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Ошибка: {ex.Message}");
            }
        }

        // Приём сообщения
        private static void HandleSend(HttpListenerRequest request, HttpListenerResponse response)
        {
            string body;
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                body = reader.ReadToEnd();

            string user = ExtractJsonValue(body, "user");
            string text = ExtractJsonValue(body, "text");

            if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(user))
            {
                lock (Lock)
                {
                    Messages.Add(new Message
                    {
                        Id = _nextId++,
                        User = user,
                        Text = text,
                        Time = DateTime.Now.ToString("HH:mm")
                    });

                    Console.WriteLine($"  💬 {user}: {text}");
                }
            }

            SendJson(response, "{\"ok\":true}");
        }

        //Отдача сообщений
        private static void HandleGetMessages(HttpListenerRequest request, HttpListenerResponse response)
        {
            string afterStr = request.QueryString["after"];
            int after = 0;
            if (!string.IsNullOrEmpty(afterStr))
                int.TryParse(afterStr, out after);

            var sb = new StringBuilder();
            sb.Append("[");

            lock (Lock)
            {
                bool first = true;
                for (int i = 0; i < Messages.Count; i++)
                {
                    Message msg = Messages[i];
                    if (msg.Id <= after) continue;

                    if (!first) sb.Append(",");
                    sb.Append("{");
                    sb.AppendFormat("\"id\":{0},", msg.Id);
                    sb.AppendFormat("\"user\":\"{0}\",", EscapeJson(msg.User));
                    sb.AppendFormat("\"text\":\"{0}\",", EscapeJson(msg.Text));
                    sb.AppendFormat("\"time\":\"{0}\"", msg.Time);
                    sb.Append("}");
                    first = false;
                }
            }

            sb.Append("]");
            SendJson(response, sb.ToString());
        }

        //Вспомогательные

        private static void SendJson(HttpListenerResponse response, string json)
        {
            byte[] data = Encoding.UTF8.GetBytes(json);
            response.ContentType = "application/json; charset=utf-8";
            response.StatusCode = 200;
            response.ContentLength64 = data.Length;
            // запрет кеширования
            response.AddHeader("Cache-Control", "no-cache");
            using (Stream output = response.OutputStream)
                output.Write(data, 0, data.Length);
        }

        /// <summary>
        /// Простейший парсер значения строки из JSON без библиотек.
        /// Ищет "key":"value" и возвращает value.
        /// </summary>
        private static string ExtractJsonValue(string json, string key)
        {
            string pattern = "\"" + key + "\"";
            int keyIdx = json.IndexOf(pattern);
            if (keyIdx < 0) return null;

            int colon = json.IndexOf(':', keyIdx + pattern.Length);
            if (colon < 0) return null;

            int qStart = json.IndexOf('"', colon + 1);
            if (qStart < 0) return null;

            // ищем закрывающую кавычку, пропуская экранированные
            int qEnd = qStart + 1;
            while (qEnd < json.Length)
            {
                if (json[qEnd] == '"' && json[qEnd - 1] != '\\')
                    break;
                qEnd++;
            }

            if (qEnd >= json.Length) return null;
            return json.Substring(qStart + 1, qEnd - qStart - 1)
                       .Replace("\\\"", "\"")
                       .Replace("\\\\", "\\")
                       .Replace("\\n", "\n");
        }

        private static string EscapeJson(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t");
        }

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

    //САМ НАПИСАЛ!!!
    class Message
    {
        public int Id;
        public string User;
        public string Text;
        public string Time;
    }
}