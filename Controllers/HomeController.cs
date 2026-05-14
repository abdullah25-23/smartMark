using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using SmartMarkMVC.Models;

namespace SmartMarkMVC.Controllers;

public class HomeController : Controller
{
    // ── Splash Screen ──────────────────────────────────
    public IActionResult Index()
    {
        // If already logged in, skip to dashboard
        if (HttpContext.Session.GetString("Role") == "Teacher")
            return RedirectToAction("Dashboard", "Teacher");
        if (HttpContext.Session.GetString("Role") == "Student")
            return RedirectToAction("Dashboard", "Student");

        return View();
    }

    // ── Login GET ──────────────────────────────────────
    public IActionResult Login(string? role)
    {
        // If already logged in, skip to dashboard
        if (HttpContext.Session.GetString("Role") == "Teacher")
            return RedirectToAction("Dashboard", "Teacher");
        if (HttpContext.Session.GetString("Role") == "Student")
            return RedirectToAction("Dashboard", "Student");

        var model = new LoginViewModel { Role = role ?? "" };
        return View(model);
    }

    // ── Login POST ─────────────────────────────────────
    [HttpPost]
    public IActionResult Login(LoginViewModel model)
    {
        // Student email validation — must be .edu.pk
        if (model.Role == "Student" && !model.Email.EndsWith(".edu.pk"))
        {
            model.ErrorMessage = "Student accounts require a university email (.edu.pk)";
            return View(model);
        }

        if (string.IsNullOrWhiteSpace(model.Email) ||
            string.IsNullOrWhiteSpace(model.Password))
        {
            model.ErrorMessage = "Please enter your email and password.";
            return View(model);
        }

        var db = new DBAccess();
        try
        {
            db.OpenConnection();

            // Check user credentials
            string query = $@"
                SELECT u.Id, u.Name, u.Role,
                       s.Id AS StudentId,
                       t.Id AS TeacherId
                FROM Users u
                LEFT JOIN Students s ON s.UserId = u.Id
                LEFT JOIN Teachers t ON t.UserId = u.Id
                WHERE u.Email = '{model.Email}'
                AND u.PasswordHash = '{model.Password}'
                AND u.Role = '{model.Role}'";

            var reader = db.GetData(query);

            if (reader.Read())
            {
                string name = reader["Name"].ToString()!;
                string role = reader["Role"].ToString()!;
                int userId = (int)reader["Id"];

                // Save to session
                HttpContext.Session.SetString("UserName", name);
                HttpContext.Session.SetString("Role", role);
                HttpContext.Session.SetInt32("UserId", userId);

                if (role == "Teacher")
                {
                    int teacherId = Convert.ToInt32(reader["TeacherId"]);
                    HttpContext.Session.SetInt32("TeacherId", teacherId);
                    reader.Close();
                    db.ClosedConnection();
                    return RedirectToAction("Dashboard", "Teacher");
                }
                else
                {
                    int studentId = Convert.ToInt32(reader["StudentId"]);
                    HttpContext.Session.SetInt32("StudentId", studentId);
                    reader.Close();
                    db.ClosedConnection();
                    return RedirectToAction("Dashboard", "Student");
                }
            }
            else
            {
                reader.Close();
                model.ErrorMessage = "Invalid email or password. Please try again.";
                return View(model);
            }
        }
        catch (Exception ex)
        {
            model.ErrorMessage = "Something went wrong. Please try again.";
            return View(model);
        }
        finally
        {
            db.ClosedConnection();
        }
    }

    // ── Logout ─────────────────────────────────────────
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }

    public IActionResult Error()
    {
        return View();
    }
}