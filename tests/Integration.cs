using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using AudioLivre;
class Integration {
 static int count;
 static void Assert(bool value,string message) { if(!value) throw new Exception(message); count++; Console.WriteLine("PASS "+message); }
 [STAThread] static void Main(string[] args) {
  string root=args[0],input=args[1],silent=args[2],dest=args[3]; Directory.CreateDirectory(dest);
  var engine=new Engine(root); engine.Check(); var info=engine.Probe(input,CancellationToken.None);
  Assert(info.Audio && info.Duration>=2.9,"Leitura de vídeo com espaço e acento");
  foreach(string format in new[]{"MP3","WAV","FLAC","M4A","Opus","OGG"}) {
   for(int q=0;q<(format=="WAV"||format=="FLAC"?1:3);q++) {
    int last=0; string result=engine.ConvertFile(input,dest,format,q,CancellationToken.None,p=>last=p);
    var audio=engine.Probe(result,CancellationToken.None);
    Assert(audio.Audio && Math.Abs(audio.Duration-info.Duration)<0.2 && last==100,format+" perfil "+q+" duração e progresso");
    using(var probe=Process.Start(Engine.StartInfo(engine.Ffprobe,"-v error -select_streams a:0 -show_entries stream=codec_name -of default=nw=1:nk=1 "+Engine.Quote(result)))) {
     string codec=probe.StandardOutput.ReadToEnd().Trim(); probe.WaitForExit();
     string expectedCodec=format=="MP3"?"mp3":format=="WAV"?"pcm_s16le":format=="M4A"?"aac":format=="OGG"?"vorbis":format.ToLowerInvariant();
     Assert(codec==expectedCodec,format+" codec confirmado pelo ffprobe");
    }
   }
  }
  string expected=Path.Combine(dest,Path.GetFileNameWithoutExtension(input)+".wav"); byte[] before=File.ReadAllBytes(expected);
  string extra=engine.ConvertFile(input,dest,"WAV",1,CancellationToken.None,p=>{});
  Assert(extra!=expected && Convert.ToBase64String(before)==Convert.ToBase64String(File.ReadAllBytes(expected)),"Não sobrescreve arquivos existentes");
  bool rejected=false; try { engine.ConvertFile(silent,dest,"MP3",1,CancellationToken.None,p=>{}); } catch(Exception ex) { rejected=ex.Message.Contains("não contém"); }
  Assert(rejected,"Rejeita vídeo sem áudio");
  string bad=Path.Combine(dest,"corrompido.mp4"); File.WriteAllText(bad,"isto não é um vídeo"); rejected=false;
  try { engine.ConvertFile(bad,dest,"MP3",1,CancellationToken.None,p=>{}); } catch(Exception) { rejected=true; }
  Assert(rejected,"Rejeita mídia corrompida");
  using(var cts=new CancellationTokenSource()) {
   rejected=false; try { engine.ConvertFile(input,dest,"MP3",1,cts.Token,p=>{ if(p<100) cts.Cancel(); }); } catch(OperationCanceledException) { rejected=true; }
   Assert(rejected,"Cancelamento durante o processamento");
  }
  Assert(Directory.GetFiles(dest,".audiolivre-*").Length==0,"Limpa arquivos temporários após cancelamento");
  Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
  using(var form=new MainForm()) {
   form.ShowInTaskbar=false; form.StartPosition=FormStartPosition.Manual; form.Location=new System.Drawing.Point(-2000,-2000); form.Show();
   var flags=BindingFlags.Instance|BindingFlags.NonPublic;
   ((TextBox)typeof(MainForm).GetField("folder",flags).GetValue(form)).Text=dest;
   typeof(MainForm).GetMethod("AddFiles",flags).Invoke(form,new object[]{new[]{silent,bad,input,input}});
   var list=(ListView)typeof(MainForm).GetField("files",flags).GetValue(form);
   Assert(list.Items.Count==3,"Interface evita duplicação de entradas");
   var queue=(Task)typeof(MainForm).GetMethod("RunQueue",flags).Invoke(form,new object[0]);
   var deadline=DateTime.UtcNow.AddSeconds(45);
   while(!queue.IsCompleted && DateTime.UtcNow<deadline) { Application.DoEvents(); Thread.Sleep(10); }
   Assert(queue.IsCompleted && !queue.IsFaulted,"Fila da interface termina sem travar");
   Assert(list.Items[0].SubItems[1].Text.StartsWith("Falhou") && list.Items[1].SubItems[1].Text.StartsWith("Falhou") && list.Items[2].SubItems[1].Text=="Concluído","Fila continua após duas falhas e converte o próximo arquivo");
   form.Close();
  }
  Console.WriteLine("TOTAL "+count+" verificações aprovadas");
 }
}
