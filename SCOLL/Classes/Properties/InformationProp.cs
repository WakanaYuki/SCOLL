using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Serialization;

namespace SCOLL.Classes.Properties
{
    [Serializable]
    public class InformationProp
    {
        public String ImageUri { get; set; }
        public List<Text> Texts { get; set; }
        public InformationProp()
        {
            Texts = new List<Text>();
        }
        public void Save(List<LabelHelper> Labels)
        {
            Texts.Clear();
            foreach (var item in Labels)
            {
                Texts.Add(new Text()
                {
                    Name = item.label.Name,
                    Content = item.label.Content.ToString(),
                    Margin = new MarginValues() { Left = item.Left, Top = item.Top},
                    Font = item.label.FontFamily.ToString(),
                    FontSize = item.label.FontSize
                });
            }
        }
    }

    [Serializable]
    public class Text
    {
        public string Name { get; set; }
        public string Content { get; set; }
        public MarginValues Margin { get; set; }
        public string Font { get; set; }
        public double FontSize { get; set; }
        public Text() { }
    }

    [Serializable]
    public class MarginValues
    {
        public double Left;
        public double Top;
    }
}
