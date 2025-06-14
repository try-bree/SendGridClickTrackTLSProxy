using System.Security.Cryptography.X509Certificates;

namespace SendGridClickTrackTLSProxy.Diagnostics;

public static class CertificateDiagnostics
{
    public static void DiagnoseCertificateFiles(string certFile, string keyFile)
    {
        Console.WriteLine("=== Certificate File Analysis ===");

        if (!File.Exists(certFile))
        {
            Console.WriteLine($"❌ Certificate file not found: {certFile}");
            return;
        }

        if (!File.Exists(keyFile))
        {
            Console.WriteLine($"❌ Key file not found: {keyFile}");
            return;
        }

        var certContent = File.ReadAllText(certFile);
        var keyContent = File.ReadAllText(keyFile);

        Console.WriteLine($"📄 Certificate file: {certFile}");
        Console.WriteLine($"📄 Key file: {keyFile}");

        // Check certificate format
        var certCount = CountCertificates(certContent);
        Console.WriteLine($"🔍 Certificates found in cert file: {certCount}");

        // Check key format
        var keyType = DetectKeyType(keyContent);
        Console.WriteLine($"🔑 Private key type: {keyType}");

        // Try different loading methods
        TestCertificateLoading(certFile, keyFile, certContent, keyContent);
    }

    private static int CountCertificates(string pemContent)
    {
        return System.Text.RegularExpressions.Regex.Matches(pemContent, @"-----BEGIN CERTIFICATE-----").Count;
    }

    private static string DetectKeyType(string keyContent)
    {
        if (keyContent.Contains("-----BEGIN PRIVATE KEY-----"))
            return "PKCS#8 Private Key";
        if (keyContent.Contains("-----BEGIN RSA PRIVATE KEY-----"))
            return "RSA Private Key";
        if (keyContent.Contains("-----BEGIN EC PRIVATE KEY-----"))
            return "EC Private Key";
        if (keyContent.Contains("-----BEGIN ENCRYPTED PRIVATE KEY-----"))
            return "Encrypted Private Key";
        return "Unknown/Invalid";
    }

    private static void TestCertificateLoading(string certFile, string keyFile, string certContent, string keyContent)
    {
        Console.WriteLine("\n=== Loading Method Tests ===");

        // Method 1: Direct file path (what you're currently using)
        try
        {
            // This is what Kestrel does internally
            var cert1 = X509Certificate2.CreateFromPemFile(certFile, keyFile);
            Console.WriteLine($"✅ CreateFromPemFile: Success - HasPrivateKey: {cert1.HasPrivateKey}");
            PrintCertDetails(cert1, "Method 1");
            cert1.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ CreateFromPemFile: {ex.Message}");
        }

        // Method 2: From string content
        try
        {
            var cert2 = X509Certificate2.CreateFromPem(certContent, keyContent);
            Console.WriteLine($"✅ CreateFromPem (string): Success - HasPrivateKey: {cert2.HasPrivateKey}");
            PrintCertDetails(cert2, "Method 2");
            cert2.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ CreateFromPem (string): {ex.Message}");
        }

        // Method 3: Load cert only (to see if the problem is key association)
        try
        {
            var cert3 = X509Certificate2.CreateFromPem(certContent);
            Console.WriteLine($"✅ Certificate only: Success - HasPrivateKey: {cert3.HasPrivateKey}");
            PrintCertDetails(cert3, "Method 3");
            cert3.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Certificate only: {ex.Message}");
        }
    }

    private static void PrintCertDetails(X509Certificate2 cert, string method)
    {
        Console.WriteLine($"   [{method}] Subject: {cert.Subject}");
        Console.WriteLine($"   [{method}] Thumbprint: {cert.Thumbprint}");
        Console.WriteLine($"   [{method}] Valid: {cert.NotBefore:yyyy-MM-dd} to {cert.NotAfter:yyyy-MM-dd}");
        Console.WriteLine($"   [{method}] Key Algorithm: {cert.PublicKey.Oid.FriendlyName}");

        if (cert.HasPrivateKey)
        {
            try
            {
                var keySize = cert.GetRSAPublicKey()?.KeySize ?? cert.GetECDsaPublicKey()?.KeySize ?? 0;
                Console.WriteLine($"   [{method}] Key Size: {keySize} bits");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   [{method}] Key Size: Unable to determine ({ex.Message})");
            }
        }
    }
}