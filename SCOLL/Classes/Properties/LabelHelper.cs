using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;

namespace SCOLL.Classes.Properties
{
    public class LabelHelper
    {
        public Label label { get; set; }
        public double Left { get; set; }
        public double Top { get; set; }
        public Font Font { get; set; }
        public LabelHelper()
        {
            label = new Label();
            Font = new Font();
            Font.currentFont = label.FontFamily;
        }
    }

    public class Font
    {
        public System.Windows.Media.FontFamily currentFont { get; set; }
        public System.Windows.Media.FontFamily previewFont { get; set; }
    }
}
