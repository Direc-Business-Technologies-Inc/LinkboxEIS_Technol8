// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultRegistry.cs" company="Web Advanced">
// Copyright 2012 Web Advanced (www.webadvanced.com)
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0

// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace LinkBoxUI.DependencyResolution {
    using InfrastructureLayer.Repositories;
    using LinkBoxUI.Services;
    using StructureMap.Configuration.DSL;
    using StructureMap.Graph;
	
    public class DefaultRegistry : Registry {
        #region Constructors and Destructors

        public DefaultRegistry() {
            Scan(
                scan => {
                    scan.TheCallingAssembly();
                    scan.WithDefaultConventions();
					scan.With(new ControllerConvention());
                });
            //For<IExample>().Use<Example>();
            For<IUserRepository>().Use<UserServices>();
            For<IAuthorizationRepository>().Use<AuthorizationServices>();
            For<IGlobalRepository>().Use<GlobalServices>();
            For<IConfigurationRepository>().Use<ConfigurationServices>();
            For<IMappingRepository>().Use<MappingServices>();
            For<IScheduleRepository>().Use<ScheduleServices>();
            For<IProcessRepository>().Use<ProcessServices>();
            For<ISyncRepository>().Use<SyncServices>();
            For<IEmailRepository>().Use<EmailServices>();
            For<IDepositRepository>().Use<DepositServices>();
            For<IUploadRepository>().Use<UploadServices>();
            For<ILoginRepository>().Use<LoginServices>();
        }

        #endregion
    }
}