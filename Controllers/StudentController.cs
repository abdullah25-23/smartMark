using Microsoft.AspNetCore.Mvc;
using SmartMarkMVC.Models;
using System.Data;
using Microsoft.Data.SqlClient;

namespace SmartMarkMVC.Controllers;

public class StudentController : Controller
{
    private readonly DBAccess _db = new();

    // ── Dashboard ──────────────────────────────────────
    public IActionResult Dashboard()
    {
        // Get logged in student id from session
        int? studentId = HttpContext.Session.GetInt32("StudentId");
        string? studentName = HttpContext.Session.GetString("UserName");

        if (studentId == null)
            return RedirectToAction("Login", "Home");

        var classes = new List<ClassViewModel>();

        try
        {
            _db.OpenConnection();

            // Get all subjects this student is enrolled in
            string query = $@"
    SELECT s.Id, s.Name, s.Code,
           u.Name AS TeacherName,
           cs.MeetingLink,
           (SELECT TOP 1 ats.Status 
            FROM AttendanceSession ats
            INNER JOIN ClassSchedule cs2 ON ats.ScheduleId = cs2.Id
            WHERE cs2.SubjectId = s.Id
            ORDER BY ats.SessionDate DESC) AS Status,
           (SELECT COUNT(*) FROM AttendanceSession ats 
            WHERE ats.ScheduleId IN 
                (SELECT Id FROM ClassSchedule WHERE SubjectId = s.Id)) AS TotalSessions,
           (SELECT COUNT(*) FROM Attendance a
            INNER JOIN AttendanceSession ats ON a.SessionId = ats.Id
            WHERE a.StudentId = {studentId} 
            AND a.IsPresent = 1
            AND ats.ScheduleId IN 
                (SELECT Id FROM ClassSchedule WHERE SubjectId = s.Id)) AS AttendedSessions
    FROM Subjects s
    INNER JOIN StudentSubject ss ON ss.SubjectId = s.Id
    INNER JOIN Teachers t ON t.Id = s.TeacherId
    INNER JOIN Users u ON u.Id = t.UserId
    LEFT JOIN ClassSchedule cs ON cs.SubjectId = s.Id
    WHERE ss.StudentId = {studentId}";

            var reader = _db.GetData(query);
            var colors = new[]
            {
                ("linear-gradient(135deg,#4F46E5,#818CF8)", "#4F46E5"),
                ("linear-gradient(135deg,#0F766E,#2DD4BF)", "#0F766E"),
                ("linear-gradient(135deg,#B45309,#FCD34D)", "#B45309"),
                ("linear-gradient(135deg,#9D174D,#F472B6)", "#9D174D"),
                ("linear-gradient(135deg,#1D4ED8,#60A5FA)", "#1D4ED8"),
                ("linear-gradient(135deg,#065F46,#34D399)", "#065F46"),
            };
            int colorIndex = 0;

            while (reader.Read())
            {
                var color = colors[colorIndex % colors.Length];
                string name = reader["Name"].ToString()!;
                classes.Add(new ClassViewModel
                {
                    Id = (int)reader["Id"],
                    Name = name,
                    Code = reader["Code"].ToString()!,
                    Teacher = reader["TeacherName"].ToString()!,
                    Chip = name.Length >= 3 ? name[..3].ToUpper() : name.ToUpper(),
                    GradientCss = color.Item1,
                    HexColor = color.Item2,
                    TotalSessions = Convert.ToInt32(reader["TotalSessions"]),
                    AttendedSessions = Convert.ToInt32(reader["AttendedSessions"]),
                    IsLive = reader["Status"] != DBNull.Value && reader["Status"].ToString() == "Live", 
                    MeetingLink = reader["MeetingLink"] == DBNull.Value ? "#" : reader["MeetingLink"].ToString()!, 
                });
                colorIndex++;
            }
            reader.Close();
        }
        finally
        {
            _db.ClosedConnection();
        }

        var model = new StudentDashboardViewModel
        {
            StudentName = studentName ?? "Student",
            TotalSubjects = classes.Count,
            SubjectsBelowThreshold = classes.Count(c => c.AttendancePercent < 75),
            Classes = classes
        };

        return View(model);
    }

