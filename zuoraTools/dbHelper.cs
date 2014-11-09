using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace zuoraTools
{
    class DbHelper
    {
        public SqlConnection Conn;

        public DbHelper(string host, string db, string username, string password)
        {
            //bi03.apex.local
            // create an open the connection

            SqlConnectionStringBuilder cs = new SqlConnectionStringBuilder
            {
                UserID = username,
                Password = password,
                IntegratedSecurity = true,
                DataSource = host,
                InitialCatalog = db
            };

            Conn = new SqlConnection(cs.ConnectionString);

            Conn.Open();
        }

        public string QuerySingleValue(string query)
        {
            string output = "";

            SqlCommand command = Conn.CreateCommand();
            command.CommandText = query;
            command.CommandType = CommandType.Text;

            SqlDataReader reader = command.ExecuteReader();

            string returnField = Regex.Split(query, @"\s").GetValue(1).ToString();

            // display the results
            if (reader.Read())
            {
                output = reader[returnField].ToString();
            }
            reader.Close();

            return output;
        }

        public string QuerySingle(string query, string returnField)
        {
            string output = "";

            SqlCommand command = Conn.CreateCommand();
            command.CommandText = query;
            command.CommandType = CommandType.Text;

            SqlDataReader reader = command.ExecuteReader();

            // display the results
            if (reader.Read())
            {
                output = reader[returnField].ToString();
               // Console.WriteLine(output);
            }
            reader.Close();

            return output;
        }
    }
}
