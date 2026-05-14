namespace SmartMarkMVC.Models;

// ─── SHARED ───────────────────────────────────────────
public class LoginViewModel
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string Role { get; set; } = "";
    public string? ErrorMessage { get; set; }
}

// ─── STUDENT ──────────────────────────────────────────
public class ClassViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Teacher { get; set; } = "";
    public string Code { get; set; } = "";
    public string Chip { get; set; } = "";
    public string GradientCss { get; set; } = "";
    public string HexColor { get; set; } = "";
    public int TotalSessions { get; set; }
    public bool IsLive { get; set; }
    public string MeetingLink { get; set; } = "";
    public int AttendedSessions { get; set; }
    public int AttendancePercent => TotalSessions == 0 ? 0
        : (int)((AttendedSessions * 100.0) / TotalSessions);
}

public class AttendanceRecordViewModel
{
    public DateTime SessionDate { get; set; }
    public string SubjectName { get; set; } = "";
    public bool IsPresent { get; set; }
    public DateTime? JoinTime { get; set; }
    public DateTime? LeaveTime { get; set; }
    public int DurationMinutes { get; set; }
}

public class ClassDetailViewModel
{
    public ClassViewModel Class { get; set; } = new();
    public List<AttendanceRecordViewModel> AttendanceRecords { get; set; } = new();
}

public class StudentDashboardViewModel
{
    public string StudentName { get; set; } = "";
    public int TotalSubjects { get; set; }
    public int SubjectsBelowThreshold { get; set; }
    public List<ClassViewModel> Classes { get; set; } = new();
}

// ─── TEACHER ──────────────────────────────────────────
public class TeacherSubjectRow
{
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = "";
    public string SubjectCode { get; set; } = "";
    public int TotalStudents { get; set; }
    public string BarColor { get; set; } = "";
    public string BadgeBg { get; set; } = "";
    public string BadgeColor { get; set; } = "";
    public string BadgeText { get; set; } = "";
}

public class TeacherDashboardViewModel
{
    public string TeacherName { get; set; } = "";
    public int TotalSubjects { get; set; }
    public int TotalStudents { get; set; }
    public List<TeacherSubjectRow> Subjects { get; set; } = new();
}

public class AttendanceSessionViewModel
{
    public int SessionId { get; set; }
    public string SubjectName { get; set; } = "";
    public DateTime SessionDate { get; set; }
    public string Status { get; set; } = "";
    public int TotalStudents { get; set; }
    public int PresentCount { get; set; }
}

public class TakeAttendanceViewModel
{
    public int SessionId { get; set; }
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = "";
    public DateTime SessionDate { get; set; }
    public bool IsEditing { get; set; }
    public List<StudentAttendanceRow> Students { get; set; } = new();
}

public class StudentAttendanceRow
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = "";
    public string RollNumber { get; set; } = "";
    public bool IsPresent { get; set; }
}

// ─── TEACHER STUDENTS VIEW ────────────────────────────
public class TeacherStudentsViewModel
{
    public Dictionary<int, SubjectStudentGroup> SubjectGroups { get; set; } = new();
}

public class SubjectStudentGroup
{
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = "";
    public string SubjectCode { get; set; } = "";
    public List<StudentSummaryRow> Students { get; set; } = new();
}

public class StudentSummaryRow
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = "";
    public string RollNumber { get; set; } = "";
    public string Section { get; set; } = "";
    public int TotalSessions { get; set; }
    public int AttendedSessions { get; set; }
    public int AttendancePercent { get; set; }
}

// ─── SCHEDULE ─────────────────────────────────────────
public class ScheduleViewModel
{
    public int ScheduleId { get; set; }
    public string SubjectName { get; set; } = "";
    public string SubjectCode { get; set; } = "";
    public string DayOfWeek { get; set; } = "";
    public string StartTime { get; set; } = "";
    public string EndTime { get; set; } = "";
    public string MeetingLink { get; set; } = "";
    public bool IsRescheduled { get; set; }
    public string TeacherName { get; set; } = "";
}

// ─── STUDENT ATTENDANCE OVERVIEW ──────────────────────
public class StudentAttendanceOverviewViewModel
{
    public string StudentName { get; set; } = "";
    public List<SubjectAttendanceSummary> Subjects { get; set; } = new();
}

public class SubjectAttendanceSummary
{
    public string SubjectName { get; set; } = "";
    public string SubjectCode { get; set; } = "";
    public string TeacherName { get; set; } = "";
    public int TotalSessions { get; set; }
    public int AttendedSessions { get; set; }
    public int AttendancePercent => TotalSessions == 0 ? 0
        : (int)((AttendedSessions * 100.0) / TotalSessions);
}
