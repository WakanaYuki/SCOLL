using Notifications.Wpf;
using SCOLL.Classes.Logic;
using SCOLL.Classes.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SCOLL
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : IMainWindow, IErrorHandler
    {
        //Everything ScrollView related is from this Article: https://www.codeproject.com/Articles/97871/WPF-simple-zoom-and-drag-support-in-a-ScrollViewer
        Point? lastCenterPositionOnTarget;
        Point? lastMousePositionOnTarget;
        Point? lastDragPoint;

        //Drag Code
        private bool _isMoving;
        private Point? _buttonPosition;
        private double deltaX;
        private double deltaY;
        private TranslateTransform _currentTT;
        private int currentLabelIndex;
        private Stopwatch stopWatch;

        private List<System.Drawing.FontFamily> fonts;

        private string lastSaveDir;
        private string currentSave;
        private System.Drawing.Bitmap currentBitmap;

        Nullable<Point> dragStart = null;

        public List<LabelHelper> text_on_image;

        Point mouseLocation;

        //List<Label> labels_on_image;

        public NotificationManager notificationManager;

        public InformationProp information;

        //Interfact to Helper
        public IMainWindowHelper mwh;
        public MainWindow()
        {
            string uri = new Uri("C:/Users/Justin/Documents/SCOLL/SCOLL/assets/icons/new.png").ToString();
            information = new InformationProp();
            //Load Interface
            mwh = new MainWindowhelper();
            mwh.SetWindow(this);
            InitializeComponent();
            HideStartContent();
            text_on_image = new List<LabelHelper>();

            //Initialize Scroller
            cnt_scrollviewer.ScrollChanged += Oncnt_scrollviewerScrollChanged;
            cnt_scrollviewer.MouseLeftButtonUp += OnMouseRightButtonUp;
            cnt_scrollviewer.PreviewMouseRightButtonUp += OnMouseRightButtonUp;
            cnt_scrollviewer.PreviewMouseWheel += OnPreviewMouseWheel;

            cnt_scrollviewer.PreviewMouseRightButtonDown += OnMouseRightButtonDown;
            cnt_scrollviewer.MouseMove += OnMouseMove;

            //Initialize Label Creator
            cnt_image.PreviewMouseLeftButtonDown += ImageLeftMosueButtonDown;
            cnt_image.PreviewMouseLeftButtonUp += ImageLeftMosueButtonUp;
            cnt_image.MouseLeftButtonUp += ImageLeftMosueButtonUp;

            cnt_grd_parent.PreviewMouseMove += Label_OnPreviewMouseMove;

            cnt_slider.ValueChanged += OnSliderValueChanged;

            //Initialize Notification Manager
            notificationManager = new NotificationManager();

            //Get all available fonts
            GetFonts();
        }

        private void GetFonts()
        {
            fonts = new List<System.Drawing.FontFamily>();

            foreach (System.Drawing.FontFamily font in System.Drawing.FontFamily.Families)
            {
                fonts.Add(font);
                hed_main_cb_label_font.Items.Add(font.Name);
            }
        }

        private void Reset()
        {
            cnt_labels.Children.Clear();
            text_on_image.Clear();
            currentBitmap = null;
        }

        //Source: https://stackoverflow.com/a/46382687 and https://markheath.net/post/how-to-drag-shapes-on-canvas-in-wpf and https://stackoverflow.com/a/5660293/12571327
        private void Label_OnPreviewMouseUp(object sender, MouseEventArgs e)
        {
            if (_isMoving)
            {
                //revert to current object position
                dragStart = null;
                text_on_image[currentLabelIndex].label.ReleaseMouseCapture();
                text_on_image[currentLabelIndex].Left = (double)text_on_image[currentLabelIndex].label.GetValue(Canvas.LeftProperty);
                text_on_image[currentLabelIndex].Top = (double)text_on_image[currentLabelIndex].label.GetValue(Canvas.TopProperty);
                //text_on_image[currentLabelIndex].Margin.Left = 
                //tell code that object is not moving anymore and change cursor back to a hand
                _isMoving = false;
                Mouse.OverrideCursor = Cursors.Hand;
            }
        }
        private void Label_OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isMoving)
            {
                Point canvPosToWindow = cnt_labels.TransformToAncestor(this).Transform(new Point(0, 0));

                var upperlimit = canvPosToWindow.Y + (text_on_image[currentLabelIndex].label.Margin.Top / 2);
                var lowerlimit = canvPosToWindow.Y + cnt_labels.ActualHeight - (text_on_image[currentLabelIndex].label.Margin.Top / 2);

                var leftlimit = canvPosToWindow.X + (text_on_image[currentLabelIndex].label.Margin.Left / 2);
                var rightlimit = canvPosToWindow.X + cnt_labels.ActualWidth - (text_on_image[currentLabelIndex].label.Margin.Left / 2);


                var absmouseXpos = e.GetPosition(this).X;
                var absmouseYpos = e.GetPosition(this).Y;

                if ((absmouseXpos > leftlimit && absmouseXpos < rightlimit)
                    && (absmouseYpos > upperlimit && absmouseYpos < lowerlimit))
                {
                    text_on_image[currentLabelIndex].label.SetValue(Canvas.LeftProperty, e.GetPosition(cnt_labels).X - (text_on_image[currentLabelIndex].label.Margin.Left / 2));
                    text_on_image[currentLabelIndex].label.SetValue(Canvas.TopProperty, e.GetPosition(cnt_labels).Y - (text_on_image[currentLabelIndex].label.Margin.Top / 2));
                }
            }
        }
        private void Label_OnPreviewMouseDown(object sender, MouseEventArgs e)
        {
            if (!_isMoving)
            {
                //If there was a previos label, clear its background color
                ResetLastIndexSelcted();
                //Get clicked label
                Label lbl = (Label)sender;
                currentLabelIndex = text_on_image.FindIndex(x => x.label.Name == lbl.Name);
                text_on_image[currentLabelIndex].label.Margin = new Thickness(0, 0, 0, 0);
                text_on_image[currentLabelIndex].label.Background = Brushes.Aqua;

                //var mousePosition = Mouse.GetPosition(cnt_grd_parent);
                //deltaX = mousePosition.X - _buttonPosition.Value.X;
                //deltaY = mousePosition.Y - _buttonPosition.Value.Y;

                dragStart = e.GetPosition(text_on_image[currentLabelIndex].label);
                text_on_image[currentLabelIndex].label.CaptureMouse();

                hed_main_tb_label_text.Text = text_on_image[currentLabelIndex].label.Content.ToString();
                hed_main_cb_label_fontSize.Text = text_on_image[currentLabelIndex].label.FontSize.ToString();
                hed_main_cb_label_font.Text = text_on_image[currentLabelIndex].label.FontFamily.ToString();
                //var offsetX = (_currentTT == null ? _buttonPosition.Value.X : _buttonPosition.Value.X - _currentTT.X) + deltaX - mousePosition.X;
                //var offsetY = (_currentTT == null ? _buttonPosition.Value.Y : _buttonPosition.Value.Y - _currentTT.Y) + deltaY - mousePosition.Y;
                //tell code that an object is being moved and change cursor
                _isMoving = true;
                Mouse.OverrideCursor = Cursors.Pen;
            }
        }

        private void ResetLastIndexSelcted()
        {
            if (currentLabelIndex != null && currentLabelIndex != -1)
            {
                try
                {
                    text_on_image[currentLabelIndex].label.Background = Brushes.Transparent;
                }
                catch (Exception)
                {
                    //Ignore, because the previos label probably got deleted
                }
            }
        }

        private void Label_OnKeyUp(object sender, KeyEventArgs e)
        {
            if (!hed_main_cb_label_fontSize.IsFocused)
            {
                hed_main_tb_label_text.Focus();
                hed_main_tb_label_text.Text = e.Key.ToString();
            }
        }

        private void Label_OnMouseHoverEnter(object sender, MouseEventArgs e)
        {
            if (!_isMoving)
            {
                Mouse.OverrideCursor = Cursors.Hand;
            }
        }
        private void Label_OnMouseHoverLeave(object sender, MouseEventArgs e)
        {
            if (!_isMoving)
            {
                Mouse.OverrideCursor = Cursors.Hand;
            }
        }

        private void ImageLeftMosueButtonUp(object sender, MouseButtonEventArgs e)
        {

        }

        private void ImageLeftMosueButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_isMoving)
            {
                ResetLastIndexSelcted();
                hed_main_tb_label_text.Focus();
                _isMoving = false;
                LabelHelper lbl = new LabelHelper();
                lbl.label.Content = "TestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTestTest";
                lbl.label.Name = "lbl_gen_cnt_" + text_on_image.Count;
                lbl.label.Margin = new Thickness(mouseLocation.X + 10, mouseLocation.Y + 10, mouseLocation.X + 10, mouseLocation.Y + 10);
                lbl.label.PreviewMouseDown += Label_OnPreviewMouseDown;
                lbl.label.PreviewMouseMove += Label_OnPreviewMouseMove;
                lbl.label.PreviewMouseUp += Label_OnPreviewMouseUp;
                lbl.label.MouseEnter += Label_OnMouseHoverEnter;
                lbl.label.MouseLeave += Label_OnMouseHoverLeave;
                lbl.label.KeyUp += Label_OnKeyUp;
                cnt_labels.Children.Add(lbl.label);
                text_on_image.Add(lbl);

                //Set this under an if, because this option should be changeable by the user
                currentLabelIndex = text_on_image.Count - 1;
                hed_main_tb_label_text.Text = "";
            }
        }

        private void HideStartContent()
        {
            this.cnt_image.Visibility = Visibility.Hidden;
        }

        private void hed_main_btn_save_click(object sender, RoutedEventArgs e)
        {
            information.Save(text_on_image);
            string filename = mwh.SaveFileDialog();
            if (filename != null)
            {
                try
                {
                    IFormatter formatter = new BinaryFormatter();
                    Stream stream = new FileStream(filename, FileMode.Create, FileAccess.Write);

                    formatter.Serialize(stream, information);
                    stream.Close();
                }
                catch (System.ArgumentException ex)
                {
                    ShowNotificationWindow(NotificationType.Error, "Path unavailable", "The Program couldn't save under the given path");
                }
            }
        }

        private void hed_main_btn_new_click(object sender, RoutedEventArgs e)
        {
            Reset();
            BitmapImage image = mwh.OpenNewImageDialog();
            currentSave = image.UriSource.LocalPath.ToString();
            currentBitmap = BitmapImage2Bitmap(image);
            if (image != null)
            {
                ft_tb_currentpath.Text = image.UriSource.ToString();
                cnt_image.Height = image.Height;
                cnt_image.Width = image.Width;
                cnt_image.Source = image;
                information.ImageUri = image.UriSource.ToString();
                ShowHiddenStartContent();
            }
            else
            {
                ShowNotification(NotificationType.Error, "Error", "Couldn't load image. Please try again.");
            }
        }

        private void hed_main_btn_load_click(object sender, RoutedEventArgs e)
        {
            Reset();
            information = mwh.LoadFile();
            BitmapImage image = mwh.GetImage(information.ImageUri);
            currentBitmap = BitmapImage2Bitmap(image);
            ft_tb_currentpath.Text = image.UriSource.ToString();
            cnt_image.Height = image.Height;
            cnt_image.Width = image.Width;
            cnt_image.Source = image;
            information.ImageUri = image.UriSource.ToString();
            currentSave = image.UriSource.LocalPath.ToString();
            foreach (Text item in information.Texts)
            {
                LabelHelper lbl = new LabelHelper();
                lbl.label.Name = item.Name;
                lbl.label.Content = item.Content;
                lbl.label.Margin = new Thickness(item.Margin.Left, item.Margin.Top, item.Margin.Left, item.Margin.Top);
                lbl.label.FontSize = item.FontSize;
                System.Windows.Media.FontFamily font = new System.Windows.Media.FontFamily(item.Font);
                lbl.label.FontFamily = font;

                lbl.label.PreviewMouseDown += Label_OnPreviewMouseDown;
                lbl.label.PreviewMouseMove += Label_OnPreviewMouseMove;
                lbl.label.PreviewMouseUp += Label_OnPreviewMouseUp;
                lbl.label.MouseEnter += Label_OnMouseHoverEnter;
                lbl.label.MouseLeave += Label_OnMouseHoverLeave;
                lbl.label.KeyUp += Label_OnKeyUp;
                cnt_labels.Children.Add(lbl.label);
                text_on_image.Add(lbl);
            }
            ShowHiddenStartContent();
        }

        private void ShowHiddenStartContent()
        {
            this.cnt_image.Visibility = Visibility.Visible;
        }




        //Much uncommented code, which I didn't write (https://www.codeproject.com/Articles/97871/WPF-simple-zoom-and-drag-support-in-a-ScrollViewer)
        void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (lastDragPoint.HasValue)
            {
                Point posNow = e.GetPosition(cnt_scrollviewer);

                double dX = posNow.X - lastDragPoint.Value.X;
                double dY = posNow.Y - lastDragPoint.Value.Y;

                lastDragPoint = posNow;

                cnt_scrollviewer.ScrollToHorizontalOffset(cnt_scrollviewer.HorizontalOffset - dX);
                cnt_scrollviewer.ScrollToVerticalOffset(cnt_scrollviewer.VerticalOffset - dY);
            }
        }

        void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {

            var mousePos = e.GetPosition(cnt_scrollviewer);
            if (mousePos.X <= cnt_scrollviewer.ViewportWidth && mousePos.Y <
                cnt_scrollviewer.ViewportHeight) //make sure we still can use the scrollbars
            {
                cnt_scrollviewer.Cursor = Cursors.SizeAll;
                lastDragPoint = mousePos;
                Mouse.Capture(cnt_scrollviewer);
            }
        }

        void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            lastMousePositionOnTarget = Mouse.GetPosition(grd_content);

            if (e.Delta > 0)
            {
                cnt_slider.Value += 0.1;
            }
            if (e.Delta < 0)
            {
                cnt_slider.Value -= 0.1;
            }

            e.Handled = true;
        }

        void OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            cnt_scrollviewer.Cursor = Cursors.Arrow;
            cnt_scrollviewer.ReleaseMouseCapture();
            lastDragPoint = null;
            _isMoving = false;
        }

        void OnSliderValueChanged(object sender,
             RoutedPropertyChangedEventArgs<double> e)
        {
            scaleTransform.ScaleX = e.NewValue;
            scaleTransform.ScaleY = e.NewValue;

            var centerOfViewport = new Point(cnt_scrollviewer.ViewportWidth / 2,
                                             cnt_scrollviewer.ViewportHeight / 2);
            lastCenterPositionOnTarget = cnt_scrollviewer.TranslatePoint(centerOfViewport, grd_content);
        }

        void Oncnt_scrollviewerScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.ExtentHeightChange != 0 || e.ExtentWidthChange != 0)
            {
                Point? targetBefore = null;
                Point? targetNow = null;

                if (!lastMousePositionOnTarget.HasValue)
                {
                    if (lastCenterPositionOnTarget.HasValue)
                    {
                        var centerOfViewport = new Point(cnt_scrollviewer.ViewportWidth / 2,
                                                         cnt_scrollviewer.ViewportHeight / 2);
                        Point centerOfTargetNow =
                              cnt_scrollviewer.TranslatePoint(centerOfViewport, grd_content);

                        targetBefore = lastCenterPositionOnTarget;
                        targetNow = centerOfTargetNow;
                    }
                }
                else
                {
                    targetBefore = lastMousePositionOnTarget;
                    targetNow = Mouse.GetPosition(grd_content);

                    lastMousePositionOnTarget = null;
                }

                if (targetBefore.HasValue)
                {
                    double dXInTargetPixels = targetNow.Value.X - targetBefore.Value.X;
                    double dYInTargetPixels = targetNow.Value.Y - targetBefore.Value.Y;

                    double multiplicatorX = e.ExtentWidth / grd_content.Width;
                    double multiplicatorY = e.ExtentHeight / grd_content.Height;

                    double newOffsetX = cnt_scrollviewer.HorizontalOffset -
                                        dXInTargetPixels * multiplicatorX;
                    double newOffsetY = cnt_scrollviewer.VerticalOffset -
                                        dYInTargetPixels * multiplicatorY;

                    if (double.IsNaN(newOffsetX) || double.IsNaN(newOffsetY))
                    {
                        return;
                    }

                    cnt_scrollviewer.ScrollToHorizontalOffset(newOffsetX);
                    cnt_scrollviewer.ScrollToVerticalOffset(newOffsetY);
                }
            }
        }

        //IErrorHandler local Methods
        public void ShowNotificationTaskbar(NotificationType notificationType, string title, string message)
        {
            notificationManager.Show(new NotificationContent
            {
                Title = title,
                Message = message,
                Type = notificationType
            });
        }
        public void ShowNotificationWindow(NotificationType notificationType, string title, string message, int duration = 10)
        {
            notificationManager.Show(
                new NotificationContent
                {
                    Title = title,
                    Message = message,
                    Type = notificationType
                },
                areaName: "WindowArea", TimeSpan.FromSeconds(duration));
        }

        //IErrorHandler
        public void ShowNotification(NotificationType notificationType, string title, string message, int duration = 10, bool taskbar = false)
        {
            if (taskbar)
            {
                ShowNotificationTaskbar(notificationType, title, message);
            }
            else
            {
                ShowNotificationWindow(notificationType, title, message, duration);
            }
        }

        //Show Mouse Location on Image
        private void cnt_image_MouseMove(object sender, MouseEventArgs e)
        {
            mouseLocation = GetImageCoordsAt(e);
            ft_tb_cursor.Text = "X: " + Math.Round(mouseLocation.X, 2) + " Y: " + Math.Round(mouseLocation.Y, 2);
        }
        public Point GetImageCoordsAt(MouseEventArgs e)
        {
            if (cnt_image != null && cnt_image.IsMouseOver)
            {
                return e.GetPosition(cnt_image);
            }
            return new Point(-1, -1);
        }

        private void hed_main_tb_label_text_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (currentLabelIndex != null && currentLabelIndex != -1)
            {
                string temp = hed_main_tb_label_text.Text.Replace("\\n", "\r\n");
                text_on_image[currentLabelIndex].label.Content = temp;
            }
        }

        private void hed_label_btn_delete_click(object sender, RoutedEventArgs e)
        {
            if (currentLabelIndex != null && currentLabelIndex != -1)
            {
                cnt_labels.Children.Remove(text_on_image[currentLabelIndex].label);
                text_on_image.RemoveAt(currentLabelIndex);
                _isMoving = false;
                currentLabelIndex = -1;
            }
        }

        private void RibbonWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (!hed_main_cb_label_fontSize.IsFocused)
            {
                if (currentLabelIndex != null && currentLabelIndex != -1)
                {
                    hed_main_tb_label_text.Focus();
                    if (!hed_main_tb_label_text.IsFocused)
                    {
                        hed_main_tb_label_text.Text = e.Key.ToString();
                    }
                }
            }
        }

        private System.Drawing.Bitmap BitmapImage2Bitmap(BitmapImage bitmapImage)
        {
            // BitmapImage bitmapImage = new BitmapImage(new Uri("../Images/test.png", UriKind.Relative));

            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);

                return new System.Drawing.Bitmap(bitmap);
            }
        }


        BitmapImage BitmapToImageSource(System.Drawing.Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

        private void hed_main_btn_export_click(object sender, RoutedEventArgs e)
        {
            bool pathError = false;

            string savePath = currentSave;
            string imageFilePath = "";
            try
            {
                //Get current Path stuff and add _SCOLL behind it. This should be optional someday
                string[] splitSavePath = currentSave.Split('\\');
                string[] splitFilename = splitSavePath[(splitSavePath.Length - 1)].Split('.');
                splitSavePath[(splitSavePath.Length - 1)] = splitFilename[0] + "_SCOLL." + splitFilename[1];
                imageFilePath = splitSavePath[0];
                lastSaveDir = splitSavePath[0];
                for (int i = 1; i < splitSavePath.Length; i++)
                {
                    imageFilePath += "\\" + splitSavePath[i];
                }
                lastSaveDir = imageFilePath;
            }
            catch (IndexOutOfRangeException)
            {
                pathError = true;
                ShowNotification(NotificationType.Error, "Path Error", "There was a problem with the path! The save operation will be suspended.");
            }
            if (!pathError)
            {
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(currentSave);//load the image file

                for (int i = 0; i < text_on_image.Count; i++)
                {
                    string text = text_on_image[i].label.Content.ToString();
                    double fontSizeDouble = text_on_image[i].label.FontSize;
                    int fontSize = Int32.Parse(fontSizeDouble.ToString());
                    string fontFamily = text_on_image[i].label.FontFamily.ToString();
                    System.Drawing.PointF location = new System.Drawing.PointF((float)text_on_image[i].Left, (float)text_on_image[i].Top);
                    using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bitmap))
                    {
                        using (System.Drawing.Font arialFont = new System.Drawing.Font(fontFamily, fontSize))
                        {
                            graphics.DrawString(text, arialFont, System.Drawing.Brushes.Black, location);
                        }
                    }
                }

                bitmap.Save(imageFilePath);//save the image file

                using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bitmap))
                {
                    graphics.Clear(System.Drawing.Color.Transparent);
                }
            }
        }

        //Code: https://stackoverflow.com/a/696144/12571327
        private void hed_bckstg_btn_openLastDir_Click(object sender, RoutedEventArgs e)
        {
            if (lastSaveDir != "" && lastSaveDir != null)
            {
                if (!File.Exists(lastSaveDir))
                {
                    ShowNotification(NotificationType.Error, "File doesn't exist", "The file doesn't exist anymore, maybe it was moved or not correctly saved.");
                    return;
                }

                // combine the arguments together
                // it doesn't matter if there is a space after ','
                string argument = "/select, \"" + lastSaveDir + "\"";

                System.Diagnostics.Process.Start("explorer.exe", argument);
            }
            else
            {
                ShowNotification(NotificationType.Warning, "No last saved file", "There is no last saved file!");
            }
        }

        //Change currently selected label Font
        private void hed_main_cb_label_font_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender != null)
            {
                string text = (sender as ComboBox).SelectedItem as string;
                int index = fonts.FindIndex(x => x.Name == text);
                System.Windows.Media.FontFamily font = new System.Windows.Media.FontFamily(fonts[index].Name);
                text_on_image[currentLabelIndex].label.FontFamily = font;
                text_on_image[currentLabelIndex].Font.currentFont = font;
            }
            else
            {
                ShowNotification(NotificationType.Warning, "Warning", "There was a problem detecting the input, please re-select the selected item.");
            }            
        }

        //Preview of Font (Source: https://stackoverflow.com/a/46913272/12571327)
        DependencyPropertyDescriptor dpd;
        private void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            ComboBoxItem cmb = sender as ComboBoxItem;
            dpd = DependencyPropertyDescriptor
                .FromProperty(IsMouseOverProperty, typeof(ComboBoxItem));
            if (dpd != null)
                dpd.AddValueChanged(cmb, OnIsMouseOver);

        }

        private void ComboBox_Unloaded(object sender, RoutedEventArgs e)
        {
            ComboBoxItem cmb = sender as ComboBoxItem;
            if (dpd != null)
                dpd.RemoveValueChanged(cmb, OnIsMouseOver);
        }

        private void OnIsMouseOver(object sender, EventArgs e)
        {
            ComboBoxItem cmb = sender as ComboBoxItem;
            if (cmb.IsMouseOver)
            {
                string text = cmb.Content.ToString();
                int index = fonts.FindIndex(x => x.Name == text);
                System.Windows.Media.FontFamily font = new System.Windows.Media.FontFamily(fonts[index].Name);
                text_on_image[currentLabelIndex].label.FontFamily = font;
                text_on_image[currentLabelIndex].Font.previewFont = font;
            }
        }

        //Clear Preview Font and replace with last selected Font
        private void hed_main_cb_label_font_DropDownClosed(object sender, EventArgs e)
        {
            text_on_image[currentLabelIndex].label.FontFamily = text_on_image[currentLabelIndex].Font.currentFont;
        }

        //Size change
        private void hed_main_cb_label_fontSize_TextChanged(object sender, TextChangedEventArgs e)
        {
            double fontSize = 0;
            if (double.TryParse(hed_main_cb_label_fontSize.Text, out fontSize))
            {
                text_on_image[currentLabelIndex].label.FontSize = fontSize;
            }
        }

        private void hed_main_btn_export_pdf_click(object sender, RoutedEventArgs e)
        {
            bool pathError = false;

            string savePath = currentSave;
            string imageFilePath = "";
            try
            {
                //Get current Path stuff and add _SCOLL behind it. This should be optional someday
                string[] splitSavePath = currentSave.Split('\\');
                string[] splitFilename = splitSavePath[(splitSavePath.Length - 1)].Split('.');
                splitSavePath[(splitSavePath.Length - 1)] = splitFilename[0] + "_SCOLL." + splitFilename[1];
                imageFilePath = splitSavePath[0];
                lastSaveDir = splitSavePath[0];
                for (int i = 1; i < splitSavePath.Length; i++)
                {
                    imageFilePath += "\\" + splitSavePath[i];
                }
                lastSaveDir = imageFilePath;
            }
            catch (IndexOutOfRangeException)
            {
                pathError = true;
                ShowNotification(NotificationType.Error, "Path Error", "There was a problem with the path! The save operation will be suspended.");
            }
            if (!pathError)
            {
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(currentSave);//load the image file

                for (int i = 0; i < text_on_image.Count; i++)
                {
                    string text = text_on_image[i].label.Content.ToString();
                    double fontSizeDouble = text_on_image[i].label.FontSize;
                    int fontSize = Int32.Parse(fontSizeDouble.ToString());
                    string fontFamily = text_on_image[i].label.FontFamily.ToString();
                    System.Drawing.PointF location = new System.Drawing.PointF((float)text_on_image[i].Left, (float)text_on_image[i].Top);
                    using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bitmap))
                    {
                        using (System.Drawing.Font arialFont = new System.Drawing.Font(fontFamily, fontSize))
                        {
                            graphics.DrawString(text, arialFont, System.Drawing.Brushes.Black, location);
                        }
                    }
                }

                bitmap.Save(imageFilePath);//save the image file

                using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bitmap))
                {
                    graphics.Clear(System.Drawing.Color.Transparent);
                }

                // Convert to PDF and delete image
                PdfHelper.Instance.SaveImageAsPdf(imageFilePath, $"{imageFilePath}.pdf", bitmap.Width, true);
            }
        }
    }
}
