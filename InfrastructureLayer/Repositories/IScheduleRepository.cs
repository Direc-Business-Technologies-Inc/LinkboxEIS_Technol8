using DomainLayer;
using DomainLayer.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfrastructureLayer.Repositories
{
    public interface IScheduleRepository
    {
        SchedCreateViewMdel View_Schedule();
        void Create_Schedule(UploadSchedule schedule, int id);
        //void OPSCreate_Schedule(UploadSchedule schedule, int id);
        SchedCreateViewMdel Find_Schedule(int id, string type);
        bool Update_Schedule(UploadSchedule schedule, int id);
        bool Run_Schedule(string Code);
        bool Enable_Schedule(string Code);
        bool Disable_Schedule(string Code);


        SchedCreateViewMdel Validate_Schedule(string code);
        int SaveChanges();
    }
}
