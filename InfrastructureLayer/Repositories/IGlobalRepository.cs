using DomainLayer.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfrastructureLayer.Repositories
{
    public interface IGlobalRepository
    {
        List<int> GetModules(int id);
        DashboardViewModel GetItemStock();
        DashboardViewModel GetDashBoardReport();
        DashboardViewModel GetReportProgress();
        DashboardViewModel GetItemStock(string Code);
        string ConcatDecimal(string Value);
        byte[] DataExport();
    }
}
