// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    public sealed partial class RSACng : RSA
    {
        // CngKeyBlob formats for RSA key blobs
        private static readonly CngKeyBlobFormat s_rsaFullPrivateBlob =
            new CngKeyBlobFormat(Interop.BCrypt.KeyBlobType.BCRYPT_RSAFULLPRIVATE_BLOB);

        private static readonly CngKeyBlobFormat s_rsaPrivateBlob =
            new CngKeyBlobFormat(Interop.BCrypt.KeyBlobType.BCRYPT_RSAPRIVATE_BLOB);

        private static readonly CngKeyBlobFormat s_rsaPublicBlob =
            new CngKeyBlobFormat(Interop.BCrypt.KeyBlobType.BCRYPT_RSAPUBLIC_KEY_BLOB);

        private void ImportKeyBlob(ReadOnlySpan<byte> rsaBlob, bool includePrivate)
        {
            CngKeyBlobFormat blobFormat = includePrivate ? s_rsaPrivateBlob : s_rsaPublicBlob;

            CngKey newKey = CngKey.Import(rsaBlob, blobFormat);
            try
            {
                newKey.ExportPolicy |= CngExportPolicies.AllowPlaintextExport;
                Key = newKey;
            }
            catch
            {
                newKey.Dispose();
                throw;
            }
        }

        private void AcceptImport(CngPkcs8.Pkcs8Response response)
        {
            try
            {
                Key = response.Key;
            }
            catch
            {
                response.FreeKey();
                throw;
            }
        }

        private byte[] ExportKeyBlob(bool includePrivateParameters)
        {
            return Key.Export(includePrivateParameters ? s_rsaFullPrivateBlob : s_rsaPublicBlob);
        }

        public override bool TryExportPkcs8PrivateKey(Span<byte> destination, out int bytesWritten)
        {
            bool encryptedOnlyExport = CngPkcs8.AllowsOnlyEncryptedExport(Key);

            if (encryptedOnlyExport)
            {
                const string TemporaryExportPassword = "DotnetExportPhrase";
                byte[] exported = ExportEncryptedPkcs8(TemporaryExportPassword, 1);
                RSAKeyFormatHelper.ReadEncryptedPkcs8(
                    exported,
                    TemporaryExportPassword,
                    out _,
                    out RSAParameters rsaParameters);
                return RSAKeyFormatHelper.WritePkcs8PrivateKey(rsaParameters).TryEncode(destination, out bytesWritten);
            }

            return Key.TryExportKeyBlob(
                Interop.NCrypt.NCRYPT_PKCS8_PRIVATE_KEY_BLOB,
                destination,
                out bytesWritten);
        }

        private byte[] ExportEncryptedPkcs8(ReadOnlySpan<char> pkcs8Password, int kdfCount)
        {
            return Key.ExportPkcs8KeyBlob(pkcs8Password, kdfCount);
        }

        private bool TryExportEncryptedPkcs8(
            ReadOnlySpan<char> pkcs8Password,
            int kdfCount,
            Span<byte> destination,
            out int bytesWritten)
        {
            return Key.TryExportPkcs8KeyBlob(
                pkcs8Password,
                kdfCount,
                destination,
                out bytesWritten);
        }
    }
}
