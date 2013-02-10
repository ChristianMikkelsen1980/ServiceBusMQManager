﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace ServiceBusMQ {
  public static class QueueColorManager {

    public static readonly int[] COLORS = new int[] { 0xA200FF, 0xFF0097, 0x00ABA9, 0x8CBF26, 0xA05000, 0xE671B8, 0xF09609, 0x1BA1E2, 
                                                        0xE51400, // red
                                                        0x339933,
                                                        0x632F00
    };

    static List<int> _unusedColors;

    static Random _rnd = new Random();

    static QueueColorManager() {
    
      _unusedColors = new List<int>(COLORS);

    }

    public static int GetRandomAvailableColor() {
      int index = _rnd.Next(_unusedColors.Count);

      int color = _unusedColors[index];
      _unusedColors.Remove(color);

      return color;
      //return Color.Azure.ToArgb();
    }
  
  }
}
