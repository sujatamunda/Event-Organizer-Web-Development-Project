using EventOrganizer.Models;
using EventOrganizer.Repository.Interface;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace EventOrganizer.Repository.Services
{
    public class HomeRepository : IHome
    {
        private readonly IConfiguration _configuration;
        public HomeRepository(IConfiguration configuration)
        {
            _configuration = configuration;

        }


        public SqlConnection GetConnection()
        {
            return new SqlConnection(_configuration.GetConnectionString("IEOConnection"));
        }

        public string CreateOrUpdate(LoginRequest objs)

        {
            string TransType = string.Empty;
            using (var con = GetConnection())
            {
                con.Open();
                SqlCommand cmd = new SqlCommand("SP_Login_CURD", con);
                cmd.CommandType = CommandType.StoredProcedure;

            }
            return TransType;
        }


        public string CreateOrUpdate(Register objs)
        {
            string TransType = string.Empty;
            using (var con = GetConnection())
            {
                con.Open();
                SqlCommand cmd = new SqlCommand("SP_Register_CURD", con);
                cmd.CommandType = CommandType.StoredProcedure;
                if (objs.UserId > 0)
                {
                    TransType = "UPDATE";
                    cmd.Parameters.AddWithValue("@TransType", TransType);
                    cmd.Parameters.AddWithValue("@RegId", objs.UserId);
                }
                else
                {
                    TransType = "INSERT";
                    cmd.Parameters.AddWithValue("@TransType", TransType);
                    cmd.Parameters.AddWithValue("@UserId", DBNull.Value);
                }

                cmd.Parameters.AddWithValue("@FirstName", objs.FirstName);
                cmd.Parameters.AddWithValue("@LastName", objs.LastName);
                cmd.Parameters.AddWithValue("@PhoneNumber", objs.PhoneNumber);
                cmd.Parameters.AddWithValue("@Email", objs.Email);
                cmd.Parameters.AddWithValue("@Address", objs.Address);
                
               
                cmd.Parameters.AddWithValue("@Password", objs.Password);
                cmd.ExecuteNonQuery();


            }
            return TransType;
        }

        public List<Register> GetSelect()
        {
            Register objs = new Register();
            using (var con = GetConnection())
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
                        
                       
                        Password = rdr["Password"].ToString()
                    });
                }
            }
            return objs.RegisterList;
        }

        public void Delete(int id)
        {
            using (var con = GetConnection())
            {
                con.Open();
                SqlCommand cmd = new SqlCommand("SP_Register_CURD", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@TransType", "DELETE");
                cmd.Parameters.AddWithValue("@UserId", id);
                cmd.ExecuteNonQuery();
            }
        }

        public Register GetSelectOne(int id)
        {
            Register objs = new Register();
            using (var con = GetConnection())
            {
                con.Open();
                SqlCommand cmd = new SqlCommand("SP_Register_CURD", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@TransType", "SELECT-ONE");
                cmd.Parameters.AddWithValue("@UserId", id);
                SqlDataReader rdr = cmd.ExecuteReader();
                if (rdr.Read())
                {

                    objs.UserId = (int)rdr["UserId"];
                    objs.FirstName = rdr["Firstname"].ToString();
                    objs.LastName = rdr["LastName"].ToString();
                    objs.PhoneNumber = rdr["PhoneNumber"].ToString();
                    objs.Email = rdr["Email"].ToString();
                    objs.Address = rdr["Address"].ToString();
                    
                    
                    objs.Password = rdr["Password"].ToString();

                }
            }
            return objs;
        }
    }
}
