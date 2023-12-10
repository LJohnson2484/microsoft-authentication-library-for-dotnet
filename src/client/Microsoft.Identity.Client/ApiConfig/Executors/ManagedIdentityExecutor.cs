﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.ApiConfig.Executors
{
#if !SUPPORTS_CONFIDENTIAL_CLIENT
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide managed identity flow on mobile
#endif
    internal class ManagedIdentityExecutor : AbstractExecutor, IManagedIdentityApplicationExecutor
    {
        private readonly ManagedIdentityApplication _managedIdentityApplication;

        public ManagedIdentityExecutor(IServiceBundle serviceBundle, ManagedIdentityApplication managedIdentityApplication)
            : base(serviceBundle)
        {
            ClientApplicationBase.GuardMobileFrameworks();

            _managedIdentityApplication = managedIdentityApplication;
        }

        public async Task<AuthenticationResult> ExecuteAsync(
            AcquireTokenCommonParameters commonParameters,
            AcquireTokenForManagedIdentityParameters managedIdentityParameters,
            CancellationToken cancellationToken)
        {
            var requestContext = CreateRequestContextAndLogVersionInfo(commonParameters.CorrelationId, cancellationToken);

            if (_managedIdentityApplication.KeyMaterialManager.CryptoKeyType != CryptoKeyType.None)
            {
                // Check resource format 
                if (Uri.TryCreate(managedIdentityParameters.Resource, UriKind.Absolute, out Uri resourceUri) &&
                    (resourceUri.Scheme == Uri.UriSchemeHttp || resourceUri.Scheme == Uri.UriSchemeHttps) &&
                    !managedIdentityParameters.Resource.EndsWith("/.default", StringComparison.OrdinalIgnoreCase))
                {
                    // Add "/.default" to the scopes
                    commonParameters.Scopes = new SortedSet<string>
                    {
                        managedIdentityParameters.Resource + "/.default"
                    };
                }

                requestContext.Logger.Info(LogMessages.CredentialScopeUpdated);
            }

            var requestParams = await _managedIdentityApplication.CreateRequestParametersAsync(
                commonParameters,
                requestContext,
                _managedIdentityApplication.AppTokenCacheInternal).ConfigureAwait(false);

            // MSI factory logic - decide if we need to use the legacy or the new MSI flow

            RequestBase handler = null;

            // May or may not be initialized, depending on the state of the machine
            handler = CredentialBasedMsiAuthRequest.TryCreate(
                ServiceBundle,
                requestParams,
                managedIdentityParameters);

            handler ??= new LegacyMsiAuthRequest(
                    ServiceBundle,
                    requestParams,
                    managedIdentityParameters);

            return await handler.RunAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
