using DomainLayer;
using DomainLayer.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfrastructureLayer.Repositories
{
    public interface IUserRepository
    {
        void Create_New_User(User user,int userid);
        void Update_User(int Id, string UserName,
                                string FirstName, string LastName,
                                string MiddleName, bool IsActive, int userid, int AuthId,
                                string Password = null, string LastPassword=null);
        UserDetailsViewModel Find_User(int id);
        List<UserDetailsViewModel.UserViewModel> Fine_Key(int id);
        UserDetailsViewModel View_Users(int UserId);
        UserDetailsViewModel GetUserModules(int id);
        IEnumerable<Authorization> AuthorizationList();
        bool IsUserExist(string username);
        bool isActiveModule(int id);
        int GetModuleId(int id);
        List<int> NewModuleList(List<int> olddata, List<int> newdata);
        void UpdateUserModules(int userid, int moduleid, bool isactive);
        void AddUserModules(int createid, int userid, int moduleid);
        int SaveChanges();
    }
}
