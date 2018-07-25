using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoipApplication;

namespace VoIP_Server
{
    class DatabaseManager
    {
        public void AddUser(Users users)
        {
            try
            {
                using (var db = new VoiceChatDBEntities())
                {
                    db.Users.Add(users);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw e;
            }

        }

        public void EditUser(Users user)
        {
            try
            {
                using (var db = new VoiceChatDBEntities())
                {
                    var result = db.Users.FirstOrDefault(u => u.UserId == user.UserId);
                    if(result!=null)
                    {
                        result.UserId = user.UserId;
                        result.Email = user.Email;
                        result.Password = user.Password;
                    }else
                    {
                        throw new Exception("Nie znaleziono takiego rekordu!");
                    }
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public void DeleteUser(Users user)
        {
            try
            {
                using (var db = new VoiceChatDBEntities())
                {
                    var userToRemove = db.Users.FirstOrDefault(u => u.Email == user.Email);
                    if(userToRemove!=null)
                    {
                        //delete friendsList
                        int id = userToRemove.UserId;
                        foreach (var item in db.FriendsList)
                        {
                            if(item.UserId==id || item.FriendId == id)//usuwany rekordy gdy my mamy usera w znajomych ale trez gdy on ma nas
                            {
                                db.FriendsList.Remove(item);
                            }
                        }
                        //now we can delete user
                        db.Users.Remove(userToRemove);
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        

    }
}
