using Microsoft.AspNetCore.Mvc;
using SmartMarkMVC.Models;
using Microsoft.Data.SqlClient;

namespace SmartMarkMVC.Controllers;

public class TeacherController : Controller
{
    private readonly DBAccess _db = new();

    // ── Dashboard ──────────────────────────────────────
    public IActionResult Dashboard()
    {
        int? teacherId = HttpContext.Session.GetInt32("TeacherId");
        string? teacherName = HttpContext.Session.GetString("UserName");
        if (teacherId == null) return RedirectToAction("Login", "Home");

        var subjects = new List<TeacherSubjectRow>();
        var colors = new[]
        {
            ("#4F46E5","#EDE8FF","#4F46E5"),
            ("#0F766E","#D1FAE5","#065F46"),
            ("#B45309","#FEF3C7","#92400E"),
            ("#9D174D","#FEE2E2","#991B1B"),
            ("#1D4ED8","#DBEAFE","#1E40AF"),
            ("#065F46","#D1FAE5","#065F46"),
        };
        int totalStudents = 0;

        try
        {
            _db.OpenConnection();
            string query = $@"
                SELECT s.Id, s.Name, s.Code,
                       COUNT(ss.StudentId) AS TotalStudents
                FROM Subjects s
                LEFT JOIN StudentSubject ss ON ss.SubjectId = s.Id
                WHERE s.TeacherId = {teacherId}
                GROUP BY s.Id, s.Name, s.Code";

            var reader = _db.GetData(query);
            int i = 0;
            while (reader.Read())
            {
                var c = colors[i % colors.Length];
                int stuCount = Convert.ToInt32(reader["TotalStudents"]);
                totalStudents += stuCount;
                subjects.Add(new TeacherSubjectRow
                {
                    SubjectId = (int)reader["Id"],
                    SubjectName = reader["Name"].ToString()!,
                    SubjectCode = reader["Code"].ToString()!,
                    TotalStudents = stuCount,
                    BarColor = c.Item1,
                    BadgeBg = c.Item2,
                    BadgeColor = c.Item3,
                    BadgeText = "Active"
                });
                i++;
            }
            reader.Close();
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Failed to load dashboard: " + ex.Message;
        }
        finally { _db.ClosedConnection(); }

        return View(new TeacherDashboardViewModel
        {
            TeacherName = teacherName ?? "Teacher",
            TotalSubjects = subjects.Count,
            TotalStudents = totalStudents,
            Subjects = subjects
        });
    }

    // ── Attendance Sessions ────────────────────────────
    public IActionResult AttendanceSessions(int subjectId)
    {
        int? teacherId = HttpContext.Session.GetInt32("TeacherId");
        if (teacherId == null) return RedirectToAction("Login", "Home");

        var sessions = new List<AttendanceSessionViewModel>();
        try
        {
            _db.OpenConnection();
            string query = $@"
                SELECT ats.Id, ats.SessionDate, ats.Status,
                       s.Name AS SubjectName,
                       COUNT(a.Id) AS TotalStudents,
                       ISNULL(SUM(CAST(a.IsPresent AS INT)), 0) AS PresentCount
                FROM AttendanceSession ats
                INNER JOIN ClassSchedule cs ON ats.ScheduleId = cs.Id
                INNER JOIN Subjects s ON cs.SubjectId = s.Id
                LEFT JOIN Attendance a ON a.SessionId = ats.Id
                WHERE cs.SubjectId = {subjectId} AND ats.TeacherId = {teacherId}
                GROUP BY ats.Id, ats.SessionDate, ats.Status, s.Name
                ORDER BY ats.SessionDate DESC";

            var reader = _db.GetData(query);
            while (reader.Read())
            {
                sessions.Add(new AttendanceSessionViewModel
                {
                    SessionId = (int)reader["Id"],
                    SubjectName = reader["SubjectName"].ToString()!,
                    SessionDate = Convert.ToDateTime(reader["SessionDate"]),
                    Status = reader["Status"].ToString()!,
                    TotalStudents = Convert.ToInt32(reader["TotalStudents"]),
                    PresentCount = Convert.ToInt32(reader["PresentCount"]),
                });
            }
            reader.Close();
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Could not load sessions: " + ex.Message;
        }
        finally { _db.ClosedConnection(); }

        ViewBag.SubjectId = subjectId;
        return View(sessions);
    }

