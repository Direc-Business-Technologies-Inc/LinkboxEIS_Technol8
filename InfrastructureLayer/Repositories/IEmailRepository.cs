using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DomainLayer.ViewModels;
using DomainLayer;
namespace InfrastructureLayer.Repositories
{
    public interface IEmailRepository
    {
        EmailCreateViewModel Get_AddonSetup();
        int SaveChanges();
        void Create_EmailSetup(EmailLogs email, string code,string[] cc, int id);
        void Create_EmailTemplate(EmailCreateViewModel.EmailTemplate email, int id);
        void Update_EmailTemplate(EmailCreateViewModel.EmailTemplate email, int id);
        EmailCreateViewModel Find_EmailSetup(int id);
        EmailCreateViewModel Find_EmailTemplate(int id);
        EmailCreateViewModel ValidateEmailCode(string code);

        
        EmailCreateViewModel ListConnectionString();

        bool Update_EmailSetup(EmailLogs email, string code, string[] cc, int id);

        EmailCreateViewModel View_EmailSetup();
    }
}
