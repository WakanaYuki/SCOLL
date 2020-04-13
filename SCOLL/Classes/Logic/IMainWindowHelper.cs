using SCOLL.Classes.Properties;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace SCOLL.Classes.Logic
{
    public interface IMainWindowHelper
    {
        void SetWindow(MainWindow mw);
        BitmapImage GetImage(string uri);
        BitmapImage OpenNewImageDialog();
        Task<BitmapSource> GetImageFromWeb(string url);
        string SaveFileDialog();
        InformationProp LoadFile();
    }
}
