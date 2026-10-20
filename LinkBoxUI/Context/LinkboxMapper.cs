using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DomainLayer;
using DomainLayer.ViewModels;
using AutoMapper;
using DomainLayer.Models;

namespace LinkBoxUI.Context
{
    public class LinkboxMapper : Profile
    {
        public LinkboxMapper()
        {
            CreateMap<User, UserDetailsViewModel.UserViewModel>();
            CreateMap<UserDetailsViewModel.UserViewModel, User>();

            CreateMap<Authorization, Authorizations.AuthorizationViewModel>();
            CreateMap<Authorizations.AuthorizationViewModel, Authorization>();

            CreateMap<AddonSetup, SetupCreateViewModel.AddonViewModel>();
            CreateMap<SetupCreateViewModel.AddonViewModel, AddonSetup>();

            CreateMap<SAPSetup, SetupCreateViewModel.SAPViewModel>();
            CreateMap<SetupCreateViewModel.SAPViewModel, SAPSetup>();

            CreateMap<PathSetup, SetupCreateViewModel.PathViewModel>();
            CreateMap<SetupCreateViewModel.PathViewModel, PathSetup>();

            CreateMap<FieldMapping, MapCreateViewModel.FieldMapping>();

            CreateMap<OPSFieldMapping, MapCreateViewModel.OPSFieldMapping>();
            CreateMap<OPSFieldTable, MapCreateViewModel.OPSFieldTable>();


            CreateMap<MapCreateViewModel.FieldMapping, FieldMapping>();

            CreateMap<UploadSchedule, SchedCreateViewMdel.Schedule>();
            CreateMap<SchedCreateViewMdel.Schedule, UploadSchedule>();

            CreateMap<ProcessSetup, ProcessCreateViewModel.process>();
            CreateMap<ProcessCreateViewModel.process, ProcessSetup>();
          




            CreateMap<Sync, SyncViewModel.Sync>();
            CreateMap<SyncViewModel.Sync, Sync>();

            CreateMap<Query, SyncViewModel.Query>();
            CreateMap<SyncViewModel.Query, Query>();

            CreateMap<SyncQuery, SyncViewModel.SyncQuery>();
            CreateMap<SyncViewModel.SyncQuery, SyncQuery>();


            CreateMap<EmailLogs, EmailCreateViewModel.Email>();
            CreateMap<EmailCreateViewModel.Email, EmailLogs>();

            CreateMap<EmailSetup, SetupCreateViewModel.EmailViewModel>();
            CreateMap<SetupCreateViewModel.EmailViewModel, EmailSetup>();

            CreateMap<Deposit, DepositCreateViewModel.Deposit>();
            CreateMap<DepositCreateViewModel.Deposit, Deposit>();

            CreateMap<APISetup, SetupCreateViewModel.APIViewModel>();
            CreateMap<SetupCreateViewModel.APIViewModel, APISetup>();

            CreateMap<QueryManager, QueryManagerViewModel.QueryManager>();
            CreateMap<QueryManagerViewModel.QueryManager, QueryManager>();

            CreateMap<QueryManagerMapViewModel.QueryManagerMap, QueryManagerMap>();

            CreateMap<EmailTemplate, EmailCreateViewModel.EmailTemplate>();
            CreateMap<EmailCreateViewModel.EmailTemplate, EmailTemplate>();

            CreateMap<ApiParameter, ApiParameterViewModel.ApiParameter>();


            CreateMap<SyncViewModel.CrystalExtract, CrystalExtractSetup>();

            CreateMap<SyncViewModel.Query, QueryManager>();

            CreateMap<SetupCreateViewModel.DocumentMapView, DocumentMap>();

            CreateMap<UserDetailsViewModel.PasswordHistoryViewModel, PasswordHistoryModel>();


        }

        public static void Run()
        {
            AutoMapper.Mapper.Initialize(hj =>
            {
                hj.AddProfile<LinkboxMapper>();
            });
        }

    }
}