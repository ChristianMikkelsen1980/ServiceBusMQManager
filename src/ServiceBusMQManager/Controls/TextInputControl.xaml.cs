#region File Information
/********************************************************************
  Project: ServiceBusMQManager
  File:    TextInputControl.xaml.cs
  Created: 2012-11-22

  Author(s):
    Daniel Halan

 (C) Copyright 2012 Ingenious Technology with Quality Sweden AB
     all rights reserved

********************************************************************/
#endregion

using System.Windows.Media;
#region File Information
/********************************************************************
  Project: ServiceBusMQManager
  File:    TextInputControl.xaml.cs
  Created: 2012-11-22

  Author(s):
    Daniel Halan

 (C) Copyright 2012 Ingenious Technology with Quality Sweden AB
     all rights reserved

********************************************************************/
#endregion

using System;
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

using ServiceBusMQ;
using System.Windows.Interop;

namespace ServiceBusMQManager.Controls {
  /// <summary>
  /// Interaction logic for TextInputControl.xaml
  /// </summary>
  public partial class TextInputControl : UserControl, IInputControl {

    readonly SolidColorBrush BACKGROUND_LISTITEM_HOVER = new SolidColorBrush(Color.FromRgb(139, 139, 139));

    readonly SolidColorBrush BORDER_SELECTED = new SolidColorBrush(Color.FromRgb(78, 166, 234));
    readonly SolidColorBrush BORDER_NORMAL = new SolidColorBrush(Colors.DarkGray);
    readonly SolidColorBrush BORDER_LISTITEM = Brushes.Transparent; //new SolidColorBrush(Color.FromRgb(201, 201, 201));



    Type _dataType;
    private bool _isListItem;
    bool _isNullable;


    object _value;

    bool _updating;

    Calendar _calendar;
    UserControl _btn;

    public TextInputControl() {
      InitializeComponent();

      tb.Height = 30;
    }


    public TextInputControl(object value, Type dataType, bool isNullable) {
      InitializeComponent();

      tb.Height = 30;

      Init(value, dataType, isNullable);
    }


    public void Init(object value, Type dataType, bool isNullable) {

      _isNullable = isNullable;
      _dataType = dataType;
      _value = value;


      SetTextBoxValue(value);

      BindDataType();

      UpdateBorder();
    }


    private void BindDataType() {

      tb.Tag = "TEXT";


      if( _dataType.IsGuid() ) {

        tb.Margin = new Thickness(0, 0, 80, 0);
        
        var btn = new TextInputLabelButton();

        btn.Text = "GENERATE";
        btn.Click += btnGuid_Click;
        _btn = btn;

        tb.Tag = "GUID";

      } else if( _dataType.IsDateTime() ) {

        tb.Margin = new Thickness(0, 0, 40, 0);

        var btn = new TextInputImageButton();

        btn.Source = "/ServiceBusMQManager;component/Images/calendar-white.png";
        btn.Click += btnDate_Click;
        _btn = btn;

        CreateCalendar();

      }

      if( _btn != null )
        theGrid.Children.Add(_btn);

    }



    private void CreateCalendar() {
      Calendar c = new Calendar();
      c.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
      c.Height = 155;
      c.Margin = new Thickness(0, 0, 0, -155);
      c.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
      c.PreviewLostKeyboardFocus += c_PreviewLostKeyboardFocus;
      c.SelectedDatesChanged += calendar_SelectedDatesChanged;
      c.Visibility = System.Windows.Visibility.Hidden;
      
      theGrid.Children.Add(c);

      _calendar = c;
    }


    void c_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) {
      if( e.NewFocus != null ) {
      
        if( !(e.NewFocus is System.Windows.Controls.Primitives.CalendarDayButton) ) {
          _calendar.Visibility = System.Windows.Visibility.Hidden;
        }
      } 
    }


    void btnGuid_Click(object sender, RoutedEventArgs e) {
      tb.Text = Guid.NewGuid().ToString().ToUpper();
    }
    void btnDate_Click(object sender, RoutedEventArgs e) {

      BringCalendarToFront();

      object value = RetrieveValue();
      if( _isNullable && value == null )
        _calendar.DisplayDate = DateTime.Now;
      else _calendar.DisplayDate = (DateTime)value;
 
      
      _calendar.Visibility = System.Windows.Visibility.Visible;
      _calendar.Focus();

    }

