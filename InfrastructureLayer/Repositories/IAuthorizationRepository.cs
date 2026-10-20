using DomainLayer;
using DomainLayer.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfrastructureLayer.Repositories
{
    public interface IAuthorizationRepository
    {
        bool Create_Authorization(Authorization authorization,int id);
        Authorizations View_Authorization();
        Authorizations Find_Authorization(int id);
        bool Update_Authorization(Authorization authorization, int id);
        Authorizations Get_AuthorizationModules(int id);
        void Update_AuthorizationModules(int userid, int moduleid, bool isactive);
        bool isActiveModule(int id);
        int GetModuleId(int id);
        List<int> NewModuleList(List<int> olddata, List<int> newdata);
        void Add_AuthorizationModules(int createid, int authid, int moduleid);
        int SaveChanges();
    }
}