    // ── Start Session ──────────────────────────────────
    public IActionResult StartSession(int subjectId)
    {
        int? teacherId = HttpContext.Session.GetInt32("TeacherId");
        if (teacherId == null) return RedirectToAction("Login", "Home");

        int sessionId = 0;
        try
        {
            _db.OpenConnection();
            string schedQuery = $"SELECT TOP 1 Id FROM ClassSchedule WHERE SubjectId = {subjectId}";
            var sr = _db.GetData(schedQuery);
            int scheduleId = 0;
            if (sr.Read()) scheduleId = (int)sr["Id"];
            sr.Close();

            if (scheduleId == 0)
            {
                TempData["Error"] = "No schedule found for this subject. Please create a schedule first.";
                return RedirectToAction("Dashboard");
            }

            string insertQuery = $@"
                INSERT INTO AttendanceSession (ScheduleId, TeacherId, SessionDate, Status)
                VALUES ({scheduleId}, {teacherId}, GETDATE(), 'Open');
                SELECT SCOPE_IDENTITY();";
            var ir = _db.GetData(insertQuery);
            if (ir.Read()) sessionId = Convert.ToInt32(ir[0]);
            ir.Close();
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Could not start session: " + ex.Message;
            return RedirectToAction("Dashboard");
        }
        finally { _db.ClosedConnection(); }

        return RedirectToAction("TakeAttendance", new { sessionId });
    }

