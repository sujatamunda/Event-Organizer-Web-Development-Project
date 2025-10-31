using System.Data.SqlClient;
using EventOrganizer.Models;

namespace EventOrganizer.Repository.Interface
{
    public interface ILogin
    {
        LoginResponse GetLogin(LoginRequest obj);


        SqlConnection GetConnection();
        string CreateOrUpdate(Register objs);
        List<Register> GetSelect();
        void Delete(int id);
        Register GetSelectOne(int id);
    }

}