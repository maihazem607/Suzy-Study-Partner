# 🎓 Suzy - Your Ultimate Study Companion

A productivity-focused web application designed to help students study more effectively through structured sessions, AI-driven insights, and collaborative tools. The platform offers flashcards, mock exams, to-do lists, study goals, and music integration — all in one place.

## 📹 Demo Video

https://github.com/user-attachments/assets/3364fb57-7e89-4a4a-85aa-8f1d68a838bf

---

## ✨ Features

### 🤖 AI-Powered Study Assistant (Suzy)

- **Personalized Analytics**: Get insights on your study patterns, focus levels, and productivity
- **Smart Conversations**: Interactive chat with AI for study guidance and motivation
- **Weekly Summaries**: Comprehensive analysis of your study progress
- **Mock Exam Reviews**: Detailed performance analysis and improvement suggestions

### 📚 Note Management

- **Upload & Organize**: Upload study notes in various formats
- **Smart Categorization**: Automatically categorize notes by subject
- **Content Search**: Find specific information across all your notes
- **File Management**: Secure storage and easy access to all materials

### 🧠 Study Tools

- **Flashcards**: Generate flashcards from your notes using AI
- **Mock Tests**: Create practice exams from uploaded materials
- **Past Papers**: Upload and organize previous exam papers
- **Progress Tracking**: Monitor your performance over time

### ⏱️ Study Sessions

- **Pomodoro Timer**: Built-in focus timer with customizable intervals
- **Flowmodoro**: Flexible study sessions that adapt to your flow state
- **Custom Timers**: Set your own study and break durations
- **Session Analytics**: Track time spent studying vs. taking breaks

### 👥 Collaborative Features

- **Public Sessions**: Join study sessions with other students
- **Private Groups**: Create invite-only study groups
- **Real-time Participants**: See who's studying alongside you
- **Session Sharing**: Share study sessions with friends

### 📋 Productivity Tools

- **Todo Lists**: Session-specific and general task management
- **Music Integration**: Spotify integration for study playlists

## 🛠️ Technology Stack

### Backend

- **Framework**: ASP.NET Core 6.0
- **Database**: SQLite with Entity Framework Core
- **Authentication**: ASP.NET Core Identity
- **AI Integration**: Google Gemini API for intelligent features

### Frontend

- **UI Framework**: Bootstrap 5
- **JavaScript**: Vanilla JS with jQuery
- **Icons**: Lucide Icons & Font Awesome
- **Styling**: Custom CSS with CSS Variables for theming

## 🚀 Getting Started

### Prerequisites

- .NET 6.0 SDK or later
- Visual Studio 2022 or VS Code
- Git

### Installation

1. **Clone the repository**

   ```bash
   git clone https://github.com/maihazem607/Suzy-Study-Partner.git
   cd Suzy
   ```

2. **Install dependencies**

   ```bash
   dotnet restore
   ```

3. **Set up the database**

   ```bash
   dotnet ef database update
   ```

4. **Configure AI Service**

   - Add your Google Gemini API key to `suzy-gemini-key.json` (you can do this in the settings page of the website aswell).
   - Update `appsettings.json` with your configuration

5. **Run the application**

   ```bash
   dotnet run
   ```

6. **Access the application**
   - Open your browser and navigate to `http://localhost:5000`

## 🏗️ Project Structure

```
Suzy/
├── Areas/Identity/          # Authentication pages
├── Controllers/
│   ├── ChatAnalyticsController.cs
│   ├── StudySessionController.cs
│   └── TodoController.cs
├── Data/                    # Database context and migrations
├── Models/                  # Data models
├── Pages/                   # Razor pages
│   ├── ChatWithSuzy/       # AI chat interface
│   ├── Dashboard/          # Main dashboard
│   ├── Flashcards/         # Flashcard management
│   ├── Sessions/           # Study sessions
│   ├── Uploadnotes/        # Note upload
│   └── Shared/             # Shared layouts and components
├── Services/               # Business logic services
├── wwwroot/                # Static files
│   ├── css/               # Stylesheets
│   ├── js/                # JavaScript files
│   ├── lib/               # Third-party libraries
│   └── uploads/           # User uploaded files
└── Program.cs             # Application entry point
```

## 👥 Team

**Suzy** is developed with ❤️ by:

- **[Ajay Anand](https://www.linkedin.com/in/ajay-anand-s-8a30a62b7/)**
- **[Mai Hazem](https://www.linkedin.com/in/mai-hazem-7a5459251/)**
- **[Haya Zaheer](https://www.linkedin.com/in/haya-zaheer-715b871b0/)**

<div align="center">
  <p>Made with ❤️ for learners everywhere</p>
</div>
