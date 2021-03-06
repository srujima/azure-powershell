﻿// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

using System;

namespace Microsoft.Azure.Commands.Automation.Common
{
    public class Constants
    {
        public const string ClientIdentity = "PowerShell";

        public const char RunbookTagsSeparatorChar = ',';

        public const string RunbookTagsSeparatorString = ",";

        public const string Published = "Published";

        public const string Draft = "Draft";

        public const string AutomationServicePrefix = "OaasCS";

        public const string JobStartedByParameterName = "MicrosoftApplicationManagementStartedBy";

        // default schedule expiry time for daily schedule, consistent with UX
        // 12/31/9999 12:00:00 AM
        public static readonly DateTimeOffset DefaultScheduleExpiryTime = DateTimeOffset.MaxValue;

        public class AutomationAccountState
        {
            public const string Ready = "Ready";

            public const string Suspended = "Suspended";
        }

        public const string PsCommandConvertToJson = "ConvertTo-Json";

        public const string PsCommandConvertFromJson = "ConvertFrom-Json";

        public const string PsCommandParamInputObject = "InputObject";

        public const string PsCommandParamDepth = "Depth";

        public const int PsCommandValueDepth = 10;

        public const string ClientRequestIdHeaderName = "x-ms-client-request-id";

    }
}
