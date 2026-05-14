# smartMark — Smart Attendance System

A role-based online class attendance management system built for BIIT as a 
Parallel & Distributed Computing semester project.

## Features

- **Teacher Module**
  - View and manage assigned courses (max 4)
  - Start live class sessions
  - Mark attendance with multi-select and bulk actions
  - Long press to remove a student from subject
  - View and edit past attendance sessions
  - View enrolled students with attendance analytics
  - View weekly class schedule

- **Student Module**
  - View enrolled classes with attendance percentage
  - Join live classes via Google Classroom link
  - View full attendance history per subject
  - Attendance warnings when below 75%
  - View weekly schedule with today's classes highlighted

- **PDC Concepts Implemented**
  - Multithreading — each student's attendance saved in a separate thread
  - Lock-based synchronization — prevents race conditions on DB writes
  - Client-Server Architecture — browser clients communicate with ASP.NET backend

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core MVC, C# |
| Database | SQL Server |
| Frontend | Razor Views, HTML, CSS, JavaScript |
| Concurrency | C# Threads, lock() synchronization |
| Auth | Cookie-based session management |

## Project Structure

SmartMarkMVC/
├── Controllers/
│   ├── HomeController.cs       # Login, Logout
│   ├── TeacherController.cs    # All teacher actions
│   └── StudentController.cs    # All student actions
├── Models/
│   ├── DBAccess.cs             # Database connectivity
│   └── ViewModels.cs           # All view models
├── Views/
│   ├── Home/                   # Login, Splash
│   ├── Teacher/                # Teacher pages
│   ├── Student/                # Student pages
│   └── Shared/                 # Sidebar, Layout
├── wwwroot/                    # Static files
└── Program.cs                  # App configuration

## Database Setup

1. Open SQL Server Management Studio
2. Run the script in `Database/SmartAttendanceDB.sql`
3. Run `Database/TestData.sql` for sample data

## Getting Started

### Prerequisites
- Visual Studio 2022
- .NET 8 SDK
- SQL Server or SQL Server Express

### Installation

1. Clone the repository
```bash
   git clone https://github.com/abdullah25-23/smartmark.git
```

2. Open `SmartMarkMVC.sln` in Visual Studio

3. Copy `appsettings.example.json` to `appsettings.json` and update your connection string
```json
   "Data Source=YOUR_SERVER\\SQLEXPRESS;Initial Catalog=SmartAttendanceDB;"
```

4. Run the database scripts from the `Database/` folder in order:
   - `SmartAttendanceDB.sql`
   - `TestData.sql`

5. Press `F5` to run the project

## Test Credentials

| Role | Email | Password |
|---|---|---|
| Teacher | ali.hussain@smartmark.com | teacher123 |
| Student | abdullah@biit.edu.pk | student123 |

## License
This project is for educational purposes only.
