using DomainLayer;
using DomainLayer.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfrastructureLayer.Repositories
{
    public interface IProcessRepository
    {
        ProcessCreateViewModel View_Process();
        bool Create_Process(ProcessSetup process, string [] map, int id);
        bool OPSCreate_Process(ProcessSetup process, string[] map, int id);

        ProcessCreateViewModel Find_Process(int id);

        ProcessCreateViewModel OPSFind_Process(int id);


        bool Update_Process(ProcessSetup process, string [] map, int id);
        bool OPSUpdate_Process(ProcessSetup process, string[] map, int id);

        ProcessCreateViewModel Validate_Process(string code);
        ProcessCreateViewModel OPSValidate_Process(string code);

    }
}
