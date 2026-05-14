CREATE DATABASE SmartAttendanceDB

USE SmartAttendanceDB

-- Users table (with login fields added)
CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Role NVARCHAR(50) NOT NULL  -- 'Teacher' or 'Student'
);

-- Students (linked to Users)
CREATE TABLE Students (
    Id INT PRIMARY KEY IDENTITY,
    UserId INT NOT NULL,
    RollNumber NVARCHAR(20),
    Section NVARCHAR(10),
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

-- Teachers (linked to Users)
CREATE TABLE Teachers (
    Id INT PRIMARY KEY IDENTITY,
    UserId INT NOT NULL,
    Department NVARCHAR(100),
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

-- Subjects (assigned to a teacher)
CREATE TABLE Subjects (
    Id INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(100) NOT NULL,
    Code NVARCHAR(20),
    TeacherId INT,
    FOREIGN KEY (TeacherId) REFERENCES Teachers(Id)
);

-- Which students are enrolled in which subjects
CREATE TABLE StudentSubject (
    Id INT PRIMARY KEY IDENTITY,
    StudentId INT NOT NULL,
    SubjectId INT NOT NULL,
    FOREIGN KEY (StudentId) REFERENCES Students(Id),
    FOREIGN KEY (SubjectId) REFERENCES Subjects(Id)
);

-- Class schedule (with reschedule support)
CREATE TABLE ClassSchedule (
    Id INT PRIMARY KEY IDENTITY,
    SubjectId INT NOT NULL,
    DayOfWeek NVARCHAR(10),
    StartTime TIME,
    EndTime TIME,
    MeetingLink NVARCHAR(255),      -- Zoom/Meet link
    MinRequiredMinutes INT DEFAULT 30, -- must attend 30 mins to be present
    IsRescheduled BIT DEFAULT 0,
    FOREIGN KEY (SubjectId) REFERENCES Subjects(Id)
);

-- Each time attendance is taken = one session
CREATE TABLE AttendanceSession (
    Id INT PRIMARY KEY IDENTITY,
    ScheduleId INT NOT NULL,
    TeacherId INT NOT NULL,
    SessionDate DATETIME NOT NULL,
    Status NVARCHAR(20) DEFAULT 'Open', -- Open / Closed
    FOREIGN KEY (ScheduleId) REFERENCES ClassSchedule(Id),
    FOREIGN KEY (TeacherId) REFERENCES Teachers(Id)
);

-- Individual student attendance per session
CREATE TABLE Attendance (
    Id INT PRIMARY KEY IDENTITY,
    SessionId INT NOT NULL,
    StudentId INT NOT NULL,
    JoinTime DATETIME,              -- when student joined
    LeaveTime DATETIME,             -- when student left
    DurationMinutes INT,            -- calculated duration
    IsPresent BIT NOT NULL,         -- auto decided based on duration
    MarkedAt DATETIME DEFAULT GETDATE(),
    MarkedBy NVARCHAR(50) DEFAULT 'System',
    FOREIGN KEY (SessionId) REFERENCES AttendanceSession(Id),
    FOREIGN KEY (StudentId) REFERENCES Students(Id)
);
-- 1. Insert Teacher Users
INSERT INTO Users (Name, Email, PasswordHash, Role) VALUES
('Ali Hussain',   'ali.hussain@smartmark.com',   'teacher123', 'Teacher'),
('Sara Ahmed',    'sara.ahmed@smartmark.com',     'teacher123', 'Teacher');

-- 2. Insert Teachers
INSERT INTO Teachers (UserId, Department) VALUES
(1, 'Computer Science'),
(2, 'Mathematics');

-- 3. Insert Student Users
INSERT INTO Users (Name, Email, PasswordHash, Role) VALUES
('Muhammad Abdullah', 'abdullah@biit.edu.pk',  'student123', 'Student'),
('Aqib Javeed',       'aqib@biit.edu.pk',       'student123', 'Student'),
('Farhan Hussain',    'farhan@biit.edu.pk',      'student123', 'Student'),
('Ahmed Raza',        'ahmed@biit.edu.pk',       'student123', 'Student');

-- 4. Insert Students
INSERT INTO Students (UserId, RollNumber, Section) VALUES
(3, '23-A-4011', 'A'),
(4, '23-A-3975', 'A'),
(5, '23-A-4046', 'A'),
(6, '23-A-3974', 'A');

-- 5. Insert Subjects (Teacher 1 teaches 3 subjects)
INSERT INTO Subjects (Name, Code, TeacherId) VALUES
('Parallel & Distributed Computing', 'CS-501', 1),
('Analysis of Algorithms',           'CS-401', 1),
('Distributed Database Systems',     'CS-451', 1),
('Calculus II',                      'MA-201', 2);

-- 6. Insert Class Schedules
INSERT INTO ClassSchedule 
    (SubjectId, DayOfWeek, StartTime, EndTime, MeetingLink, MinRequiredMinutes, IsRescheduled) 
VALUES
(1, 'Monday',    '09:00', '10:30', 'https://classroom.google.com/c/pdc101',  45, 0),
(2, 'Tuesday',   '11:00', '12:30', 'https://classroom.google.com/c/algo401', 45, 0),
(3, 'Wednesday', '14:00', '15:30', 'https://classroom.google.com/c/dds451',  45, 0),
(4, 'Thursday',  '10:00', '11:30', 'https://classroom.google.com/c/cal201',  45, 0);

-- 7. Enroll Students in Subjects
INSERT INTO StudentSubject (StudentId, SubjectId) VALUES
(1, 1), (1, 2), (1, 3),  -- Abdullah enrolled in 3 subjects
(2, 1), (2, 2),           -- Aqib enrolled in 2 subjects
(3, 1), (3, 3),           -- Farhan enrolled in 2 subjects
(4, 2), (4, 3);           -- Ahmed enrolled in 2 subjects

-- 8. Insert a test Attendance Session
INSERT INTO AttendanceSession 
    (ScheduleId, TeacherId, SessionDate, Status) 
VALUES
(1, 1, GETDATE(), 'Closed');

-- 9. Insert test Attendance records
INSERT INTO Attendance 
    (SessionId, StudentId, IsPresent, MarkedAt, MarkedBy) 
VALUES
(1, 1, 1, GETDATE(), 'Teacher'),  -- Abdullah present
(1, 2, 1, GETDATE(), 'Teacher'),  -- Aqib present
(1, 3, 0, GETDATE(), 'Teacher'),  -- Farhan absent
(1, 4, 1, GETDATE(), 'Teacher');  -- Ahmed present
