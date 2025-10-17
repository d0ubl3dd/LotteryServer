using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.DAOs
{
    public class UserDAO
    {
        public void CheckNikname(string nickname)
        {
            try
            {
                using (var context = new base_pruebaEntities1())
                {
                    context.Database.Connection.Open();
                    var user = context.User.FirstOrDefault(u => u.nickname == nickname);
                    if (user != null)
                    {
                        throw new Exception("Nickname already exists");
                    }
                }
            }
            catch (EntityException ex)
            {
                throw new Exception("Error checking nickname: " + ex.Message);
            }
            catch (SqlException ex)
            {
                throw new Exception("SQL Error checking nickname: " + ex.Message);
            }
        }
        public void CheckEmail(string email)
        {
            try
            {
                using (var context = new base_pruebaEntities1())
                {
                    context.Database.Connection.Open();
                    var user = context.User.FirstOrDefault(u => u.email == email);
                    if (user != null)
                    {
                        throw new Exception("Email already exists");
                    }
                }
            }
            catch (EntityException ex)
            {
                throw new Exception("Error checking email: " + ex.Message);
            }
            catch (SqlException ex)
            {
                throw new Exception("SQL Error checking email: " + ex.Message);
            }
        }

        public void AddUser(User user)
        {
            try
            {
                using (var context = new base_pruebaEntities1())
                {
                    context.Database.Connection.Open();
                    CheckNikname(user.nickname);
                    CheckEmail(user.email);
                    var newUser = new User()
                    {
                        nickname = user.nickname,
                        email = user.email,
                        //password = user.password,
                        registration_date = user.registration_date,
                        first_name = user.first_name,
                        paternal_last_name = user.paternal_last_name,
                        maternal_last_name = user.maternal_last_name,
                        score = user.score,
                        id_avatar = user.id_avatar
                    };
                    context.User.Add(newUser);
                    context.SaveChanges();
                }
            }
            catch (EntityException ex)
            {
                throw new Exception("Error adding user: " + ex.Message);
            }
            catch (SqlException ex)
            {
                throw new Exception("SQL Error adding user: " + ex.Message);
            }
        }






    }
}
