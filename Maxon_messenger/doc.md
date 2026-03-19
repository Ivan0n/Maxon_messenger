<div align="center">

<br>

# 💬 Maxon Messenger

<br>

**Минималистичный групповой веб-чат на чистом C# без единой внешней библиотеки**

<br>

[![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)](https://docs.microsoft.com/dotnet/csharp/)
[![.NET](https://img.shields.io/badge/.NET_Framework_4.5+-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![HTML5](https://img.shields.io/badge/HTML5-E34F26?style=for-the-badge&logo=html5&logoColor=white)](https://developer.mozilla.org/docs/Web/HTML)
[![JavaScript](https://img.shields.io/badge/Vanilla_JS-F7DF1E?style=for-the-badge&logo=javascript&logoColor=black)](https://developer.mozilla.org/docs/Web/JavaScript)
[![License](https://img.shields.io/badge/License-MIT-blue?style=for-the-badge)](LICENSE)
[![Status](https://img.shields.io/badge/Status-Active-brightgreen?style=for-the-badge)]()

<br>

> *Один файл сервера · Один файл клиента · Ноль зависимостей · Полноценный групповой чат*

<br>

[🚀 Быстрый старт](#-быстрый-старт) ·
[✨ Возможности](#-возможности) ·
[🏗 Архитектура](#-архитектура) ·
[📡 API](#-api) ·
[⚙️ Настройка](#%EF%B8%8F-настройка) ·
[❓ FAQ](#-faq)

<br>

---

</div>

<br>

## 📋 Оглавление

- [🔍 О проекте](#-о-проекте)
- [✨ Возможности](#-возможности)
- [🛠 Технологический стек](#-технологический-стек)
- [🚀 Быстрый старт](#-быстрый-старт)
- [📁 Структура проекта](#-структура-проекта)
- [🏗 Архитектура](#-архитектура)
- [📡 API](#-api)
- [💾 Формат хранения данных](#-формат-хранения-данных)
- [⚙️ Настройка](#%EF%B8%8F-настройка)
- [🔒 Безопасность](#-безопасность)
- [⚠️ Известные ограничения](#%EF%B8%8F-известные-ограничения)
- [❓ FAQ](#-faq)
- [📄 Лицензия](#-лицензия)

<br>

---

<br>

## 🔍 О проекте

**Maxon Messenger** — полностью автономный групповой веб-чат, написанный на чистом C#.
Серверная часть построена на встроенном классе `System.Net.HttpListener`, клиентская —
единственный HTML-файл с инлайновыми CSS-стилями и JavaScript-кодом.

Проект **не использует ни одной внешней библиотеки** — ни на сервере, ни на клиенте.

### Ключевые принципы
✦ Простота — минимум кода, максимум понятности
✦ Автономность — ноль внешних зависимостей
✦ Функциональность — полноценный чат с сохранением истории
✦ Читаемость — код как документация


<br>

---

<br>

## ✨ Возможности

<table>
<tr>
<td width="50" align="center">💬</td>
<td><b>Групповой чат в реальном времени</b><br>Все подключённые пользователи видят сообщения друг друга с задержкой не более 1 секунды</td>
</tr>
<tr>
<td align="center">👤</td>
<td><b>Выбор имени при входе</b><br>Пользователь вводит своё имя на экране входа — оно отображается у всех собеседников</td>
</tr>
<tr>
<td align="center">💾</td>
<td><b>Персистентное хранение</b><br>Все сообщения автоматически сохраняются в текстовый файл <code>chat_history.txt</code></td>
</tr>
<tr>
<td align="center">🔄</td>
<td><b>Восстановление при перезапуске</b><br>При старте сервера вся история загружается из файла — ни одно сообщение не теряется</td>
</tr>
<tr>
<td align="center">⚡</td>
<td><b>Инкрементальный polling</b><br>Клиент запрашивает только новые сообщения через параметр <code>after=ID</code></td>
</tr>
<tr>
<td align="center">🧵</td>
<td><b>Многопоточная обработка</b><br>Каждый HTTP-запрос обрабатывается в отдельном потоке через <code>ThreadPool</code></td>
</tr>
<tr>
<td align="center">📁</td>
<td><b>Встроенный статический сервер</b><br>Раздача HTML, CSS, JS, изображений из папки <code>web/</code> с корректными MIME-типами</td>
</tr>
<tr>
<td align="center">🎨</td>
<td><b>Терминальный дизайн</b><br>Чёрно-белый минималистичный интерфейс с моноширинным шрифтом Courier New</td>
</tr>
<tr>
<td align="center">📦</td>
<td><b>Ноль зависимостей</b><br>Ни NuGet, ни npm, ни CDN — только стандартная библиотека .NET</td>
</tr>
</table>

<br>

---

<br>

## 🛠 Технологический стек

<table>
<tr>
<th align="center">Уровень</th>
<th align="center">Технология</th>
<th align="center">Назначение</th>
</tr>
<tr>
<td rowspan="4" align="center"><b>Сервер</b></td>
<td>C# / .NET Framework 4.5+</td>
<td>Язык и платформа</td>
</tr>
<tr>
<td><code>System.Net.HttpListener</code></td>
<td>HTTP-сервер</td>
</tr>
<tr>
<td><code>System.Threading.ThreadPool</code></td>
<td>Параллельная обработка запросов</td>
</tr>
<tr>
<td><code>System.IO.File</code></td>
<td>Чтение и запись истории чата</td>
</tr>
<tr>
<td rowspan="3" align="center"><b>Клиент</b></td>
<td>HTML5 + CSS3 (inline)</td>
<td>Разметка и стили интерфейса</td>
</tr>
<tr>
<td>Vanilla JavaScript</td>
<td>Логика чата и взаимодействие с API</td>
</tr>
<tr>
<td>Fetch API</td>
<td>HTTP-запросы к серверу</td>
</tr>
<tr>
<td align="center"><b>Хранение</b></td>
<td>Plain text (<code>.txt</code>)</td>
<td>Файл с историей сообщений</td>
</tr>
</table>

<br>

---

<br>

## 🚀 Быстрый старт

### Требования

| Компонент | Версия | Примечание |
|:----------|:-------|:-----------|
| **ОС** | Windows 7+ | Или Windows Server 2008 R2+ |
| **.NET Framework** | 4.5+ | Или .NET 6.0+ SDK |
| **Свободный порт** | `2222` | Настраивается в коде |
| **Браузер** | Любой современный | Chrome, Firefox, Edge, Safari |

maxon-messenger/
│
├── 📄 Program.cs                  ← Сервер: HTTP, API, хранение
│   ├── Main()                        точка входа, запуск HttpListener
│   ├── LoadHistory()                 загрузка истории из файла
│   ├── HandleRequest()               маршрутизация запросов
│   ├── HandleSend()                   обработка POST /api/send
│   ├── HandleGetMessages()            обработка GET /api/messages
│   ├── SaveMessageToFile()            дозапись сообщения в файл
│   ├── ExtractJsonValue()             парсинг JSON без библиотек
│   ├── EscapeJson()                   экранирование строк для JSON
│   ├── SendJson()                     отправка JSON-ответа
│   └── GetContentType()               определение MIME-типа
│
├── 📂 web/                        ← Корневая папка веб-сервера
│   └── 📄 index.html                 клиент: UI + CSS + JS
│       ├── <style>                      инлайновые стили
│       ├── .login-screen                экран ввода имени
│       ├── .chat-container              окно чата
│       └── <script>                     polling, отправка, рендеринг
│
├── 📄 chat_history.txt            ← История сообщений (автосоздание)
│
└── 📄 README.md                   ← Документация