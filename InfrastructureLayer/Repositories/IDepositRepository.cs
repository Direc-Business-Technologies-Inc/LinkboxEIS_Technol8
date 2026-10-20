using DomainLayer;
using DomainLayer.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfrastructureLayer.Repositories
{
    public interface IDepositRepository
    {
        DepositCreateViewModel View_Deposit();

        bool Create_Deposit(Deposit deposit, int id);

        int SaveChanges();

        DepositCreateViewModel Find_Deposit(int id);

        bool Update_Deposit(Deposit dep, int id);

        DepositCreateViewModel ValidateCode(string code);


    }
}
