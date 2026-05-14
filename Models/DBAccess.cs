using System.Data;
using Microsoft.Data.SqlClient;

namespace SmartMarkMVC.Models
{
    public class DBAccess
    {
        static string constr = @"Data Source=DESKTOP-FSSMFEE\SQLEXPRESS;
            Initial Catalog=SmartAttendanceDB;
            Integrated Security=True;
            TrustServerCertificate=True;";

        SqlConnection con = new SqlConnection(constr);
        SqlCommand cmd = null;
        SqlDataReader sdr = null;

        public void OpenConnection()
        {
            if (con.State == ConnectionState.Closed)
                con.Open();
        }

        public void ClosedConnection()
        {
            if (con.State == ConnectionState.Open)
                con.Close();
        }

        public void IUD(string query)
        {
            cmd = new SqlCommand(query, con);
            cmd.ExecuteNonQuery();
        }

        public SqlDataReader GetData(string query)
        {
            cmd = new SqlCommand(query, con);
            sdr = cmd.ExecuteReader();
            return sdr;
        }
    }
}