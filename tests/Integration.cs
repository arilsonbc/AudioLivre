using System;
using System.IO;
using System.Threading;
using AudioLivre;
class Integration {
 static int count;
 static void Assert(bool value,string message) { if(!value) throw new Exception(message); count++; Console.WriteLine("PASS "+message); }
 static void Main(string[] args) {
  string root=args[0],input=args[1],silent=args[2],dest=args[3]; Directory.CreateDirectory(dest);
  var engine=new Engine(root); engine.Check(); var info=engine.Probe(input,CancellationToken.None);
  Assert(info.Audio && info.Duration>=2.9,"Leitura de vídeo com espaço e acento");
  foreach(string format in new[]{"MP3","WAV","FLAC","M4A","Opus","OGG"}) {
   for(int q=0;q<(format=="WAV"||format=="FLAC"?1:3);q++) {
    int last=0; string result=engine.ConvertFile(input,dest,format,q,CancellationToken.None,p=>last=p);
    var audio=engine.Probe(result,CancellationToken.None);
    Assert(audio.Audio && Math.Abs(audio.Duration-info.Duration)<0.2 && last==100,format+" perfil "+q+" duração e progresso");
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
  Console.WriteLine("TOTAL "+count+" verificações aprovadas");
 }
}
