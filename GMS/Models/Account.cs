using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace GMS.Models
{
    public class Account
    {
        [DisplayName("User Name")] //Not used in final project, placeholders were used instead
        [Required(ErrorMessage = "Please enter your building's username")]
        public string username { get; set; }

        [DisplayName("Password")] //Not used in final project, placeholders were used instead
        [DataType(DataType.Password)]
        [Required(ErrorMessage = "Please enter your building's password")]
        public string password { get; set; }

        public string buildingCode { get; set; }

        public bool isAdmin { get; set; }

        public string errorMessage { get; set; }

        //This method validates the login credentials, takes username and password from login form entry
        public string LoginProcess(string u, string p) {
            //Open SQL connection and query the registered users table
            string message = "SELECT * FROM Users WHERE username = ";
            message += u;
            SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["connectionString"].ConnectionString);
            SqlCommand cmd = new SqlCommand(message, con);
            cmd.Parameters.AddWithValue("@username", u);
            message = "";

            try {
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                //Statement activates if SqlDataReader connection was set properly
                if (reader.Read()) {
                    //Discover if matching username and password were found
                    Boolean login = (p.Equals(reader["pword"].ToString(), StringComparison.InvariantCulture)) ? true : false;

                    if (login) {
                        message = "1";
                        buildingCode = reader["associatedBuilding"].ToString();
                    }

                    else message = "Invalid Credentials";
                }

                //Error message appears upon failed SQL connection
                else message = "Invalid Credentials";

                reader.Close();
                reader.Dispose();
                cmd.Dispose();
                con.Close();
            }

            //Exception error, try block failed
            catch (Exception ex) {
                message = ex.Message.ToString() + "Error.";
            }

            return message;
        }

    }
}