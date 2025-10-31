using System.Data.SqlClient;
using System.Data;
using EventOrganizer.Models;
using EventOrganizer.Repository.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Linq.Expressions;
using System.Text;

namespace EventOrganizer.Controllers
{
    public class LoginController : Controller
    {
        public readonly ILogger<LoginController> _logger;
        private readonly IConfiguration _configuration;
        public readonly IErrorLogs _errorLogs;
        public readonly ILogin _login;
        private readonly IHome _home;
        public LoginController(IErrorLogs errorLogs, ILogger<LoginController> logger, IConfiguration configuration, ILogin login)
        {
            _login = login;
            _errorLogs = errorLogs;
            _logger = logger;
            _configuration = configuration;
        }


      


        [Obsolete]
        private SqlConnection GetConnection()
        {
            return new SqlConnection(_configuration.GetConnectionString("IEOConnection"));
        }
        public IActionResult Index()
        {
            LoginRequest obj = new LoginRequest();
            return View(obj);
        }
        

        [HttpPost]
        public IActionResult Index(LoginRequest request)
        {
            LoginRequest obj = new LoginRequest();
            obj = request;
            try
            {
                if (ModelState.IsValid)
                {
                    ModelState.Clear();
                    var Result = _login.GetLogin(obj);  // Fetch login result from DB

                    if (Result.UserId > 0)
                    {
                        // Decode the entered password (Base64)
                        string encodedPassword = EncodePasswordToBase64(obj.Password);  // Encode entered password


                        // Compare decoded password (entered) with stored (Base64-encoded) password
                        if (encodedPassword == Result.Password)
                        {
                            HttpContext.Session.SetInt32("UserId", Result.UserId);
                            HttpContext.Session.SetString("UserEmail", Result.Email);
                            HttpContext.Session.SetString("UserType", Result.UserType);
                            HttpContext.Session.SetString("UName", Result.FirstName);
                            HttpContext.Session.SetString("ULastName", Result.LastName);

                            // Redirect based on user type
                            if (Result.UserType == "Admin")
                            {
                                return RedirectToAction("Dashboard", "Admin");
                            }
                            else if (Result.UserType == "User")
                            {
                                return RedirectToAction("Dashboard", "User");
                            }
                        }
                        else
                        {
                            ViewBag.Error = "Invalid email or password!";
                        }
                    }
                    else
                    {
                        ViewBag.Error = "Invalid email or password!";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during login.");
                _errorLogs.ErrorLog(LogLevel.Error.ToString(), ex.Message, ex.StackTrace);
            }

            return View(obj);
        }


        //private string DecodeBase64Password(string password)
        //{
        //    byte[] passwordBytes = Convert.FromBase64String(password);
        //    return Encoding.UTF8.GetString(passwordBytes);  // Decode Base64 back to original password
        //}

        private string DecodeBase64Password(string password)
        {
            // Check if the password is null or empty before attempting to decode
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password cannot be null or empty", nameof(password));
            }

            try
            {
                byte[] passwordBytes = Convert.FromBase64String(password);
                return Encoding.UTF8.GetString(passwordBytes);
            }
            catch (FormatException ex)
            {
                // Log the exception and rethrow or handle it as needed
                _logger.LogError(ex, "Error decoding password from Base64");
                throw new ArgumentException("Invalid Base64 encoded password.", nameof(password));
            }
        }


        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction("Index", "Login");
        }




        public IActionResult Register()
        {
            var model = new Register();
            return View(model);
        }


        [HttpPost]



        public IActionResult Register(Register register)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Encode the password to Base64 before inserting it into the database
                    string encodedPassword = EncodePasswordToBase64(register.Password);

                    using (var con = _login.GetConnection())

                    {
                        con.Open();
                        SqlCommand cmd = new SqlCommand("SP_Register_CURD", con);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@FirstName", register.FirstName);
                        cmd.Parameters.AddWithValue("@LastName", register.LastName);
                        cmd.Parameters.AddWithValue("@PhoneNumber", register.PhoneNumber);
                        cmd.Parameters.AddWithValue("@Address", register.Address);
                        
                        cmd.Parameters.AddWithValue("@Password", encodedPassword);
                        cmd.Parameters.AddWithValue("@Email", register.Email);
                        // ✅ Set UserType to 'User' if null/empty
                        cmd.Parameters.AddWithValue("@UserType", string.IsNullOrEmpty(register.UserType) ? "User" : register.UserType);


                        int i = cmd.ExecuteNonQuery();
                        if (i > 0)
                        {
                            ViewBag.SuccessMsg = $"Register Successfull. Please Login.";
                            ModelState.Clear();
                            return View();
                        }
                        else
                        {
                            ViewBag.ErrorMsg = "Data not inserted";
                            return View(register);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMsg = ex.Message;
                }
            }
            return View(register);
        }

       
        public IActionResult RegisterList()
        {
            
            Register objs = new Register();
            try
            {
                using (var con = _login.GetConnection())
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("SP_Register_CURD", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@TransType", "SELECT");
                    SqlDataReader rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        objs.RegisterList.Add(new Register
                        {
                            UserId = (int)rdr["UserId"],
                            FirstName = rdr["FirstName"].ToString(),
                            LastName = rdr["LastName"].ToString(),
                            PhoneNumber = rdr["PhoneNumber"].ToString(),
                            Email = rdr["Email"].ToString(),
                            Address = rdr["Address"].ToString(),
                            
                            UserType = rdr["UserType"].ToString(),
                            Password = rdr["Password"].ToString()
                        });
                    }
                }
            }

