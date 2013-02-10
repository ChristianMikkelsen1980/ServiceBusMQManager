#region File Information
/********************************************************************
  Project: ServiceBusMQManager
  File:    QueueItem.cs
  Created: 2012-08-21

  Author(s):
    Daniel Halan

 (C) Copyright 2012 Ingenious Technology with Quality Sweden AB
     all rights reserved

********************************************************************/
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ServiceBusMQ.Model {

  public enum QueueType { Command = 0, Event, Message, Error }

  public class QueueItem {

    public QueueItem(Queue queue) {
      Queue = queue;
    }

    public string DisplayName { get; set; }

    public double TextWidth {
      get {

        var formattedText = new System.Windows.Media.FormattedText(
             DisplayName,
             CultureInfo.CurrentCulture,
             FlowDirection.LeftToRight,
             new Typeface("Calibri"),
             15.5,
             Brushes.Black);

        return formattedText.Width + 20;

      }
    }

    public Queue Queue { get; private set; }

    public string Label { get; set; }

    public string[] MessageNames { get; set; }

    public DateTime ArrivedTime { get; set; }
    public int ProcessTime { get; set; }
    
    public string ArrivedTimeString {
      get {
        if( ArrivedTime.Date == DateTime.Today.Date ) {
          return ArrivedTime.ToString("HH:mm:ss");
        } else return string.Format("{1} {0} - {2}", Tools.MONTH_NAMES_ABBR[ArrivedTime.Month-1], ArrivedTime.Day, ArrivedTime.ToString("HH:mm:ss"));
      }
    }

    public string Id { get; set; }

    public bool Processed { get; set; }

    public string ImagePath { get { return "Images/" + Queue.Type + ".png"; } }
    public string SelectedImagePath { get { return "Images/" + Queue.Type + ".selected.png"; } }

    public string Content { get; set; }

    public Dictionary<string, string> Headers { get; set; }

    public QueueItemError Error { get; set; }

  }
}