    private void BringCalendarToFront() {
      var atrCtl = ( ( this.Parent as Grid ).Parent as AttributeControl );
      var parentStack = atrCtl.Parent as StackPanel;
      foreach( AttributeControl p in parentStack.Children.OfType<AttributeControl>() ) {
        if( p.DisplayName != atrCtl.DisplayName ) {
          Panel.SetZIndex(p, 1);
        } else {
          Panel.SetZIndex(p, 2);
        }
      }
    }
    private void calendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e) {
      if( _calendar.SelectedDate.HasValue )
        UpdateValue(_calendar.SelectedDate.Value);
      else {
        if( _isNullable )
          UpdateValue(null);
        else UpdateValue(DateTime.Now);
      }

      _calendar.Visibility = System.Windows.Visibility.Hidden;
    }

    private void calendar_LostFocus_1(object sender, RoutedEventArgs e) {
      //_calendar.Visibility = System.Windows.Visibility.Hidden;
    }


    void SetTextBoxValue(object value) {

      if( value != null ) {

        if( value is string )
          tb.Text = (string)value;

        else if( value is Guid )
          tb.Text = value.ToString().ToUpper();

        else tb.Text = value.ToString();

      } else tb.Text = string.Empty;

    }

    public void UpdateValue(object value) {
      _updating = true;
      try {
        _value = value;

        SetTextBoxValue(value);

      } finally {
        _updating = false;
      }
    }



    bool UpdateValueFromControl() {

      try {
        _value = Tools.Convert(tb.Text, _dataType);

      } catch( NotSupportedException e ) {
        throw e;

      } catch {
        return false;
      }


      return true;
    }


    public object RetrieveValue() {
      UpdateValueFromControl();

      return _value;
    }
    public T RetrieveValue<T>() {
      UpdateValueFromControl();

      return (T)_value;
    }

    public bool IsListItem {
      get {
        return _isListItem;
      }
      set {
        _isListItem = value;
        ListItemStateChanged();
      }
    }

    private void ListItemStateChanged() {

      if( _isListItem ) {
        tb.Foreground = Brushes.White;
        tb.Background = Brushes.Transparent;

        if( _btn != null ) {
          _btn.Visibility = System.Windows.Visibility.Hidden;
          tb.Margin = new Thickness(0, 0, 0, 0);
        }
      }

      UpdateBorder();
    }


    public event EventHandler<EventArgs> ValueChanged;
    void OnValueChanged() {
      if( ValueChanged != null )
        ValueChanged(this, EventArgs.Empty);
    }


    bool _isValidValue = true;

    private void tb_TextChanged(object sender, TextChangedEventArgs e) {
      if( !_updating ) {

        try {
          _isValidValue = UpdateValueFromControl();
          UpdateBorder();

        } catch { }

        OnValueChanged();
      }
    }


    private void UpdateBorder() {
      if( !_isValidValue ) {
        tb.BorderBrush = Brushes.Red;

      } else if( tb.IsFocused ) {
        tb.BorderBrush = BORDER_SELECTED;
        tb.BorderThickness = new Thickness(0.99, 2, 2, 2);

      } else {

        if( !_isListItem ) {
          tb.BorderThickness = new Thickness(0.99);
          tb.BorderBrush = BORDER_NORMAL;

        } else {
          tb.BorderThickness = new Thickness(1, 1, 0, 1);
          tb.BorderBrush = BORDER_LISTITEM;
        }
      }
    }

    private void tb_PreviewTextInput(object sender, TextCompositionEventArgs e) {


      if( _dataType.IsInteger() ) {
        if( !e.Text.IsInt32() )
          e.Handled = true;


      } else if( _dataType.IsDecimal() ) {
        if( !e.Text.IsDecimal() )
          e.Handled = true;

      } else if( _dataType.IsAnyFloatType() ) {
        if( !e.Text.IsDouble() )
          e.Handled = true;
      }

    }
    private void tb_PreviewKeyDown_1(object sender, KeyEventArgs e) {

      if( e.Key == Key.Up ) {

        if( _dataType.IsInteger() ) {
          UpdateValue(Tools.AddValue(_value, _dataType, 1));

        } else if( _dataType.IsAnyFloatType() ) {
          UpdateValue(Tools.AddValue(_value, _dataType, 0.5F));
        }

      } else if( e.Key == Key.Down ) {

        if( _dataType.IsInteger() ) {
          UpdateValue(Tools.AddValue(_value, _dataType, -1));

        } else if( _dataType.IsAnyFloatType() ) {
          UpdateValue(Tools.AddValue(_value, _dataType, -0.5F));
        }

      }

    }

    private void tb_LostFocus(object sender, RoutedEventArgs e) {
      UpdateBorder();
    }

    private void tb_GotFocus(object sender, RoutedEventArgs e) {
      UpdateBorder();
    }

    private bool ContainsDefaultValue() {
      if( _dataType == typeof(Guid) )
        return RetrieveValue<Guid>() == Guid.Empty;

      return false;
    }

    public bool SelectAllTextOnFocus { get; set; }

    internal void SelectAll() {
      tb.SelectAll();
    }

    internal void FocusTextBox() {
      tb.Focus();
    }

    private void tb_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {

      if( ( SelectAllTextOnFocus || ContainsDefaultValue() ) &&
            !tb.IsKeyboardFocusWithin ) {
        tb.SelectAll();
        tb.Focus();

        e.Handled = true;
      }

    }

    private void tb_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) {


      if( SelectAllTextOnFocus || ContainsDefaultValue() ) {
        tb.SelectAll();
      }

    }

    private void tb_MouseEnter(object sender, MouseEventArgs e) {
      if( _isListItem ) {
        theGrid.Background = BACKGROUND_LISTITEM_HOVER;
      }
    }

    private void tb_MouseLeave(object sender, MouseEventArgs e) {
      if( _isListItem ) {
        theGrid.Background = Brushes.Transparent;
      }
    }


  }
}