    // ── Class Detail (attendance records) ─────────────
    public IActionResult ClassDetail(int id)
    {
        int? studentId = HttpContext.Session.GetInt32("StudentId");
        if (studentId == null)
            return RedirectToAction("Login", "Home");

        ClassViewModel? cls = null;
        var records = new List<AttendanceRecordViewModel>();

        try
        {
            _db.OpenConnection();

            // Get subject info
            string subQuery = $@"
    SELECT s.Id, s.Name, s.Code, u.Name AS TeacherName,
           (SELECT COUNT(*) FROM Attendance a
            INNER JOIN AttendanceSession ats ON a.SessionId = ats.Id
            INNER JOIN ClassSchedule cs ON ats.ScheduleId = cs.Id
            WHERE a.StudentId = {studentId}
            AND cs.SubjectId = s.Id) AS TotalSessions,
           (SELECT COUNT(*) FROM Attendance a
            INNER JOIN AttendanceSession ats ON a.SessionId = ats.Id
            INNER JOIN ClassSchedule cs ON ats.ScheduleId = cs.Id
            WHERE a.StudentId = {studentId} AND a.IsPresent = 1
            AND cs.SubjectId = s.Id) AS AttendedSessions
    FROM Subjects s
    INNER JOIN Teachers t ON t.Id = s.TeacherId
    INNER JOIN Users u ON u.Id = t.UserId
    WHERE s.Id = {id}";

            var r = _db.GetData(subQuery);
            if (r.Read())
            {
                string name = r["Name"].ToString()!;
                cls = new ClassViewModel
                {
                    Id = (int)r["Id"],
                    Name = name,
                    Code = r["Code"].ToString()!,
                    Teacher = r["TeacherName"].ToString()!,
                    Chip = name.Length >= 3 ? name[..3].ToUpper() : name.ToUpper(),
                    GradientCss = "linear-gradient(135deg,#4F46E5,#818CF8)",
                    HexColor = "#4F46E5",
                    TotalSessions = Convert.ToInt32(r["TotalSessions"]),
                    AttendedSessions = Convert.ToInt32(r["AttendedSessions"]),
                };
            }
            r.Close();

            if (cls == null)
                return RedirectToAction("Dashboard");

            // Get attendance records for this subject
            string attQuery = $@"
                SELECT ats.SessionDate, a.IsPresent, 
                       a.JoinTime, a.LeaveTime, a.DurationMinutes
                FROM Attendance a
                INNER JOIN AttendanceSession ats ON a.SessionId = ats.Id
                INNER JOIN ClassSchedule cs ON ats.ScheduleId = cs.Id
                WHERE a.StudentId = {studentId} AND cs.SubjectId = {id}
                ORDER BY ats.SessionDate DESC";

            var ar = _db.GetData(attQuery);
            while (ar.Read())
            {
                records.Add(new AttendanceRecordViewModel
                {
                    SessionDate = Convert.ToDateTime(ar["SessionDate"]),
                    SubjectName = cls.Name,
                    IsPresent = Convert.ToBoolean(ar["IsPresent"]),
                    JoinTime = ar["JoinTime"] == DBNull.Value ? null : Convert.ToDateTime(ar["JoinTime"]),
                    LeaveTime = ar["LeaveTime"] == DBNull.Value ? null : Convert.ToDateTime(ar["LeaveTime"]),
                    DurationMinutes = ar["DurationMinutes"] == DBNull.Value ? 0 : Convert.ToInt32(ar["DurationMinutes"]),
                });
            }
            ar.Close();
        }
        finally
        {
            _db.ClosedConnection();
        }

        return View(new ClassDetailViewModel { Class = cls!, AttendanceRecords = records });
    }