    // ── Take Attendance ────────────────────────────────
    public IActionResult TakeAttendance(int sessionId)
    {
        int? teacherId = HttpContext.Session.GetInt32("TeacherId");
        if (teacherId == null) return RedirectToAction("Login", "Home");

        var model = new TakeAttendanceViewModel { SessionId = sessionId };
        try
        {
            _db.OpenConnection();

            // Get session info
            string sessionQuery = $@"
                SELECT ats.SessionDate, ats.Status, s.Name AS SubjectName, s.Id AS SubjectId
                FROM AttendanceSession ats
                INNER JOIN ClassSchedule cs ON ats.ScheduleId = cs.Id
                INNER JOIN Subjects s ON cs.SubjectId = s.Id
                WHERE ats.Id = {sessionId}";
            var sr = _db.GetData(sessionQuery);
            if (sr.Read())
            {
                model.SubjectName = sr["SubjectName"].ToString()!;
                model.SubjectId = (int)sr["SubjectId"];
                model.SessionDate = Convert.ToDateTime(sr["SessionDate"]);
                model.IsEditing = sr["Status"].ToString() == "Closed";
            }
            sr.Close();

            // Load existing attendance records
            var existing = new Dictionary<int, bool>();
            string existQ = $"SELECT StudentId, IsPresent FROM Attendance WHERE SessionId = {sessionId}";
            var er = _db.GetData(existQ);
            while (er.Read())
                existing[(int)er["StudentId"]] = Convert.ToBoolean(er["IsPresent"]);
            er.Close();

            // Load students
            string stuQuery = $@"
                SELECT st.Id, u.Name, st.RollNumber
                FROM Students st
                INNER JOIN Users u ON u.Id = st.UserId
                INNER JOIN StudentSubject ss ON ss.StudentId = st.Id
                WHERE ss.SubjectId = {model.SubjectId}
                ORDER BY u.Name";
            var stuR = _db.GetData(stuQuery);
            while (stuR.Read())
            {
                int sid = (int)stuR["Id"];
                model.Students.Add(new StudentAttendanceRow
                {
                    StudentId = sid,
                    StudentName = stuR["Name"].ToString()!,
                    RollNumber = stuR["RollNumber"].ToString()!,
                    IsPresent = existing.ContainsKey(sid) && existing[sid]
                });
            }
            stuR.Close();

            if (!model.Students.Any())
            {
                _db.IUD($"UPDATE AttendanceSession SET Status='Closed' WHERE Id={sessionId}");
                TempData["Error"] = "No students are enrolled in this subject yet.";
                return RedirectToAction("Dashboard");
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Could not load attendance page: " + ex.Message;
            return RedirectToAction("Dashboard");
        }
        finally { _db.ClosedConnection(); }

        return View(model);
    }

    // ── Save Attendance POST ───────────────────────────
    [HttpPost]
    public IActionResult SaveAttendance(int sessionId, List<int> presentStudentIds)
    {
        int? teacherId = HttpContext.Session.GetInt32("TeacherId");
        if (teacherId == null) return RedirectToAction("Login", "Home");

        try
        {
            _db.OpenConnection();

            // Get all enrolled students for this session
            string stuQuery = $@"
                SELECT st.Id FROM Students st
                INNER JOIN StudentSubject ss ON ss.StudentId = st.Id
                INNER JOIN ClassSchedule cs ON cs.SubjectId = ss.SubjectId
                INNER JOIN AttendanceSession ats ON ats.ScheduleId = cs.Id
                WHERE ats.Id = {sessionId}";
            var stuR = _db.GetData(stuQuery);
            var allStudents = new List<int>();
            while (stuR.Read()) allStudents.Add((int)stuR["Id"]);
            stuR.Close();

            // PDC — parallel threads with lock
            var threads = new List<Thread>();
            var lockObj = new object();

            foreach (int studentId in allStudents)
            {
                int sid = studentId;
                bool isPresent = presentStudentIds != null && presentStudentIds.Contains(sid);

                var thread = new Thread(() =>
                {
                    var tdb = new DBAccess();
                    try
                    {
                        tdb.OpenConnection();
                        lock (lockObj)
                        {
                            // Check if record exists
                            var cr = tdb.GetData($"SELECT COUNT(*) FROM Attendance WHERE SessionId={sessionId} AND StudentId={sid}");
                            cr.Read();
                            int exists = Convert.ToInt32(cr[0]);
                            cr.Close();

                            if (exists > 0)
                                tdb.IUD($@"UPDATE Attendance SET IsPresent={(isPresent ? 1 : 0)}, MarkedAt=GETDATE(), MarkedBy='Teacher' WHERE SessionId={sessionId} AND StudentId={sid}");
                            else
                                tdb.IUD($@"INSERT INTO Attendance(SessionId,StudentId,IsPresent,MarkedAt,MarkedBy) VALUES({sessionId},{sid},{(isPresent ? 1 : 0)},GETDATE(),'Teacher')");
                        }
                    }
                    catch { /* individual thread failure — skip */ }
                    finally { tdb.ClosedConnection(); }
                });
                threads.Add(thread);
                thread.Start();
            }

            foreach (var t in threads) t.Join();

            _db.OpenConnection();
            _db.IUD($"UPDATE AttendanceSession SET Status='Closed' WHERE Id={sessionId}");
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Failed to save attendance: " + ex.Message;
            return RedirectToAction("TakeAttendance", new { sessionId });
        }
        finally { _db.ClosedConnection(); }

        TempData["Success"] = "Attendance saved successfully!";
        return RedirectToAction("Dashboard");
    }

    // ── Students view ──────────────────────────────────
    public IActionResult Students()
    {
        int? teacherId = HttpContext.Session.GetInt32("TeacherId");
        if (teacherId == null) return RedirectToAction("Login", "Home");

        var model = new TeacherStudentsViewModel();
        try
        {
            _db.OpenConnection();
            string query = $@"
                SELECT s.Id AS SubjectId, s.Name AS SubjectName, s.Code,
                       st.Id AS StudentId, u.Name AS StudentName,
                       st.RollNumber, st.Section,
                       COUNT(a.Id) AS TotalSessions,
                       ISNULL(SUM(CAST(a.IsPresent AS INT)),0) AS AttendedSessions
                FROM Subjects s
                INNER JOIN StudentSubject ss ON ss.SubjectId = s.Id
                INNER JOIN Students st ON st.Id = ss.StudentId
                INNER JOIN Users u ON u.Id = st.UserId
                LEFT JOIN Attendance a ON a.StudentId = st.Id
                    AND a.SessionId IN (
                        SELECT ats.Id FROM AttendanceSession ats
                        INNER JOIN ClassSchedule cs ON ats.ScheduleId = cs.Id
                        WHERE cs.SubjectId = s.Id)
                WHERE s.TeacherId = {teacherId}
                GROUP BY s.Id, s.Name, s.Code, st.Id, u.Name, st.RollNumber, st.Section
                ORDER BY s.Name, u.Name";

            var reader = _db.GetData(query);
            while (reader.Read())
            {
                int subId = (int)reader["SubjectId"];
                if (!model.SubjectGroups.ContainsKey(subId))
                {
                    model.SubjectGroups[subId] = new SubjectStudentGroup
                    {
                        SubjectId = subId,
                        SubjectName = reader["SubjectName"].ToString()!,
                        SubjectCode = reader["Code"].ToString()!,
                        Students = new List<StudentSummaryRow>()
                    };
                }
                int total = Convert.ToInt32(reader["TotalSessions"]);
                int attended = Convert.ToInt32(reader["AttendedSessions"]);
                model.SubjectGroups[subId].Students.Add(new StudentSummaryRow
                {
                    StudentId = (int)reader["StudentId"],
                    StudentName = reader["StudentName"].ToString()!,
                    RollNumber = reader["RollNumber"].ToString()!,
                    Section = reader["Section"].ToString()!,
                    TotalSessions = total,
                    AttendedSessions = attended,
                    AttendancePercent = total == 0 ? 0 : (int)((attended * 100.0) / total)
                });
            }
            reader.Close();
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Could not load students: " + ex.Message;
        }
        finally { _db.ClosedConnection(); }

        return View(model);
    }

    // ── Schedule view ──────────────────────────────────
    public IActionResult Schedule()
    {
        int? teacherId = HttpContext.Session.GetInt32("TeacherId");
        if (teacherId == null) return RedirectToAction("Login", "Home");

        var schedules = new List<ScheduleViewModel>();
        try
        {
            _db.OpenConnection();
            string query = $@"
                SELECT cs.Id, cs.DayOfWeek, cs.StartTime, cs.EndTime,
                       cs.MeetingLink, cs.IsRescheduled, s.Name AS SubjectName, s.Code
                FROM ClassSchedule cs
                INNER JOIN Subjects s ON s.Id = cs.SubjectId
                WHERE s.TeacherId = {teacherId}
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
    // ── Remove Student from Subject ────────────────────
    [HttpPost]
    public IActionResult RemoveStudent(int studentId)
    {
        int? teacherId = HttpContext.Session.GetInt32("TeacherId");
        if (teacherId == null) return RedirectToAction("Login", "Home");

        try
        {
            _db.OpenConnection();

            // Get the subjectId taught by this teacher for this student
            string subQuery = $@"
            SELECT ss.SubjectId FROM StudentSubject ss
            INNER JOIN Subjects s ON s.Id = ss.SubjectId
            WHERE ss.StudentId = {studentId} AND s.TeacherId = {teacherId}";

            var reader = _db.GetData(subQuery);
            var subjectIds = new List<int>();
            while (reader.Read())
                subjectIds.Add((int)reader["SubjectId"]);
            reader.Close();

            foreach (int subId in subjectIds)
            {
                // Remove from StudentSubject
                _db.IUD($"DELETE FROM StudentSubject WHERE StudentId={studentId} AND SubjectId={subId}");

                // Also remove their attendance records for this subject
                _db.IUD($@"
                DELETE FROM Attendance 
                WHERE StudentId = {studentId}
                AND SessionId IN (
                    SELECT ats.Id FROM AttendanceSession ats
                    INNER JOIN ClassSchedule cs ON ats.ScheduleId = cs.Id
                    WHERE cs.SubjectId = {subId})");
            }

            TempData["Success"] = "Student removed successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Could not remove student: " + ex.Message;
        }
        finally { _db.ClosedConnection(); }

        return RedirectToAction("Students");
    }
}