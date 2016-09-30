using System;
using System.Threading.Tasks;
using System.IO;

namespace engenious.Pipeline
{
    public class ffmpeg
    {
        private string ffmpegExe;
        public ffmpeg()
            :this(locateFFmpegExe())
        {

        }
        private static string locateFFmpegExe()
        {
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string ext="",searchPath="";
            var platform = PlatformHelper.RunningPlatform();
            switch(platform)
            {
                case Platform.Windows:
                    ext=".exe";
                    break;
            }
            string completePath=System.IO.Path.Combine(path,"ffmpeg"+ext);
            if (System.IO.File.Exists(completePath))
                return completePath;
            switch(platform)
            {
                case Platform.Linux:
                    completePath =System.IO.Path.Combine("/usr/bin","ffmpeg"+ext);
                    if (System.IO.File.Exists(completePath))
                        return completePath;
                    break;
                case Platform.Mac:
                    completePath = System.IO.Path.Combine("/Applications","ffmpeg"+ext);
                    if (System.IO.File.Exists(completePath))
                        return completePath;
                    break;
            }
            return "ffmpeg" + ext;
        }
        public ffmpeg(string exePath)
        {
            ffmpegExe = exePath;
        }
        public System.Diagnostics.Process RunCommand(string arguments)
        {
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo = new System.Diagnostics.ProcessStartInfo(ffmpegExe,arguments);
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            FileStream outputStream = p.StandardOutput.BaseStream as FileStream;
            if (p.Start())
            {
                return p;
                
            }
            return null;
        }
    }
}

