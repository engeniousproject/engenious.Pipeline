using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using engenious.Helper;

namespace engenious.Pipeline
{
    /// <summary>
    ///     Wrapper class for the ffmpeg command line utility.
    /// </summary>
    public class FFmpeg
    {
        private string _ffmpegExe;
        private readonly SynchronizationContext _syncContext;
        
        /// <summary>
        ///     Initializes a new instance of the <see cref="FFmpeg"/> class.
        /// </summary>
        /// <param name="syncContext">
        ///     The synchronization context used for UI. To be able to select ffmpeg executable with an open file dialog.
        /// </param>
        /// <param name="exePath">The path to the ffmpeg executable.</param>
        public FFmpeg(SynchronizationContext syncContext,string exePath)
        {
            _syncContext = syncContext;
            _ffmpegExe = exePath;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="FFmpeg"/> class.
        /// </summary>
        /// <param name="syncContext">
        ///     The synchronization context used for UI. To be able to select ffmpeg executable with an open file dialog.
        /// </param>
        public FFmpeg(SynchronizationContext syncContext)
            : this(syncContext,LocateFFmpegExe())
        {

        }
        
        private static string LocateFFmpegExe()
        {
            string completePath;
            
            try
            {
                completePath = File.ReadAllText(".ffmpeg");
                if (File.Exists(completePath))
                    return completePath;
            }
            catch
            {
                // ignored
            }

            string? path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string ext =string.Empty;
            var platform = PlatformHelper.RunningPlatform();
            switch (platform)
            {
                case Platform.Windows:
                    ext = ".exe";
                    break;
            }
            // ReSharper disable once AssignNullToNotNullAttribute
            completePath = path == null ? "ffmpeg" + ext : Path.Combine(path, "ffmpeg" + ext);
            if (File.Exists(completePath))
                return completePath;
            switch (platform)
            {
                case Platform.Linux:
                    completePath = Path.Combine("/usr/bin", "ffmpeg" + ext);
                    if (File.Exists(completePath))
                        return completePath;
                    break;
                case Platform.Mac:
                    completePath = Path.Combine("/Applications", "ffmpeg" + ext);
                    if (File.Exists(completePath))
                        return completePath;
                    break;
                case Platform.Windows:
                    var envPath = Environment.GetEnvironmentVariable("FFMPEG");
                    if (envPath != null && File.Exists(envPath))
                        return envPath;
                    break;
            }
            return "ffmpeg" + ext;
        }

        /// <summary>
        ///     Run the ffmpeg command with arguments.
        /// </summary>
        /// <param name="arguments">The arguments to pass to the ffmpeg command.</param>
        /// <param name="throwAll">Whether to throw all exceptions or not.</param>
        /// <returns>The started ffmpeg process.</returns>
        /// <exception cref="FileNotFoundException">Thrown when ffmpeg was not found.</exception>
        public Process RunCommand(string arguments, bool throwAll = false)
        {
            Process p = new();
            p.StartInfo = new ProcessStartInfo(_ffmpegExe, arguments);
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;

            try
            {
                if (p.Start())
                {

                    return p;
                }
            }
            catch (Win32Exception ex)
            {
                if (throwAll || ex.NativeErrorCode != 2) //File not found
                    throw new FileNotFoundException($"Could not find ffmpeg at location: '{_ffmpegExe}'.");
                
                _syncContext?.Send(o =>
                {
                    // TODO: implement new file dialog
                    /*using (var ofd = new OpenFileDialog())
                    {
                        ofd.Title = "FFmpeg";
                        ofd.FileName = _ffmpegExe;
                        ofd.Filter = "Executable files|" + (PlatformHelper.RunningPlatform() == Platform.Windows ? "*.exe" : "*.*");
                        if (ofd.ShowDialog() == DialogResult.OK)
                        {
                            _ffmpegExe = ofd.FileName;
                            File.WriteAllText(".ffmpeg", _ffmpegExe);
                            
                        }
                    }*/
                },null);
                if (File.Exists(_ffmpegExe))
                    return RunCommand(arguments, true);
                throw new FileNotFoundException($"Could not find ffmpeg at location: '{_ffmpegExe}'.");

            }
            return p;
        }
    }
}

