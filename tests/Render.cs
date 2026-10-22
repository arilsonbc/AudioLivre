using System;
using System.Drawing;
using System.Windows.Forms;
using AudioLivre;
class Render {
 [STAThread] static void Main(string[] args) {
  Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
  using(var form=new MainForm()) {
   form.ShowInTaskbar=false; form.StartPosition=FormStartPosition.Manual; form.Location=new Point(-2000,-2000);
   form.Show(); Application.DoEvents(); form.PerformLayout();
   using(var bitmap=new Bitmap(form.Width,form.Height)) { form.DrawToBitmap(bitmap,new Rectangle(0,0,form.Width,form.Height)); bitmap.Save(args[0],System.Drawing.Imaging.ImageFormat.Png); }
  }
 }
}