            catch (Exception ex)
            {
                ViewBag.ErrorMsg = ex.Message;

            }

            return View(objs);
        }

        private string EncodePasswordToBase64(string password)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            return Convert.ToBase64String(passwordBytes);  // Encoding password to Base64
        }









        public IActionResult ForgotPassword()
        {
            return View();
        }



        //public IActionResult MigrateOldPasswords()
        //{
        //    try
        //    {
        //        string connStr = _configuration.GetConnectionString("IEOConnection");
        //        using (SqlConnection con = new SqlConnection(connStr))
        //        {
        //            con.Open();
        //            SqlCommand cmd = new SqlCommand("SELECT UserId, Password FROM Users", con);
        //            SqlDataReader reader = cmd.ExecuteReader();

        //            List<(int UserId, string Password)> users = new List<(int, string)>();

        //            while (reader.Read())
        //            {
        //                users.Add((reader.GetInt32(0), reader.GetString(1)));
        //            }
        //            reader.Close();

        //            foreach (var user in users)
        //            {
        //                if (!IsBase64String(user.Password))
        //                {
        //                    string encodedPassword = EncodePasswordToBase64(user.Password);

        //                    SqlCommand updateCmd = new SqlCommand("UPDATE Users SET Password = @Password WHERE UserId = @UserId", con);
        //                    updateCmd.Parameters.AddWithValue("@Password", encodedPassword);
        //                    updateCmd.Parameters.AddWithValue("@UserId", user.UserId);
        //                    updateCmd.ExecuteNonQuery();
        //                }
        //            }
        //        }

        //        ViewBag.SuccessMsg = "✅ Password migration completed successfully.";
        //    }
        //    catch (Exception ex)
        //    {
        //        ViewBag.ErrorMsg = "❌ Error occurred: " + ex.Message;
        //    }

        //    return Content("Password migration completed successfully!"); // ✅ Show message directly
        //}

        //public IActionResult MigrateOldPasswords()
        //{
        //    string connectionString = _configuration.GetConnectionString("IEOConnection");

        //    List<(int UserId, string Password)> users = new List<(int, string)>();

        //    using (SqlConnection con = new SqlConnection(connectionString))
        //    {
        //        con.Open();
        //        SqlCommand cmd = new SqlCommand("SELECT UserId, Password FROM [User]", con);
        //        SqlDataReader reader = cmd.ExecuteReader();

        //        while (reader.Read())
        //        {
        //            users.Add((reader.GetInt32(0), reader.GetString(1)));
        //        }
        //        reader.Close(); // ✅ Close the reader first before doing updates
        //    }

        //    using (SqlConnection con = new SqlConnection(connectionString))
        //    {
        //        con.Open();
        //        foreach (var user in users)
        //        {
        //            if (!IsBase64String(user.Password))
        //            {
        //                string encodedPassword = EncodePasswordToBase64(user.Password);

        //                SqlCommand updateCmd = new SqlCommand("UPDATE [User] SET Password = @Password WHERE UserId = @UserId", con);
        //                updateCmd.Parameters.AddWithValue("@Password", encodedPassword);
        //                updateCmd.Parameters.AddWithValue("@UserId", user.UserId);
        //                updateCmd.ExecuteNonQuery();
        //            }
        //        }
        //    }

        //    return Content("Password migration completed successfully!");
        //}


        ////// ✅ Check if already Base64
        //private bool IsBase64String(string s)
        //{
        //    Span<byte> buffer = new Span<byte>(new byte[s.Length]);
        //   return Convert.TryFromBase64String(s, buffer, out _);
        //}

        //// 🛑 DO NOT ADD EncodePasswordToBase64 again if already present!
        //// ✅ Use your existing EncodePasswordToBase64(string password) method.

    }
}
    
