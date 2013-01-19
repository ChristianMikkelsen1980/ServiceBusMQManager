﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using NServiceBus.Profiler.Common.CodeParser;
using ServiceBusMQ;

namespace ServiceBusMQManager.Controls {

  /// <summary>
  /// Interaction logic for TextEditor.xaml
  /// </summary>
  public partial class TextEditor : UserControl {
    public TextEditor() {
      InitializeComponent();
    }

    public void SetText(string text) {
      doc.Document.Blocks.Clear();
      
      if( text.IsValid() ) {
        var presenter = new CodeBlockPresenter(CodeLanguage.Xml);
        var t = new Paragraph();

        presenter.FillInlines(Tools.FormatXml(text), t.Inlines);
        doc.Document.Blocks.Add(t);
      } 
    }

    public static readonly DependencyProperty ReadOnlyProperty =
      DependencyProperty.Register("ReadOnlyProperty", typeof(bool), typeof(TextEditor), new UIPropertyMetadata(false));

    public bool ReadOnly {
      get { return (bool)GetValue(ReadOnlyProperty); }
      set { SetValue(ReadOnlyProperty, value); }
    }


    public static readonly DependencyProperty TextProperty =
      DependencyProperty.Register("Text", typeof(string), typeof(TextEditor), new UIPropertyMetadata(string.Empty));

    public string Text {
      get { return new TextRange(doc.Document.ContentStart, doc.Document.ContentEnd).Text; }
      set {
        SetText(value);
      }
    }


  }
}
