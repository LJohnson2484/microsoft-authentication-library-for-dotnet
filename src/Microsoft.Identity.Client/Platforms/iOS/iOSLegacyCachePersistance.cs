﻿//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using Foundation;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Security;

namespace Microsoft.Identity.Client.Platforms.iOS
{
    internal class iOSLegacyCachePersistence : ILegacyCachePersistence
    {
        private const string NAME = "ADAL.PCL.iOS";
        private const string LocalSettingsContainerName = "ActiveDirectoryAuthenticationLibrary";

        private string _keychainGroup;
        private readonly ICoreLogger _logger;

        public iOSLegacyCachePersistence(ICoreLogger logger)
        {
            _logger = logger;
        }

        private string GetBundleId()
        {
            return NSBundle.MainBundle.BundleIdentifier;
        }

        public void SetKeychainSecurityGroup(string keychainSecurityGroup)
        {
            _keychainGroup = keychainSecurityGroup ?? GetBundleId();
        }

        byte[] ILegacyCachePersistence.LoadCache()
        {
            try
            {
                SecStatusCode res;
                var rec = new SecRecord(SecKind.GenericPassword)
                {
                    Generic = NSData.FromString(LocalSettingsContainerName),
                    Accessible = SecAccessible.Always,
                    Service = NAME + " Service",
                    Account = NAME + " cache",
                    Label = NAME + " Label",
                    Comment = NAME + " Cache",
                    Description = "Storage for cache"
                };

                if (_keychainGroup != null)
                {
                    rec.AccessGroup = _keychainGroup;
                }

                var match = SecKeyChain.QueryAsRecord(rec, out res);
                if (res == SecStatusCode.Success && match != null && match.ValueData != null)
                {
                    return match.ValueData.ToArray();

                }
            }
            catch (Exception ex)
            {
                _logger.WarningPiiWithPrefix(ex, "Failed to load adal cache: ");
                // Ignore as the cache seems to be corrupt
            }
            return null;
        }

        void ILegacyCachePersistence.WriteCache(byte[] serializedCache)
        {
            try
            {
                var s = new SecRecord(SecKind.GenericPassword)
                {
                    Generic = NSData.FromString(LocalSettingsContainerName),
                    Accessible = SecAccessible.Always,
                    Service = NAME + " Service",
                    Account = NAME + " cache",
                    Label = NAME + " Label",
                    Comment = NAME + " Cache",
                    Description = "Storage for cache"
                };

                if (_keychainGroup != null)
                {
                    s.AccessGroup = _keychainGroup;
                }

                var err = SecKeyChain.Remove(s);
                if (err != SecStatusCode.Success)
                {
                    string msg = "Failed to remove adal cache record: ";
                    _logger.WarningPii(msg + err, msg);
                }

                if (serializedCache != null && serializedCache.Length > 0)
                {
                    s.ValueData = NSData.FromArray(serializedCache);
                    err = SecKeyChain.Add(s);
                    if (err != SecStatusCode.Success)
                    {
                        string msg = "Failed to save adal cache record: ";
                        _logger.WarningPii(msg + err, msg);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WarningPiiWithPrefix(ex, "Failed to save adal cache: ");
            }
        }
    }
}
