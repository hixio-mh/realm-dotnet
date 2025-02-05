﻿////////////////////////////////////////////////////////////////////////////
//
// Copyright 2022 Realm Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License")
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
////////////////////////////////////////////////////////////////////////////

namespace Realms.Exceptions.Sync
{
    internal enum ClientError
    {
        /// <summary>
        /// A fatal error was encountered which prevents the completion of a client reset.
        /// </summary>
        AutoClientResetFailed = 132,
    }

    internal enum SessionErrorCategory : byte
    {
        ClientError = 0,
        SessionError = 1
    }

    internal enum ServerRequestsAction
    {
        NoAction = 0,
        ApplicationBug = 2,
        ClientReset = 6
    }
}