    // ── My Attendance Overview ─────────────────────────
    public IActionResult MyAttendance()
    {
        int? studentId = HttpContext.Session.GetInt32("StudentId");
        string? studentName = HttpContext.Session.GetString("UserName");
        if (studentId == null) return RedirectToAction("Login", "Home");

        var model = new StudentAttendanceOverviewViewModel { StudentName = studentName ?? "Student" };
        try
        {
            _db.OpenConnection();
            string query = $@"
            SELECT s.Name AS SubjectName, s.Code, u.Name AS TeacherName,
                   COUNT(a.Id) AS TotalSessions,
                   ISNULL(SUM(CAST(a.IsPresent AS INT)),0) AS AttendedSessions
            FROM Subjects s
            INNER JOIN StudentSubject ss ON ss.SubjectId = s.Id
            INNER JOIN Teachers t ON t.Id = s.TeacherId
            INNER JOIN Users u ON u.Id = t.UserId
            LEFT JOIN Attendance a ON a.StudentId = {studentId}
                AND a.SessionId IN (
                    SELECT ats.Id FROM AttendanceSession ats
                    INNER JOIN ClassSchedule cs ON ats.ScheduleId = cs.Id
                    WHERE cs.SubjectId = s.Id)
            WHERE ss.StudentId = {studentId}
            GROUP BY s.Name, s.Code, u.Name";

            var reader = _db.GetData(query);
            while (reader.Read())
            {
                model.Subjects.Add(new SubjectAttendanceSummary
                {
                    SubjectName = reader["SubjectName"].ToString()!,
                    SubjectCode = reader["Code"].ToString()!,
                    TeacherName = reader["TeacherName"].ToString()!,
                    TotalSessions = Convert.ToInt32(reader["TotalSessions"]),
                    AttendedSessions = Convert.ToInt32(reader["AttendedSessions"])
                });
            }
            reader.Close();
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Could not load attendance: " + ex.Message;
        }
        finally { _db.ClosedConnection(); }

        return View(model);
    }

    // ── My Schedule ────────────────────────────────────
    public IActionResult MySchedule()
    {
        int? studentId = HttpContext.Session.GetInt32("StudentId");
        if (studentId == null) return RedirectToAction("Login", "Home");

        var schedules = new List<ScheduleViewModel>();
        try
        {
            _db.OpenConnection();
            string query = $@"
            SELECT cs.Id, cs.DayOfWeek, cs.StartTime, cs.EndTime,
                   cs.MeetingLink, cs.IsRescheduled,
                   s.Name AS SubjectName, s.Code,
                   u.Name AS TeacherName
            FROM ClassSchedule cs
            INNER JOIN Subjects s ON s.Id = cs.SubjectId
            INNER JOIN Teachers t ON t.Id = s.TeacherId
            INNER JOIN Users u ON u.Id = t.UserId
            INNER JOIN StudentSubject ss ON ss.SubjectId = s.Id
            WHERE ss.StudentId = {studentId}
            ORDER BY cs.DayOfWeek, cs.StartTime";

            var reader = _db.GetData(query);
            while (reader.Read())
            {
                schedules.Add(new ScheduleViewModel
                {
                    ScheduleId = (int)reader["Id"],
                    SubjectName = reader["SubjectName"].ToString()!,
                    SubjectCode = reader["Code"].ToString()!,
                    DayOfWeek = reader["DayOfWeek"].ToString()!,
                    StartTime = reader["StartTime"].ToString()!,
                    EndTime = reader["EndTime"].ToString()!,
                    MeetingLink = reader["MeetingLink"] == DBNull.Value ? "" : reader["MeetingLink"].ToString()!,
                    IsRescheduled = Convert.ToBoolean(reader["IsRescheduled"])
                });
            }
            reader.Close();
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Could not load schedule: " + ex.Message;
        }
        finally { _db.ClosedConnection(); }

        return View(schedules);
    }
}
