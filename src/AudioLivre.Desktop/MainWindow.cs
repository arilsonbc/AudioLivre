using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace AudioLivre;
public class QueueItem : INotifyPropertyChanged {
    public string Path { get; init; }
    public string Name => System.IO.Path.GetFileName(Path);
    string state="Na fila",percent="—";
    public string State { get=>state; set { state=value; PropertyChanged?.Invoke(this,new(nameof(State))); } }
    public string Percent { get=>percent; set { percent=value; PropertyChanged?.Invoke(this,new(nameof(Percent))); } }
    public string Details { get; set; }
    public event PropertyChangedEventHandler PropertyChanged;
}
public class MainWindow : Window {
    public ObservableCollection<QueueItem> Items { get; }=new();
    readonly ListBox files=new() { SelectionMode=SelectionMode.Multiple };
    readonly ComboBox format=new() { ItemsSource=new[]{"MP3","WAV","FLAC","M4A","Opus","OGG"},SelectedIndex=0,HorizontalAlignment=HorizontalAlignment.Stretch };
    readonly ComboBox quality=new() { ItemsSource=new[]{"Econômica","Padrão (recomendada)","Alta"},SelectedIndex=1,HorizontalAlignment=HorizontalAlignment.Stretch };
    readonly TextBox folder=new();
    readonly TextBlock status=new() { Text="Pronto. Adicione arquivos para começar.",TextWrapping=TextWrapping.Wrap };
    readonly TextBlock hint=new() { Text="Arraste vídeos ou áudios para a lista • A primeira faixa de áudio será usada",TextWrapping=TextWrapping.Wrap };
    readonly ProgressBar progress=new() { Minimum=0,Maximum=100,Height=6 };
    readonly Button add=new() { Content="+ Adicionar arquivos" },remove=new() { Content="Remover" },clear=new() { Content="Limpar fila" },browse=new() { Content="Escolher…" },convert=new() { Content="Converter" },cancel=new() { Content="Cancelar",IsEnabled=false },open=new() { Content="Abrir pasta" };
    readonly Engine engine=new(AppContext.BaseDirectory);
    CancellationTokenSource cancellation; bool running;
    public string OutputFolder { get=>folder.Text; set=>folder.Text=value; }
    public string Status=>status.Text;
    public MainWindow() {
        Title="ÁudioLivre · Conversor de áudio"; Width=920; Height=740; MinWidth=760; MinHeight=660;
        Background=Brush.Parse("#F6F8FB"); Foreground=Brush.Parse("#1D2737");
        WindowStartupLocation=WindowStartupLocation.CenterScreen;
        var root=new Grid { RowDefinitions=new("Auto,Auto,*,Auto,Auto,Auto,Auto,Auto,Auto"),Margin=new Thickness(26),RowSpacing=14 };
        Content=root;
        var heading=new StackPanel { Spacing=4 };
        heading.Children.Add(new TextBlock { Text="ÁudioLivre",FontSize=30,FontWeight=FontWeight.Bold });
        heading.Children.Add(new TextBlock { Text="Seu vídeo vira áudio. Simples, local e sem complicação.",Foreground=Brush.Parse("#636F80") });
        Put(root,heading,0);
        Put(root,new StackPanel { Orientation=Orientation.Horizontal,Spacing=8,Children={add,remove,clear} },1);
        files.ItemsSource=Items;
        files.ItemTemplate=new FuncDataTemplate<QueueItem>((item,scope)=> {
            var row=new Grid { ColumnDefinitions=new("*,200,85"),Margin=new Thickness(6),MinHeight=30 };
            var name=new TextBlock { Text=item.Name,TextTrimming=TextTrimming.CharacterEllipsis,VerticalAlignment=VerticalAlignment.Center };
            ToolTip.SetTip(name,item.Path); row.Children.Add(name);
            var state=new TextBlock { VerticalAlignment=VerticalAlignment.Center }; state.Bind(TextBlock.TextProperty,new Binding(nameof(QueueItem.State))); Grid.SetColumn(state,1); row.Children.Add(state);
            var percent=new TextBlock { VerticalAlignment=VerticalAlignment.Center }; percent.Bind(TextBlock.TextProperty,new Binding(nameof(QueueItem.Percent))); Grid.SetColumn(percent,2); row.Children.Add(percent);
            return row;
        });
        var table=new Grid { RowDefinitions=new("Auto,*"),Background=Brushes.White };
        var header=new Grid { ColumnDefinitions=new("*,200,85"),Margin=new Thickness(14,8) };
        foreach(var pair in new[]{("Arquivo",0),("Situação",1),("Progresso",2)}) { var label=new TextBlock { Text=pair.Item1,FontWeight=FontWeight.SemiBold }; Grid.SetColumn(label,pair.Item2); header.Children.Add(label); }
        table.Children.Add(header); Grid.SetRow(files,1); table.Children.Add(files);
        Put(root,new Border { BorderBrush=Brush.Parse("#D2DAE6"),BorderThickness=new Thickness(1),Child=table },2);
        hint.FontSize=12; hint.Foreground=Brush.Parse("#636F80"); Put(root,hint,3);
        var options=new Grid { ColumnDefinitions=new("2*,3*"),ColumnSpacing=18 };
        options.Children.Add(Field("Formato de saída",format)); var q=Field("Qualidade",quality); Grid.SetColumn(q,1); options.Children.Add(q); Put(root,options,4);
        var destination=new Grid { ColumnDefinitions=new("*,Auto"),ColumnSpacing=8 };
        destination.Children.Add(folder); Grid.SetColumn(browse,1); destination.Children.Add(browse); Put(root,Field("Salvar em",destination),5);
        string music=Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        folder.Text=Path.Combine(String.IsNullOrWhiteSpace(music)?Environment.GetFolderPath(Environment.SpecialFolder.UserProfile):music,"ÁudioLivre");
        Put(root,progress,6); status.FontSize=12; Put(root,status,7);
        convert.Background=Brush.Parse("#2463DC"); convert.Foreground=Brushes.White;
        Put(root,new StackPanel { Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,Spacing=8,Children={open,cancel,convert} },8);
        add.Click+=async(_,_)=> {
            try {
                var selected=await StorageProvider.OpenFilePickerAsync(new() { Title="Adicionar vídeos ou áudios",AllowMultiple=true,FileTypeFilter=new[]{new FilePickerFileType("Vídeos e áudios") { Patterns=new[]{"*.mp4","*.mkv","*.mov","*.avi","*.webm","*.wmv","*.m4v","*.mpeg","*.mpg","*.mts","*.ts","*.mp3","*.wav","*.flac","*.m4a","*.aac","*.ogg","*.opus","*.wma"} },FilePickerFileTypes.All} });
                AddFiles(selected.Select(x=>x.TryGetLocalPath()).Where(x=>x!=null));
            } catch(Exception ex) { await Error(ex.Message); }
        };
        browse.Click+=async(_,_)=> { try { var selected=await StorageProvider.OpenFolderPickerAsync(new() { Title="Pasta dos áudios convertidos",AllowMultiple=false }); if(selected.Count>0 && selected[0].TryGetLocalPath() is string path) folder.Text=path; } catch(Exception ex) { await Error(ex.Message); } };
        remove.Click+=(_,_)=> { foreach(var item in files.SelectedItems.Cast<QueueItem>().ToArray()) Items.Remove(item); };
        clear.Click+=(_,_)=>Items.Clear();
        format.SelectionChanged+=(_,_)=> { UpdateQuality(); hint.Text=(string)format.SelectedItem=="WAV"?"WAV sem compressão • Arquivos maiores, sem recuperar qualidade perdida na origem":(string)format.SelectedItem=="FLAC"?"FLAC sem perda • Preserva o áudio decodificado da origem":"Arraste vídeos ou áudios para a lista • A primeira faixa de áudio será usada"; };
        convert.Click+=async(_,_)=>await RunQueue();
        cancel.Click+=(_,_)=> { cancellation?.Cancel(); cancel.IsEnabled=false; status.Text="Cancelando com segurança…"; };
        open.Click+=async(_,_)=> { try { if(!Directory.Exists(folder.Text)) { await Error("A pasta será criada na primeira conversão."); return; } Process.Start(new ProcessStartInfo { FileName=Path.GetFullPath(folder.Text),UseShellExecute=true }); } catch(Exception ex) { await Error(ex.Message); } };
        files.DoubleTapped+=async(_,_)=> { if(files.SelectedItem is QueueItem item) await Error(item.Details??item.Path); };
        DragDrop.SetAllowDrop(files,true);
        files.AddHandler(DragDrop.DragOverEvent,(_,e)=>e.DragEffects=!running&&e.Data.Contains(DataFormats.Files)?DragDropEffects.Copy:DragDropEffects.None);
        files.AddHandler(DragDrop.DropEvent,(_,e)=> { if(!running) AddFiles(e.Data.GetFiles()?.Select(x=>x.TryGetLocalPath()).Where(x=>x!=null)??Array.Empty<string>()); });
        Closing+=(_,e)=> { if(running) { e.Cancel=true; cancellation?.Cancel(); status.Text="Cancelando… Aguarde a conclusão para fechar."; } };
    }
    static StackPanel Field(string title,Control control)=>new() { Spacing=6,Children={new TextBlock { Text=title },control} };
    static void Put(Grid root,Control child,int row) { Grid.SetRow(child,row); root.Children.Add(child); }
    void UpdateQuality()=>quality.IsEnabled=!running && (string)format.SelectedItem!="WAV" && (string)format.SelectedItem!="FLAC";
    void Busy(bool value) { running=value; foreach(var c in new Control[]{add,remove,clear,browse,format,folder,convert,open}) c.IsEnabled=!value; cancel.IsEnabled=value; UpdateQuality(); }
    async Task Error(string text) {
        var ok=new Button { Content="OK",HorizontalAlignment=HorizontalAlignment.Right };
        var dialog=new Window { Title="ÁudioLivre",Width=560,SizeToContent=SizeToContent.Height,CanResize=false,WindowStartupLocation=WindowStartupLocation.CenterOwner,Content=new StackPanel { Margin=new Thickness(20),Spacing=16,Children={new TextBlock { Text=text,TextWrapping=TextWrapping.Wrap,MaxHeight=350 },ok} } };
        ok.Click+=(_,_)=>dialog.Close(); await dialog.ShowDialog(this);
    }
    public void AddFiles(IEnumerable<string> paths) {
        var comparer=OperatingSystem.IsWindows()?StringComparer.OrdinalIgnoreCase:StringComparer.Ordinal;
        foreach(string path in paths) { if(!File.Exists(path)) continue; string full=Path.GetFullPath(path); if(!Items.Any(x=>comparer.Equals(x.Path,full))) Items.Add(new() { Path=full }); }
        status.Text=Items.Count+" arquivo(s) na lista. Arquivos existentes serão preservados.";
    }
    public async Task RunQueue() {
        if(running) return;
        if(Items.Count==0) { await Error("Adicione pelo menos um vídeo ou áudio."); return; }
        string target;
        try { engine.Check(); if(String.IsNullOrWhiteSpace(folder.Text)) throw new Exception("Escolha uma pasta de saída."); target=Path.GetFullPath(folder.Text); Directory.CreateDirectory(target); folder.Text=target; }
        catch(Exception ex) { await Error(ex.Message); return; }
        var items=Items.Where(x=>x.State!="Concluído").ToArray();
        if(items.Length==0) { status.Text="Todos os arquivos já foram convertidos. Limpe a fila para converter novamente."; return; }
        string selectedFormat=(string)format.SelectedItem; int selectedQuality=quality.SelectedIndex,ok=0,failed=0,done=0;
        cancellation=new(); var token=cancellation.Token; Busy(true); progress.Value=0;
        try {
            foreach(var item in items) {
                if(token.IsCancellationRequested) break;
                item.State="Convertendo…"; item.Percent="0%"; status.Text=$"Convertendo {done+1} de {items.Length}: {item.Name}";
                int index=done;
                try {
                    string result=await Task.Run(()=>engine.ConvertFile(item.Path,target,selectedFormat,selectedQuality,token,p=>Dispatcher.UIThread.Post(()=> { if(item.State!="Convertendo…") return; item.Percent=p+"%"; progress.Value=(index*100+p)/items.Length; })));
                    item.State="Concluído"; item.Percent="100%"; item.Details=result; ok++;
                } catch(OperationCanceledException) { item.State="Cancelado"; item.Percent="—"; break; }
                catch(Exception ex) { item.State="Falhou · detalhes"; item.Percent="—"; item.Details=ex.Message; failed++; }
                done++; progress.Value=done*100/items.Length;
            }
            status.Text=(token.IsCancellationRequested?"Cancelado. ":"Finalizado. ")+$"{ok} concluído(s), {failed} falha(s)."+(failed>0?" Clique duas vezes na falha para ver detalhes.":"");
        } finally { Busy(false); cancellation.Dispose(); cancellation=null; }
    }
}
