﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.AuthScheme.CDT
{
    //TODO: Add support for ECD keys
    internal class CdtCryptoProvider : ICdtCryptoProvider
    {
        private readonly X509Certificate2 _cert;

        public CdtCryptoProvider(X509Certificate2 cert)
        {
            _cert = cert ?? throw new ArgumentNullException(nameof(cert));

            RSA provider = _cert.GetRSAPublicKey();
            RSAParameters publicKeyParams = provider.ExportParameters(false);
            CannonicalPublicKeyJwk = ComputeCanonicalJwk(publicKeyParams);
        }

        public byte[] Sign(byte[] payload)
        {
            using (RSA key = _cert.GetRSAPrivateKey())
            {
                return key.SignData(
                    payload,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pss);
            }
        }

        public string CannonicalPublicKeyJwk { get; }

        public string CryptographicAlgorithm { get => "PS256"; }

        /// <summary>
        /// Creates the canonical representation of the JWK.  See https://tools.ietf.org/html/rfc7638#section-3
        /// The number of parameters as well as the lexicographic order is important, as this string will be hashed to get a thumbprint
        /// </summary>
        private static string ComputeCanonicalJwk(RSAParameters rsaPublicKey)
        {
            return $@"{{""e"":""{Base64UrlHelpers.Encode(rsaPublicKey.Exponent)}"",""kty"":""RSA"",""n"":""{Base64UrlHelpers.Encode(rsaPublicKey.Modulus)}""}}";
        }
    }
}
