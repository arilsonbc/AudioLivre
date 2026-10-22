using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
namespace AudioLivre;
    public class MediaInfo { public double Duration; public bool Audio; }
    public class Engine {
        public readonly string Ffmpeg, Ffprobe;
        public Engine(string root) { Ffmpeg=FindTool(root,"ffmpeg"); Ffprobe=FindTool(root,"ffprobe"); }
        static string FindTool(string root,string name) {
            string file=name+(OperatingSystem.IsWindows()?".exe":"");
            string local=Path.Combine(root,file);
            if(File.Exists(local)) return local;
            foreach(string dir in (Environment.GetEnvironmentVariable("PATH")??"").Split(Path.PathSeparator)) {
                if(String.IsNullOrWhiteSpace(dir)) continue;
                string candidate=Path.Combine(dir,file);
                if(File.Exists(candidate)) return candidate;
            }
            return local;
        }
        public void Check() { if(!File.Exists(Ffmpeg)||!File.Exists(Ffprobe)) throw new Exception("FFmpeg não encontrado. No Windows, extraia todo o ZIP. No Linux, instale ffmpeg pelo gerenciador de pacotes ou coloque ffmpeg e ffprobe ao lado do aplicativo."); }
        public static string Quote(string value) {
            // CommandLineToArgvW quoting: preserve trailing slashes and embedded quotes.
            var b=new StringBuilder("\""); int slashes=0;
            foreach(char c in value) { if(c=='\\') { slashes++; continue; } if(c=='\"') { b.Append('\\',slashes*2+1); b.Append(c); slashes=0; continue; } b.Append('\\',slashes); slashes=0; b.Append(c); }
            b.Append('\\',slashes*2); return b.Append('"').ToString();
        }
        public static ProcessStartInfo StartInfo(string exe,string args) { return new ProcessStartInfo(exe,args) { UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8 }; }
        static void Kill(Process p) { try { if(!p.HasExited) p.Kill(entireProcessTree:true); } catch(InvalidOperationException) {} catch(System.ComponentModel.Win32Exception) {} }
        public MediaInfo Probe(string input,CancellationToken token) {
            token.ThrowIfCancellationRequested();
            using(var p=new Process { StartInfo=StartInfo(Ffprobe,"-v error -show_entries format=duration:stream=codec_type,duration -of json "+Quote(input)) }) {
                p.Start(); using(token.Register(()=>Kill(p))) {
                    Task<string> output=p.StandardOutput.ReadToEndAsync(), error=p.StandardError.ReadToEndAsync();
                    if(!p.WaitForExit(30000)) { Kill(p); throw new Exception("Tempo esgotado ao ler o arquivo."); }
                    p.WaitForExit(); Task.WaitAll(output,error); token.ThrowIfCancellationRequested();
                    if(p.ExitCode!=0) throw new Exception("Não foi possível ler este arquivo. Ele pode estar danificado ou em formato incompatível.");
                    using var json=JsonDocument.Parse(output.Result);
                    var info=new MediaInfo();
                    if(json.RootElement.TryGetProperty("format",out var f) && f.TryGetProperty("duration",out var d))
                        double.TryParse(d.GetString(),NumberStyles.Float,CultureInfo.InvariantCulture,out info.Duration);
                    if(json.RootElement.TryGetProperty("streams",out var streams))
                        foreach(var stream in streams.EnumerateArray())
                            if(stream.TryGetProperty("codec_type",out var type) && type.GetString()=="audio") info.Audio=true;
                    return info;
                }
            }
        }
        public static string Codec(string format,int quality) {
            switch(format) {
                case "MP3": return "-c:a libmp3lame -b:a "+new[]{"128k","192k","320k"}[quality];
                case "WAV": return "-c:a pcm_s16le";
                case "FLAC": return "-c:a flac -compression_level 5";
                case "M4A": return "-c:a aac -b:a "+new[]{"128k","192k","256k"}[quality]+" -movflags +faststart";
                case "Opus": return "-c:a libopus -b:a "+new[]{"64k","128k","192k"}[quality];
                case "OGG": return "-c:a libvorbis -q:a "+new[]{"3","5","8"}[quality];
                default: throw new Exception("Formato desconhecido.");
            }
        }
        public string ConvertFile(string input,string folder,string format,int quality,CancellationToken token,Action<int> progress) {
            Check(); var info=Probe(input,token); if(!info.Audio) throw new Exception("Este arquivo não contém uma faixa de áudio.");
            Directory.CreateDirectory(folder);
            string stem=Path.GetFileNameWithoutExtension(input),ext="."+format.ToLowerInvariant();
            string temp=Path.Combine(folder,".audiolivre-"+Guid.NewGuid().ToString("N")+ext);
            var errors=new StringBuilder();
            try {
                string args="-hide_banner -nostdin -v error -n -i "+Quote(input)+" -map 0:a:0 -vn -sn -dn "+Codec(format,quality)+" -progress pipe:1 -nostats "+Quote(temp);
                using(var p=new Process { StartInfo=StartInfo(Ffmpeg,args) }) {
                    p.Start(); using(token.Register(()=>Kill(p))) {
                        var err=Task.Run(async delegate { string errorLine; while((errorLine=await p.StandardError.ReadLineAsync())!=null) { if(errors.Length<12000) errors.AppendLine(errorLine); } });
                        string line;
                        while((line=p.StandardOutput.ReadLine())!=null) {
                            if(line.StartsWith("out_time_us=")) { double us; if(info.Duration>0 && double.TryParse(line.Substring(12),NumberStyles.Float,CultureInfo.InvariantCulture,out us)) progress(Math.Max(0,Math.Min(99,(int)(us/1000000/info.Duration*100)))); }
                        }
                        p.WaitForExit(); err.Wait(); token.ThrowIfCancellationRequested();
                        if(p.ExitCode!=0 || !File.Exists(temp) || new FileInfo(temp).Length==0) throw new Exception("Falha na conversão. "+errors.ToString().Trim());
                    }
                }
                // File.Move is atomic on this volume and never overwrites an existing file.
                for(int n=0;;n++) {
                    token.ThrowIfCancellationRequested();
                    string target=Path.Combine(folder,stem+(n==0?"":" ("+n+")")+ext);
                    try { File.Move(temp,target); progress(100); return target; }
                    catch(IOException) { if(!File.Exists(target)) throw; }
                }
            } finally { if(File.Exists(temp)) File.Delete(temp); }
        }
    }

