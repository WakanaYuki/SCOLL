using Microsoft.Win32;
using SCOLL.Classes.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace SCOLL.Classes.Logic
{
    public class MainWindowhelper : IMainWindowHelper
    {
        public IMainWindow main;
        public IErrorHandler errorHandler;
        public BitmapImage CurrentImage;

        public BitmapSource AsyncTempDownload;

        public BitmapImage GetImage(string uri)
        {
            Uri fileUri = new Uri(uri);
            try
            {
                CurrentImage = new BitmapImage(fileUri);
                return CurrentImage;
            }
            catch (System.NotSupportedException ex)
            {
                errorHandler.ShowNotification(Notifications.Wpf.NotificationType.Error, "Unsopported Filetype", "Couldn't load the file. Filetype is not supported by this program.");
            }
            return null;
        }

        //Open File Dialog for user to choose his Image
        public BitmapImage OpenNewImageDialog()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                Uri fileUri = new Uri(openFileDialog.FileName);
                try
                {
                    CurrentImage = new BitmapImage(fileUri);
                    return CurrentImage;
                }
                catch (System.NotSupportedException ex)
                {
                    errorHandler.ShowNotification(Notifications.Wpf.NotificationType.Error, "Unsopported Filetype", "Couldn't load the file. Filetype is not supported by this program.");
                }
            }
            return null;
        }

        public void SetWindow(MainWindow mw)
        {
            main = mw;
            errorHandler = mw;
        }

        public async Task<BitmapSource> GetImageFromWeb(string url)
        {
            Uri fileUri = new Uri(url);
            try
            {
                BitmapSource downlaodedImage = await AdvancedGetNewImageAsync(fileUri);
                return downlaodedImage;
            }
            catch (System.NotSupportedException ex)
            {
                errorHandler.ShowNotification(Notifications.Wpf.NotificationType.Error, "Unsopported Filetype", "Couldn't load the file. Filetype is not supported by this program.");
            }
            return null;
        }


        //Use this here when we have HTTP Support
        public static async Task<BitmapImage> GetNewImageAsync(Uri uri)
        {
            BitmapImage bitmap = null;
            var httpClient = new HttpClient();

            using (var response = await httpClient.GetAsync(uri))
            {
                if (response.IsSuccessStatusCode)
                {
                    using (var stream = new MemoryStream())
                    {
                        await response.Content.CopyToAsync(stream);
                        stream.Seek(0, SeekOrigin.Begin);

                        bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = stream;
                        bitmap.EndInit();
                        bitmap.Freeze();
                    }
                }
            }

            return bitmap;
        }

        //Shorter better version
        public static async Task<BitmapSource> AdvancedGetNewImageAsync(Uri uri)
        {
            BitmapSource bitmap = null;
            var httpClient = new HttpClient();

            using (var response = await httpClient.GetAsync(uri))
            {
                if (response.IsSuccessStatusCode)
                {
                    using (var stream = new MemoryStream())
                    {
                        await response.Content.CopyToAsync(stream);
                        stream.Seek(0, SeekOrigin.Begin);

                        bitmap = BitmapFrame.Create(
                            stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                    }
                }
            }

            return bitmap;
        }

        public string SaveFileDialog()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.DefaultExt = "leet";
            saveFileDialog.Filter = "SCOLL files (*.leet)|*.leet|All files (*.*)|*.*";
            saveFileDialog.ShowDialog();
            return saveFileDialog.FileName;
        }

        public InformationProp LoadFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "SCOLL files (*.leet)|*.leet|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                Uri fileUri = new Uri(openFileDialog.FileName);
                try
                {
                    IFormatter formatter = new BinaryFormatter();
                    Stream stream = new FileStream(openFileDialog.FileName, FileMode.Open, FileAccess.Read);
                    InformationProp objnew = (InformationProp)formatter.Deserialize(stream);
                    return objnew;
                }
                catch (System.NotSupportedException ex)
                {
                    errorHandler.ShowNotification(Notifications.Wpf.NotificationType.Error, "Unsopported Filetype", "Couldn't load the file. Filetype is not supported by this program.");
                }
            }
            return null;
        }
    }
}
