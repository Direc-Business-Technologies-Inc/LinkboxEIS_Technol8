using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfrastructureLayer.Repositories
{
    public interface IGetGlobalServices
    {
        DataTable GetData(string apiRoute);
    }
}
