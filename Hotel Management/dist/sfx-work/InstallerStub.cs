using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows.Forms;

internal static class Program
{
    private const string Marker = "HMSETUP1";
    private const string AppName = "Hotel Management";
    private const string ExeName = "Hotel Management.exe";
    private const string ConfigName = "Hotel Management.exe.config";

    [STAThread]
    private static void Main()
    {
        try
        {
            if (!IsDotNet48OrNewer())
            {
                MessageBox.Show(
                    ".NET Framework 4.8+ is required. Please install it and run setup again.",
                    "Hotel Management Setup",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            var installerExe = Application.ExecutablePath;
            var payloadZip = ExtractEmbeddedPayload(installerExe);
            if (string.IsNullOrWhiteSpace(payloadZip) || !File.Exists(payloadZip))
            {
                MessageBox.Show("Setup payload was not found.", "Hotel Management Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var installDir = ResolveInstallDir();
            Directory.CreateDirectory(installDir);

            var extractedDir = Path.Combine(Path.GetTempPath(), "hm_extract_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(extractedDir);
            ZipFile.ExtractToDirectory(payloadZip, extractedDir);

            string backupConfig = null;
            var existingConfig = Path.Combine(installDir, ConfigName);
            if (File.Exists(existingConfig))
            {
                backupConfig = Path.Combine(Path.GetTempPath(), "hm_cfg_" + Guid.NewGuid().ToString("N") + ".config");
                File.Copy(existingConfig, backupConfig, true);
            }

            CopyDirectory(extractedDir, installDir);

            if (!string.IsNullOrWhiteSpace(backupConfig) && File.Exists(backupConfig))
            {
                File.Copy(backupConfig, existingConfig, true);
                TryDeleteFile(backupConfig);
            }

            var appExe = Path.Combine(installDir, ExeName);
            if (!File.Exists(appExe))
            {
                MessageBox.Show("Install failed: app executable not found after extraction.", "Hotel Management Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            TryDeleteFile(payloadZip);
            TryDeleteDirectory(extractedDir);

            MessageBox.Show(
                "Install completed successfully.\nLocation: " + installDir,
                "Hotel Management Setup",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Installer failed: " + ex.Message, "Hotel Management Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static bool IsDotNet48OrNewer()
    {
        return GetReleaseFromKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full") >= 528040
            || GetReleaseFromKey(@"SOFTWARE\WOW6432Node\Microsoft\NET Framework Setup\NDP\v4\Full") >= 528040;
    }

    private static int GetReleaseFromKey(string keyPath)
    {
        try
        {
            using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(keyPath))
            {
                if (key == null) return 0;
                var value = key.GetValue("Release");
                if (value == null) return 0;
                return Convert.ToInt32(value);
            }
        }
        catch
        {
            return 0;
        }
    }

    private static string ResolveInstallDir()
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var preferred = Path.Combine(programFiles, AppName);
        try
        {
            Directory.CreateDirectory(preferred);
            return preferred;
        }
        catch
        {
            var fallback = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs",
                AppName);
            Directory.CreateDirectory(fallback);
            return fallback;
        }
    }

    private static string ExtractEmbeddedPayload(string installerExePath)
    {
        var markerBytes = System.Text.Encoding.ASCII.GetBytes(Marker);
        using (var fs = new FileStream(installerExePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            if (fs.Length < markerBytes.Length + 8) return null;

            fs.Seek(-(markerBytes.Length + 8), SeekOrigin.End);

            var markerTail = new byte[markerBytes.Length];
            ReadExactly(fs, markerTail, 0, markerTail.Length);
            if (!markerTail.SequenceEqual(markerBytes)) return null;

            var lengthBytes = new byte[8];
            ReadExactly(fs, lengthBytes, 0, 8);
            var payloadLength = BitConverter.ToInt64(lengthBytes, 0);
            if (payloadLength <= 0) return null;

            var payloadStart = fs.Length - markerBytes.Length - 8 - payloadLength;
            if (payloadStart < 0) return null;

            var zipPath = Path.Combine(Path.GetTempPath(), "hm_payload_" + Guid.NewGuid().ToString("N") + ".zip");
            fs.Seek(payloadStart, SeekOrigin.Begin);
            using (var outStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                CopyBytes(fs, outStream, payloadLength);
            }
            return zipPath;
        }
    }

    private static void ReadExactly(Stream stream, byte[] buffer, int offset, int count)
    {
        var readTotal = 0;
        while (readTotal < count)
        {
            var read = stream.Read(buffer, offset + readTotal, count - readTotal);
            if (read <= 0) throw new EndOfStreamException();
            readTotal += read;
        }
    }

    private static void CopyBytes(Stream input, Stream output, long count)
    {
        var buffer = new byte[81920];
        long remaining = count;
        while (remaining > 0)
        {
            var read = input.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
            if (read <= 0) throw new EndOfStreamException();
            output.Write(buffer, 0, read);
            remaining -= read;
        }
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        Directory.CreateDirectory(destinationDir);

        foreach (var dir in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relative = dir.Substring(sourceDir.Length).TrimStart(Path.DirectorySeparatorChar);
            Directory.CreateDirectory(Path.Combine(destinationDir, relative));
        }

        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relative = file.Substring(sourceDir.Length).TrimStart(Path.DirectorySeparatorChar);
            var target = Path.Combine(destinationDir, relative);
            var targetDir = Path.GetDirectoryName(target);
            if (!string.IsNullOrWhiteSpace(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }
            File.Copy(file, target, true);
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
        catch
        {
        }
    }
}
