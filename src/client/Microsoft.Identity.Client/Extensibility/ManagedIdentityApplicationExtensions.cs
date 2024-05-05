﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Extensibility methods for <see cref="IManagedIdentityApplication"/>
    /// </summary>
    public static class ManagedIdentityApplicationExtensions
    {
        /// <summary>
        /// Used to determine if managed identity is able to handle claims.
        /// </summary>
        /// <returns>Boolean indicating if Claims is supported</returns>
        public static bool IsClaimsSupportedByClient(this IManagedIdentityApplication app)
        {
            if (app is ManagedIdentityApplication mia)
            {
                return mia.IsClaimsSupportedByClient();
            }

            return false;
        }
    }
}
