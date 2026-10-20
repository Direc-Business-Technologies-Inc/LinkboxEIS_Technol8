using DomainLayer;
using DomainLayer.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfrastructureLayer.Repositories
{
    public interface ILoginRepository
    {
        void Login_Initialize();
        int SaveChanges();

        User Get_User(LoginViewModels.Login login);
        User Check_User(LoginViewModels.Login login,User user);
        void Login_Attempt(int id,int count);
        void Logout(int id);

        void Update_Attempt(User user,User userpass);
    }
}
