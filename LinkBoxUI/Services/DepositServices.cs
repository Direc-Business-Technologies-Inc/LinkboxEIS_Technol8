using InfrastructureLayer.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DomainLayer.ViewModels;
using LinkBoxUI.Context;
using DomainLayer;

namespace LinkBoxUI.Services
{
    public class DepositServices : IDepositRepository
    {
        private readonly LinkboxDb _context = new LinkboxDb();

        public bool Create_Deposit(Deposit deposit, int id)
        {
            deposit.IsActive = true;
            deposit.CreateDate = DateTime.Now;
            deposit.CreateUserID = id;
            _context.Deposits.Add(deposit);
            _context.SaveChanges();
            SaveChanges();
            return true;
        }

        public DepositCreateViewModel Find_Deposit(int id)
        {

            var model = new DepositCreateViewModel();

            model.DepositView = _context.Deposits.Where(x => x.BrkId == id).Select((x) => new DepositCreateViewModel.Deposit
            {
                BrkCode = x.BrkCode,
                BrkDescription = x.BrkDescription,
                IsActive = x.IsActive,

            }).ToList();
            return model;
        }

        public int SaveChanges()
        {
            return _context.SaveChanges();
        }

        public bool Update_Deposit(Deposit dep, int id)
        {
            var deposit = dep;     
            deposit.UpdateDate = DateTime.Now;
            deposit.UpdateUserID = id;
            _context.Entry(deposit).State = System.Data.Entity.EntityState.Modified;
            _context.SaveChanges();
            SaveChanges();
            return true;
        }

        public DepositCreateViewModel ValidateCode(string code)
        {
            var model = new DepositCreateViewModel();

            model.DepositView = _context.Deposits.Where(x => x.BrkCode == code).Select((x) => new DepositCreateViewModel.Deposit
            {
                BrkCode = x.BrkCode,
            }).ToList();
            return model;
        }

        public DepositCreateViewModel View_Deposit()
        {
            DepositCreateViewModel model = new DepositCreateViewModel();
            model.DepositView = _context.Deposits.Select(x =>
                    new DepositCreateViewModel.Deposit
                    {
                        BrkId = x.BrkId,
                        BrkCode = x.BrkCode,
                        BrkDescription = x.BrkDescription,
                        IsActive = x.IsActive

                    }).ToList();
            return model;
        }
    }
}