using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DomainLayer.ViewModels;
using DomainLayer;
using System.Data;

namespace InfrastructureLayer.Repositories
{
    public interface IUploadRepository
    {
        UploadViewModel View_Upload();
        UploadViewModel GetUploadDetails();
        UploadViewModel Get_Details(string map);
        Task<bool> Upload(string map);
        Task<string> UploadMultiple(string [] map);
        Task<string> UploadMultipleSAPtoSAP(string [] map);
        Task<string> UploadPlatForm(string code, string DateFrom,string DateTo);

        DataTable GetData(string Code);
        UploadViewModel GetProcess(string ProcessCode);
        UploadViewModel GetProgress(string ProcessCode);
        UploadViewModel GetSAPtoSAPProcess(string ProcessCode);
        UploadViewModel GetSAPtoSAPProgress(string ProcessCode);
    }
}
