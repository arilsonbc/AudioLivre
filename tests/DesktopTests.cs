using Avalonia;
using Avalonia.Headless;
using Avalonia.Threading;
using AudioLivre;

class DesktopTests {
 [STAThread] static void Main(string[] args) {
  AppBuilder.Configure<App>().UseHeadless(new AvaloniaHeadlessPlatformOptions()).SetupWithoutStarting();
  var window=new MainWindow { OutputFolder=args[2] }; window.Show();
  window.AddFiles(new[]{args[0],args[1],args[1]});
  if(window.Items.Count!=2) throw new Exception("Entradas duplicadas");
  var task=window.RunQueue(); var timeout=DateTime.UtcNow.AddSeconds(45);
  while(!task.IsCompleted && DateTime.UtcNow<timeout) { Dispatcher.UIThread.RunJobs(); Thread.Sleep(10); }
  if(!task.IsCompleted) throw new TimeoutException("Fila da interface excedeu 45 segundos");
  task.GetAwaiter().GetResult();
  if(!window.Items[0].State.StartsWith("Falhou") || window.Items[1].State!="Concluído") throw new Exception("Fila não continuou após falha: "+window.Status);
  if(window.Items[1].Percent!="100%") throw new Exception("Progresso incorreto");
  window.Close(); Console.WriteLine("PASS interface: duplicatas, fila após erro, progresso e conclusão");
 }
}
