namespace SendGridClickTrackTLSProxy.Extensions;

using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using SendGridClickTrackTLSProxy.Diagnostics;

public static class KestrelTlsExtensions
{
    public static WebApplicationBuilder ConfigureKestrelWithTls(this WebApplicationBuilder builder)
    {
        var tlsSection = builder.Configuration.GetSection("Kestrel:Tls");
        var enableTls = tlsSection.GetValue<bool>("Enabled");

        if (!enableTls)
        {
            return builder;
        }

        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            var httpsPort = tlsSection.GetValue("HttpsPort", 443);
            var httpPort = tlsSection.GetValue("HttpPort", 80);
            var redirectHttpToHttps = tlsSection.GetValue("RedirectHttpToHttps", true);

            // Configure HTTP endpoint (if not redirecting or explicit port specified)
            if (!redirectHttpToHttps || httpPort != 0)
            {
                serverOptions.ListenAnyIP(httpPort);
            }

            // Configure HTTPS endpoint
            serverOptions.ListenAnyIP(httpsPort, listenOptions =>
            {
                ConfigureHttpsCertificate(listenOptions, tlsSection);
            });
        });

        // Configure HTTPS redirection
        var redirectHttpToHttps = tlsSection.GetValue("RedirectHttpToHttps", true);
        if (redirectHttpToHttps)
        {
            builder.Services.AddHttpsRedirection(options =>
            {
                options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
                options.HttpsPort = tlsSection.GetValue("HttpsPort", 443);
            });
        }

        return builder;
    }
    private static void ConfigureHttpsCertificate(ListenOptions listenOptions, IConfigurationSection tlsSection)
    {
        var certFile = tlsSection["CertificateFile"];
        var keyFile = tlsSection["KeyFile"];
        var certPath = tlsSection["CertificatePath"]; // For PFX files
        var certPasswordEnvVar = tlsSection["CertificatePasswordEnvVar"];

        try
        {
            // Option 1: PEM certificate and key files with manual loading
            if (!string.IsNullOrEmpty(certFile) && !string.IsNullOrEmpty(keyFile))
            {
                if (!File.Exists(certFile))
                    throw new FileNotFoundException($"Certificate file not found: {certFile}");
                if (!File.Exists(keyFile))
                    throw new FileNotFoundException($"Key file not found: {keyFile}");

                // Run diagnostics in debug/development mode
#if DEBUG
                CertificateDiagnostics.DiagnoseCertificateFiles(certFile, keyFile);
#endif

                // Method A: Use X509Certificate2.CreateFromPemFile with persistent key
                try
                {
                    var certificate = X509Certificate2.CreateFromPemFile(certFile, keyFile);

                    if (!certificate.HasPrivateKey)
                    {
                        throw new InvalidOperationException("Certificate loaded but private key not associated");
                    }

                    // Make the private key persistent to avoid ephemeral key issues
                    var persistentCert = CreatePersistentCertificate(certificate);
                    certificate.Dispose(); // Dispose the original

                    ValidateCertificate(persistentCert);
                    listenOptions.UseHttps(persistentCert);
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load certificate using CreateFromPemFile: {ex.Message}");

                    // Method B: Manual PEM parsing with RSA key handling (fallback)
                    try
                    {
                        var certPem = File.ReadAllText(certFile);
                        var keyPem = File.ReadAllText(keyFile);

                        X509Certificate2 certificate;

                        // Check if it's an RSA private key format
                        if (keyPem.Contains("-----BEGIN RSA PRIVATE KEY-----"))
                        {
                            // Convert RSA key to PKCS#8 format
                            using var rsa = RSA.Create();
                            rsa.ImportFromPem(keyPem);
                            var pkcs8Key = rsa.ExportPkcs8PrivateKey();
                            var pkcs8Pem = Convert.ToBase64String(pkcs8Key);
                            var formattedKey = $"-----BEGIN PRIVATE KEY-----\n{pkcs8Pem}\n-----END PRIVATE KEY-----";

                            certificate = X509Certificate2.CreateFromPem(certPem, formattedKey);
                        }
                        else
                        {
                            certificate = X509Certificate2.CreateFromPem(certPem, keyPem);
                        }

                        if (!certificate.HasPrivateKey)
                        {
                            throw new InvalidOperationException("Certificate loaded but private key not associated");
                        }

                        // Make the private key persistent
                        var persistentCert = CreatePersistentCertificate(certificate);
                        certificate.Dispose(); // Dispose the original

                        ValidateCertificate(persistentCert);
                        listenOptions.UseHttps(persistentCert);
                        return;
                    }
                    catch (Exception fallbackEx)
                    {
                        throw new InvalidOperationException(
                            $"Both certificate loading methods failed. Primary: {ex.Message}, Fallback: {fallbackEx.Message}");
                    }
                }
            }

            // Option 2: PFX file with optional password
            if (!string.IsNullOrEmpty(certPath))
            {
                if (!File.Exists(certPath))
                    throw new FileNotFoundException($"Certificate file not found: {certPath}");

                var password = !string.IsNullOrEmpty(certPasswordEnvVar)
                    ? Environment.GetEnvironmentVariable(certPasswordEnvVar)
                    : null;

                var certificate = !string.IsNullOrEmpty(password)
                    ? new X509Certificate2(certPath, password)
                    : new X509Certificate2(certPath);

                listenOptions.UseHttps(certificate);
                return;
            }

            // Fallback: Development certificate (not recommended for production)
            Console.WriteLine("Warning: Using development certificate. Not recommended for production.");
            listenOptions.UseHttps();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to configure HTTPS certificate: {ex.Message}", ex);
        }
    }

    private static X509Certificate2 CreatePersistentCertificate(X509Certificate2 certificate)
    {
        try
        {
            // Method 1: Try user key store first (no admin rights needed)
            var pfxBytes = certificate.Export(X509ContentType.Pkcs12);
            var persistentCert = new X509Certificate2(pfxBytes, (string?)null,
                X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.PersistKeySet);
            return persistentCert;
        }
        catch (CryptographicException)
        {
            try
            {
                // Method 2: Try exportable flag without persistence
                var pfxBytes = certificate.Export(X509ContentType.Pkcs12);
                var persistentCert = new X509Certificate2(pfxBytes, (string?)null,
                    X509KeyStorageFlags.Exportable);
                return persistentCert;
            }
            catch (CryptographicException)
            {
                // Method 3: Create a new certificate with the same key material but different storage
                return CreateCertificateWithNonEphemeralKey(certificate);
            }
        }
    }

    private static X509Certificate2 CreateCertificateWithNonEphemeralKey(X509Certificate2 certificate)
    {
        // Get the key algorithm
        var publicKey = certificate.GetRSAPublicKey() ?? certificate.GetECDsaPublicKey() as AsymmetricAlgorithm;

        if (certificate.GetRSAPrivateKey() is RSA rsaPrivateKey)
        {
            // For RSA keys, create a new certificate with the same key material
            var rsaKey = RSA.Create();
            rsaKey.ImportParameters(rsaPrivateKey.ExportParameters(true));

            // Create a new certificate with the persistent key
            var certWithKey = certificate.CopyWithPrivateKey(rsaKey);
            return certWithKey;
        }
        else if (certificate.GetECDsaPrivateKey() is ECDsa ecdsaPrivateKey)
        {
            // For ECDSA keys
            var ecdsaKey = ECDsa.Create();
            ecdsaKey.ImportParameters(ecdsaPrivateKey.ExportParameters(true));

            var certWithKey = certificate.CopyWithPrivateKey(ecdsaKey);
            return certWithKey;
        }

        // If we can't determine the key type, return the original
        return certificate;
    }

    private static void ValidateCertificate(X509Certificate2 certificate)
    {
        Console.WriteLine($"Certificate Subject: {certificate.Subject}");
        Console.WriteLine($"Certificate Issuer: {certificate.Issuer}");
        Console.WriteLine($"Certificate Valid From: {certificate.NotBefore}");
        Console.WriteLine($"Certificate Valid To: {certificate.NotAfter}");
        Console.WriteLine($"Has Private Key: {certificate.HasPrivateKey}");

        if (!certificate.HasPrivateKey)
        {
            throw new InvalidOperationException("Certificate does not have an associated private key");
        }
    }

}
