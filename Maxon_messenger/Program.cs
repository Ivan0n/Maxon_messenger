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
        private const string ChatFile = "chat_history.txt";
        private const string UploadsDir = "web/uploads";
        private const long MaxUploadBytes = 10 * 1024 * 1024; // 10 МБ

        private static readonly List<Message> Messages = new List<Message>();
        private static int _nextId = 1;
        private static readonly object Lock = new object();

        static void Main(string[] args)
        {
            if (!Directory.Exists(WebRoot))
                Directory.CreateDirectory(WebRoot);

            if (!Directory.Exists(UploadsDir))
                Directory.CreateDirectory(UploadsDir);

            LoadHistory();

            _listener = new HttpListener();
            _listener.Prefixes.Add(Url);
            _listener.Start();

            Console.WriteLine($"Сервер запущен: {Url}");
            Console.WriteLine($"Корневая папка:  {Path.GetFullPath(WebRoot)}");
            Console.WriteLine($"Папка загрузок:  {Path.GetFullPath(UploadsDir)}");
            Console.WriteLine($"Файл истории:    {Path.GetFullPath(ChatFile)}");
            Console.WriteLine($"Загружено сообщений: {Messages.Count}");

            while (true)
            {
                HttpListenerContext context = _listener.GetContext();
                ThreadPool.QueueUserWorkItem(_ => HandleRequest(context));
            }
        }

        // ========== Загрузка истории ==========
        private static void LoadHistory()
        {
            if (!File.Exists(ChatFile))
            {
                Console.WriteLine("Файл истории не найден, начинаем с чистого чата.");
                return;
            }

            try
            {
                string[] lines = File.ReadAllLines(ChatFile, Encoding.UTF8);
                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    string[] parts = line.Split(new[] { '|' }, 4);
                    if (parts.Length < 4)
                        continue;

                    int id;
                    if (!int.TryParse(parts[0], out id))
                        continue;

                    Messages.Add(new Message
                    {
                        Id = id,
                        User = parts[1],
                        Time = parts[2],
                        Text = parts[3].Replace("\\n", "\n")
                    });

                    if (id >= _nextId)
                        _nextId = id + 1;
                }

                Console.WriteLine($"Загружено {Messages.Count} сообщений из {ChatFile}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки истории: {ex.Message}");
            }
        }

        // ========== Сохранение сообщения ==========
        private static void SaveMessageToFile(Message msg)
        {
            try
            {
                string safeText = msg.Text.Replace("\n", "\\n").Replace("\r", "");
                string line = $"{msg.Id}|{msg.User}|{msg.Time}|{safeText}";
                File.AppendAllText(ChatFile, line + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения: {ex.Message}");
            }
        }

        // ========== Маршрутизация ==========
        private static void HandleRequest(HttpListenerContext context)
        {
            try
            {
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;
                string path = request.Url.AbsolutePath;

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {request.HttpMethod} {path}");

                // API: отправить сообщение
                if (path == "/api/send" && request.HttpMethod == "POST")
                {
                    HandleSend(request, response);
                    return;
                }

                // API: получить сообщения
                if (path == "/api/messages" && request.HttpMethod == "GET")
                {
                    HandleGetMessages(request, response);
                    return;
                }

                // API: загрузить картинку
                if (path == "/api/upload" && request.HttpMethod == "POST")
                {
                    HandleUpload(request, response);
                    return;
                }

                // Статические файлы
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
                    byte[] err = Encoding.UTF8.GetBytes("<h1>404</h1>");
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

        // ========== Приём сообщения ==========
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
                    var msg = new Message
                    {
                        Id = _nextId++,
                        User = user,
                        Text = text,
                        Time = DateTime.Now.ToString("HH:mm")
                    };

                    Messages.Add(msg);
                    SaveMessageToFile(msg);

                    if (text.StartsWith("[IMG]"))
                        Console.WriteLine($"  🖼 {user}: (картинка)");
                    else
                        Console.WriteLine($"  💬 {user}: {text}");
                }
            }

            SendJson(response, "{\"ok\":true}");
        }

        // ========== Загрузка картинки ==========
        private static void HandleUpload(HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                // Проверка размера
                if (request.ContentLength64 > MaxUploadBytes)
                {
                    SendJson(response, "{\"error\":\"Файл слишком большой (макс. 10 МБ)\"}");
                    return;
                }

                string body;
                using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    body = reader.ReadToEnd();

                string name = ExtractJsonValue(body, "name");
                string data = ExtractJsonValue(body, "data");

                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(data))
                {
                    SendJson(response, "{\"error\":\"Нет данных\"}");
                    return;
                }

                // Проверка расширения
                string ext = Path.GetExtension(name).ToLower();
                if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" &&
                    ext != ".gif" && ext != ".webp" && ext != ".bmp")
                {
                    SendJson(response, "{\"error\":\"Недопустимый формат. Разрешены: jpg, png, gif, webp, bmp\"}");
                    return;
                }

                // Декодируем Base64
                byte[] fileBytes;
                try
                {
                    fileBytes = Convert.FromBase64String(data);
                }
                catch
                {
                    SendJson(response, "{\"error\":\"Ошибка декодирования\"}");
                    return;
                }

                // Генерируем уникальное имя
                string fileName = Guid.NewGuid().ToString("N") + ext;
                string filePath = Path.Combine(UploadsDir, fileName);

                // Сохраняем файл
                File.WriteAllBytes(filePath, fileBytes);

                string url = "/uploads/" + fileName;

                Console.WriteLine($"  📁 Загружен: {fileName} ({fileBytes.Length} байт)");

                SendJson(response, "{\"url\":\"" + url + "\"}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Ошибка загрузки: {ex.Message}");
                SendJson(response, "{\"error\":\"Ошибка сервера\"}");
            }
        }

        // ========== Отдача сообщений ==========
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

        // ========== Утилиты ==========

        private static void SendJson(HttpListenerResponse response, string json)
        {
            byte[] data = Encoding.UTF8.GetBytes(json);
            response.ContentType = "application/json; charset=utf-8";
            response.StatusCode = 200;
            response.ContentLength64 = data.Length;
            response.AddHeader("Cache-Control", "no-cache");
            using (Stream output = response.OutputStream)
                output.Write(data, 0, data.Length);
        }

        private static string ExtractJsonValue(string json, string key)
        {
            string pattern = "\"" + key + "\"";
            int keyIdx = json.IndexOf(pattern);
            if (keyIdx < 0) return null;

            int colon = json.IndexOf(':', keyIdx + pattern.Length);
            if (colon < 0) return null;

            int qStart = json.IndexOf('"', colon + 1);
            if (qStart < 0) return null;

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
                case ".webp": return "image/webp";
                case ".bmp":  return "image/bmp";
                case ".svg":  return "image/svg+xml";
                case ".ico":  return "image/x-icon";
                case ".json": return "application/json";
                default:      return "application/octet-stream";
            }
        }
    }

    class Message
    {
        public int Id;
        public string User;
        public string Text;
        public string Time;
    }
}