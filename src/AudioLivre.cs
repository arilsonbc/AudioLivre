using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace AudioLivre {
    public class MediaInfo { public double Duration; public bool Audio; }
    public class Engine {
        public readonly string Ffmpeg, Ffprobe;
        public Engine(string root) { Ffmpeg=Path.Combine(root,"ffmpeg.exe"); Ffprobe=Path.Combine(root,"ffprobe.exe"); }
        public void Check() { if(!File.Exists(Ffmpeg)||!File.Exists(Ffprobe)) throw new Exception("FFmpeg não encontrado. Extraia todos os arquivos do ZIP para a mesma pasta."); }
        public static string Quote(string value) {
            // CommandLineToArgvW quoting: preserve trailing slashes and embedded quotes.
            var b=new StringBuilder("\""); int slashes=0;
            foreach(char c in value) { if(c=='\\') { slashes++; continue; } if(c=='\"') { b.Append('\\',slashes*2+1); b.Append(c); slashes=0; continue; } b.Append('\\',slashes); slashes=0; b.Append(c); }
            b.Append('\\',slashes*2); return b.Append('"').ToString();
        }
        public static ProcessStartInfo StartInfo(string exe,string args) { return new ProcessStartInfo(exe,args) { UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8 }; }
        static void Kill(Process p) { try { if(!p.HasExited) p.Kill(); } catch(InvalidOperationException) {} catch(System.ComponentModel.Win32Exception) {} }
        public MediaInfo Probe(string input,CancellationToken token) {
            token.ThrowIfCancellationRequested();
            using(var p=new Process { StartInfo=StartInfo(Ffprobe,"-v error -show_entries format=duration:stream=codec_type,duration -of json "+Quote(input)) }) {
                p.Start(); using(token.Register(()=>Kill(p))) {
                    Task<string> output=p.StandardOutput.ReadToEndAsync(), error=p.StandardError.ReadToEndAsync();
                    if(!p.WaitForExit(30000)) { Kill(p); throw new Exception("Tempo esgotado ao ler o arquivo."); }
                    p.WaitForExit(); Task.WaitAll(output,error); token.ThrowIfCancellationRequested();
                    if(p.ExitCode!=0) throw new Exception("Não foi possível ler este arquivo. Ele pode estar danificado ou em formato incompatível.");
                    var json=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(output.Result);
                    var info=new MediaInfo(); object raw;
                    if(json.TryGetValue("format",out raw)) { var f=raw as Dictionary<string,object>; if(f!=null && f.TryGetValue("duration",out raw)) double.TryParse(Convert.ToString(raw,CultureInfo.InvariantCulture),NumberStyles.Float,CultureInfo.InvariantCulture,out info.Duration); }
                    if(json.TryGetValue("streams",out raw)) foreach(object s in (System.Collections.IEnumerable)raw) { var stream=s as Dictionary<string,object>; object type; if(stream!=null && stream.TryGetValue("codec_type",out type) && Convert.ToString(type)=="audio") info.Audio=true; }
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

    public class MainForm : Form {
        readonly ListView files=new ListView();
        readonly ComboBox format=new ComboBox(), quality=new ComboBox();
        readonly TextBox folder=new TextBox();
        readonly Label status=new Label(),hint=new Label();
        readonly ProgressBar progress=new ProgressBar();
        readonly Button add=new Button(),remove=new Button(),clear=new Button(),browse=new Button(),convert=new Button(),cancel=new Button(),open=new Button();
        CancellationTokenSource cancellation; bool running; readonly Engine engine;
        readonly Color ink=Color.FromArgb(29,39,55),muted=Color.FromArgb(99,111,128),blue=Color.FromArgb(36,99,220);
        public MainForm() {
            engine=new Engine(AppDomain.CurrentDomain.BaseDirectory);
            Text="ÁudioLivre · Conversor de áudio"; Size=new Size(920,740); MinimumSize=new Size(760,660); StartPosition=FormStartPosition.CenterScreen;
            Font=new Font("Segoe UI",10); BackColor=Color.FromArgb(246,248,251); ForeColor=ink; AutoScaleMode=AutoScaleMode.Dpi;
            var layout=new TableLayoutPanel { Dock=DockStyle.Fill,Padding=new Padding(26),ColumnCount=1,RowCount=9 };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,80)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute,46)); layout.RowStyles.Add(new RowStyle(SizeType.Percent,100)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute,34)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute,85)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute,70)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute,32)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute,44)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute,52));
            Controls.Add(layout);
            var heading=new Panel { Dock=DockStyle.Fill };
            heading.Controls.Add(new Label { Text="ÁudioLivre",Font=new Font("Segoe UI",24,FontStyle.Bold),AutoSize=true,Location=new Point(0,0) });
            heading.Controls.Add(new Label { Text="Seu vídeo vira áudio. Simples, local e sem complicação.",AutoSize=true,ForeColor=muted,Location=new Point(2,48) }); layout.Controls.Add(heading,0,0);
            var tools=new FlowLayoutPanel { Dock=DockStyle.Fill,WrapContents=false };
            ButtonStyle(add,"+ Adicionar arquivos",180,false); ButtonStyle(remove,"Remover",110,false); ButtonStyle(clear,"Limpar fila",110,false); tools.Controls.AddRange(new Control[]{add,remove,clear}); layout.Controls.Add(tools,0,1);
            files.Dock=DockStyle.Fill; files.View=View.Details; files.FullRowSelect=true; files.HideSelection=false; files.BorderStyle=BorderStyle.FixedSingle; files.Columns.Add("Arquivo",410); files.Columns.Add("Situação",190); files.Columns.Add("Progresso",100); files.AllowDrop=true; files.ShowItemToolTips=true; layout.Controls.Add(files,0,2);
            files.Resize+=(s,e)=> { files.Columns[0].Width=Math.Max(230,files.ClientSize.Width-300); };
            hint.Text="Arraste vídeos ou áudios para a lista • A primeira faixa de áudio será usada"; hint.Dock=DockStyle.Fill; hint.ForeColor=muted; hint.TextAlign=ContentAlignment.MiddleLeft; layout.Controls.Add(hint,0,3);
            var settings=new TableLayoutPanel { Dock=DockStyle.Fill,ColumnCount=2 }; settings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,40)); settings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,60)); settings.RowStyles.Add(new RowStyle(SizeType.Absolute,26)); settings.RowStyles.Add(new RowStyle(SizeType.Absolute,35));
            settings.Controls.Add(new Label { Text="Formato de saída",AutoSize=true },0,0); settings.Controls.Add(new Label { Text="Qualidade",AutoSize=true },1,0);
            format.DropDownStyle=quality.DropDownStyle=ComboBoxStyle.DropDownList; format.Items.AddRange(new object[]{"MP3","WAV","FLAC","M4A","Opus","OGG"}); quality.Items.AddRange(new object[]{"Econômica","Padrão (recomendada)","Alta"}); format.Dock=quality.Dock=DockStyle.Fill; format.Margin=new Padding(0,0,18,0); quality.Margin=new Padding(0); settings.Controls.Add(format,0,1); settings.Controls.Add(quality,1,1); layout.Controls.Add(settings,0,4); quality.SelectedIndex=1; format.SelectedIndex=0;
            format.SelectedIndexChanged+=(s,e)=> { quality.Enabled=!running && format.Text!="WAV" && format.Text!="FLAC"; hint.Text=format.Text=="WAV"?"WAV sem compressão • Arquivos maiores, sem recuperar qualidade perdida na origem":format.Text=="FLAC"?"FLAC sem perda • Preserva o áudio decodificado da origem":"Arraste vídeos ou áudios para a lista • A primeira faixa de áudio será usada"; };
            var destination=new TableLayoutPanel { Dock=DockStyle.Fill,ColumnCount=2 }; destination.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); destination.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,110)); destination.RowStyles.Add(new RowStyle(SizeType.Absolute,26)); destination.RowStyles.Add(new RowStyle(SizeType.Absolute,36)); destination.Controls.Add(new Label { Text="Salvar em",AutoSize=true },0,0); folder.Dock=DockStyle.Fill; folder.Text=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),"ÁudioLivre"); destination.Controls.Add(folder,0,1); ButtonStyle(browse,"Escolher…",100,false); browse.Dock=DockStyle.Fill; destination.Controls.Add(browse,1,1); layout.Controls.Add(destination,0,5);
            progress.Dock=DockStyle.Fill; progress.Margin=new Padding(0,8,0,8); layout.Controls.Add(progress,0,6);
            status.Text="Pronto. Adicione arquivos para começar."; status.Dock=DockStyle.Fill; status.ForeColor=muted; status.AutoEllipsis=true; layout.Controls.Add(status,0,7);
            var actions=new FlowLayoutPanel { Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft,WrapContents=false }; ButtonStyle(convert,"Converter",145,true); ButtonStyle(cancel,"Cancelar",110,false); ButtonStyle(open,"Abrir pasta",120,false); cancel.Enabled=false; actions.Controls.AddRange(new Control[]{convert,cancel,open}); layout.Controls.Add(actions,0,8);
            add.Click+=(s,e)=> { using(var d=new OpenFileDialog { Multiselect=true,Title="Adicionar vídeos ou áudios",Filter="Mídia|*.mp4;*.mkv;*.mov;*.avi;*.webm;*.wmv;*.m4v;*.mpeg;*.mpg;*.mts;*.ts;*.mp3;*.wav;*.flac;*.m4a;*.aac;*.ogg;*.opus;*.wma|Todos os arquivos|*.*" }) if(d.ShowDialog()==DialogResult.OK) AddFiles(d.FileNames); };
            files.DragEnter+=(s,e)=> { if(!running && e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect=DragDropEffects.Copy; };
            files.DragDrop+=(s,e)=> { if(!running) AddFiles((string[])e.Data.GetData(DataFormats.FileDrop)); };
            remove.Click+=(s,e)=> { foreach(ListViewItem item in files.SelectedItems) files.Items.Remove(item); };
            clear.Click+=(s,e)=>files.Items.Clear();
            browse.Click+=(s,e)=> { using(var d=new FolderBrowserDialog { Description="Pasta dos áudios convertidos",SelectedPath=folder.Text }) if(d.ShowDialog()==DialogResult.OK) folder.Text=d.SelectedPath; };
            open.Click+=(s,e)=> { try { if(Directory.Exists(folder.Text)) Process.Start("explorer.exe",Engine.Quote(folder.Text)); else MessageBox.Show(this,"A pasta será criada na primeira conversão.","Pasta de saída"); } catch(Exception ex) { ShowError(ex.Message); } };
            convert.Click+=async(s,e)=>await RunQueue(); cancel.Click+=(s,e)=> { if(cancellation!=null) { cancellation.Cancel(); cancel.Enabled=false; status.Text="Cancelando com segurança…"; } };
            files.DoubleClick+=(s,e)=> { if(files.SelectedItems.Count>0 && !String.IsNullOrEmpty(files.SelectedItems[0].ToolTipText)) MessageBox.Show(this,files.SelectedItems[0].ToolTipText,"Detalhes do arquivo"); };
            FormClosing+=(s,e)=> { if(running) { e.Cancel=true; if(cancellation!=null) cancellation.Cancel(); status.Text="Cancelando… Aguarde a conclusão para fechar."; } };
        }
        void ButtonStyle(Button b,string text,int width,bool primary) { b.Text=text; b.Width=width; b.Height=36; b.FlatStyle=FlatStyle.Flat; b.FlatAppearance.BorderSize=primary?0:1; b.FlatAppearance.BorderColor=Color.FromArgb(210,218,230); b.BackColor=primary?blue:Color.White; b.ForeColor=primary?Color.White:ink; b.Cursor=Cursors.Hand; b.Margin=new Padding(0,0,8,0); }
        void AddFiles(IEnumerable<string> paths) { foreach(string path in paths) { if(!File.Exists(path)) continue; string full=Path.GetFullPath(path); if(files.Items.Cast<ListViewItem>().Any(x=>String.Equals((string)x.Tag,full,StringComparison.OrdinalIgnoreCase))) continue; var item=new ListViewItem(Path.GetFileName(full)); item.Tag=full; item.SubItems.Add("Na fila"); item.SubItems.Add("—"); item.ToolTipText=full; files.Items.Add(item); } status.Text=files.Items.Count+" arquivo(s) na lista. Arquivos existentes serão preservados."; }
        void ShowError(string text) { MessageBox.Show(this,text,"ÁudioLivre",MessageBoxButtons.OK,MessageBoxIcon.Warning); }
        void Busy(bool value) { running=value; foreach(Control c in new Control[]{add,remove,clear,browse,format,quality,folder,convert,open}) c.Enabled=!value; quality.Enabled=!value && format.Text!="WAV" && format.Text!="FLAC"; cancel.Enabled=value; }
        async Task RunQueue() {
            if(files.Items.Count==0) { ShowError("Adicione pelo menos um vídeo ou áudio."); return; }
            string target;
            try { engine.Check(); if(String.IsNullOrWhiteSpace(folder.Text)) throw new Exception("Escolha uma pasta de saída."); target=Path.GetFullPath(folder.Text); Directory.CreateDirectory(target); folder.Text=target; }
            catch(Exception ex) { ShowError(ex.Message); return; }
            var items=files.Items.Cast<ListViewItem>().Where(x=>x.SubItems[1].Text!="Concluído").ToArray();
            if(items.Length==0) { status.Text="Todos os arquivos já foram convertidos. Limpe a fila para converter novamente."; return; }
            string selectedFormat=format.Text; int selectedQuality=quality.SelectedIndex; int ok=0,failed=0,done=0;
            cancellation=new CancellationTokenSource(); var token=cancellation.Token; Busy(true); progress.Value=0;
            try {
                foreach(var item in items) {
                    if(token.IsCancellationRequested) break;
                    item.SubItems[1].Text="Convertendo…"; item.SubItems[2].Text="0%"; status.Text="Convertendo "+(done+1)+" de "+items.Length+": "+item.Text;
                    var report=new Progress<int>(percent=> { item.SubItems[2].Text=percent+"%"; progress.Value=Math.Min(100,(done*100+percent)/items.Length); });
                    try { string result=await Task.Run(()=>engine.ConvertFile((string)item.Tag,target,selectedFormat,selectedQuality,token,p=>((IProgress<int>)report).Report(p))); item.SubItems[1].Text="Concluído"; item.SubItems[2].Text="100%"; item.ToolTipText=result; ok++; }
                    catch(OperationCanceledException) { item.SubItems[1].Text="Cancelado"; item.SubItems[2].Text="—"; break; }
                    catch(Exception ex) { item.SubItems[1].Text="Falhou · detalhes"; item.SubItems[2].Text="—"; item.ToolTipText=ex.Message; failed++; }
                    done++; progress.Value=done*100/items.Length;
                }
                status.Text=(token.IsCancellationRequested?"Cancelado. ":"Finalizado. ")+ok+" concluído(s), "+failed+" falha(s)."+(failed>0?" Clique duas vezes na falha para ver detalhes.":"");
            } finally { Busy(false); cancellation.Dispose(); cancellation=null; }
        }
    }
    static class Program {
        [STAThread] static int Main(string[] args) {
            if(args.Length>0 && args[0]=="--convert") {
                try { if(args.Length<5) return 2; var e=new Engine(AppDomain.CurrentDomain.BaseDirectory); e.ConvertFile(args[1],args[2],args[3],int.Parse(args[4]),CancellationToken.None,p=>{}); return 0; } catch(Exception ex) { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"conversion-error.log"),ex.ToString()); return 1; }
            }
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); Application.Run(new MainForm()); return 0;
        }
    }
}
