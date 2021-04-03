using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

namespace aliyundrive_Client_CSharp
{
    /// <summary>
    /// 真搞不懂写个数字框这么多代码
    /// </summary>
    [DefaultEvent("ValueChanged"), DefaultProperty("Value")]
    [TemplatePart(Name = TextBoxTemplateName, Type = typeof(TextBox))]
    [TemplatePart(Name = NumericUpTemplateName, Type = typeof(RepeatButton))]
    [TemplatePart(Name = NumericDownTemplateName, Type = typeof(RepeatButton))]
    public class NumericBox : Control
    {
        private static readonly Type _typeofSelf = typeof(NumericBox);

        private const double DefaultInterval = 1d;
        private const int DefaultDelay = 500;

        private const string TextBoxTemplateName = "PART_TextBox";
        private const string NumericUpTemplateName = "PART_NumericUp";
        private const string NumericDownTemplateName = "PART_NumericDown";

        private static RoutedCommand _increaseCommand = null;
        private static RoutedCommand _decreaseCommand = null;

        private TextBox _valueTextBox;
        private RepeatButton _repeatUp;
        private RepeatButton _repeatDown;

        private double _internalLargeChange = DefaultInterval;
        private double _intervalValueSinceReset = 0;
        private double? _lastOldValue = null;

        private bool _isManual;
        private bool _isBusy;

        static NumericBox()
        {
            InitializeCommands();

            DefaultStyleKeyProperty.OverrideMetadata(_typeofSelf, new FrameworkPropertyMetadata(_typeofSelf));
        }

        #region Command

        private static void InitializeCommands()
        {
            _increaseCommand = new RoutedCommand("Increase", _typeofSelf);
            _decreaseCommand = new RoutedCommand("Decrease", _typeofSelf);

            CommandManager.RegisterClassCommandBinding(_typeofSelf, new CommandBinding(_increaseCommand, OnIncreaseCommand, OnCanIncreaseCommand));
            CommandManager.RegisterClassCommandBinding(_typeofSelf, new CommandBinding(_decreaseCommand, OnDecreaseCommand, OnCanDecreaseCommand));
        }

        public static RoutedCommand IncreaseCommand
        {
            get { return _increaseCommand; }
        }

        public static RoutedCommand DecreaseCommand
        {
            get { return _decreaseCommand; }
        }

        private static void OnIncreaseCommand(object sender, RoutedEventArgs e)
        {
            var numericBox = sender as NumericBox;
            numericBox.ContinueChangeValue(true);
        }

        private static void OnCanIncreaseCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            var numericBox = sender as NumericBox;
            e.CanExecute = (!numericBox.IsReadOnly && numericBox.IsEnabled && DoubleUtil.LessThan(numericBox.Value, numericBox.Maximum));
        }

        private static void OnDecreaseCommand(object sender, RoutedEventArgs e)
        {
            var numericBox = sender as NumericBox;
            numericBox.ContinueChangeValue(false);
        }

        private static void OnCanDecreaseCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            var numericBox = sender as NumericBox;
            e.CanExecute = (!numericBox.IsReadOnly && numericBox.IsEnabled && DoubleUtil.GreaterThan(numericBox.Value, numericBox.Minimum));
        }

        #endregion

        #region RouteEvent

        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent("ValueChanged", RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<double>), _typeofSelf);
        public event RoutedPropertyChangedEventHandler<double> ValueChanged
        {
            add { AddHandler(ValueChangedEvent, value); }
            remove { RemoveHandler(ValueChangedEvent, value); }
        }

        #endregion

        #region Properties

        public static readonly DependencyProperty DisabledValueChangedWhileBusyProperty = DependencyProperty.Register("DisabledValueChangedWhileBusy", typeof(bool), _typeofSelf,
           new PropertyMetadata(false));
        [Category("Common")]
        [DefaultValue(true)]
        public bool DisabledValueChangedWhileBusy
        {
            get { return (bool)GetValue(DisabledValueChangedWhileBusyProperty); }
            set { SetValue(DisabledValueChangedWhileBusyProperty, value); }
        }

        public static readonly DependencyProperty IntervalProperty = DependencyProperty.Register("Interval", typeof(double), _typeofSelf,
           new FrameworkPropertyMetadata(DefaultInterval, IntervalChanged, CoerceInterval));
        [Category("Behavior")]
        [DefaultValue(DefaultInterval)]
        public double Interval
        {
            get { return (double)GetValue(IntervalProperty); }
            set { SetValue(IntervalProperty, value); }
        }

        private static object CoerceInterval(DependencyObject d, object value)
        {
            var interval = (double)value;
            return DoubleUtil.IsNaN(interval) ? 0 : Math.Max(interval, 0);
        }

        private static void IntervalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var numericBox = (NumericBox)d;
            numericBox.ResetInternal();
        }

        public static readonly DependencyProperty SpeedupProperty = DependencyProperty.Register("Speedup", typeof(bool), _typeofSelf,
           new PropertyMetadata(true));
        [Category("Common")]
        [DefaultValue(true)]
        public bool Speedup
        {
            get { return (bool)GetValue(SpeedupProperty); }
            set { SetValue(SpeedupProperty, value); }
        }

        public static readonly DependencyProperty DelayProperty = DependencyProperty.Register("Delay", typeof(int), _typeofSelf,
            new PropertyMetadata(DefaultDelay, null, CoerceDelay));
        [DefaultValue(DefaultDelay)]
        [Category("Behavior")]
        public int Delay
        {
            get { return (int)GetValue(DelayProperty); }
            set { SetValue(DelayProperty, value); }
        }

        private static object CoerceDelay(DependencyObject d, object value)
        {
            var delay = (int)value;
            return Math.Max(delay, 0);
        }

        public static readonly DependencyProperty UpDownButtonsWidthProperty = DependencyProperty.Register("UpDownButtonsWidth", typeof(double), _typeofSelf,
           new PropertyMetadata(20d));
        [Category("Appearance")]
        [DefaultValue(20d)]
        public double UpDownButtonsWidth
        {
            get { return (double)GetValue(UpDownButtonsWidthProperty); }
            set { SetValue(UpDownButtonsWidthProperty, value); }
        }

        public static readonly DependencyProperty TextAlignmentProperty = TextBox.TextAlignmentProperty.AddOwner(_typeofSelf);
        [Category("Common")]
        public TextAlignment TextAlignment
        {
            get { return (TextAlignment)GetValue(TextAlignmentProperty); }
            set { SetValue(TextAlignmentProperty, value); }
        }

        public static readonly DependencyProperty IsReadOnlyProperty = TextBoxBase.IsReadOnlyProperty.AddOwner(_typeofSelf,
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits, IsReadOnlyPropertyChangedCallback));
        [Category("Appearance")]
        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        private static void IsReadOnlyPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue == e.NewValue || e.NewValue == null)
                return;

            ((NumericBox)d).ToggleReadOnlyMode((bool)e.NewValue);
        }

        public static readonly DependencyProperty PrecisionProperty = DependencyProperty.Register("Precision", typeof(int?), _typeofSelf,
            new PropertyMetadata(null, OnPrecisionChanged, CoercePrecision));
        [Category("Common")]
        public int? Precision
        {
            get { return (int?)GetValue(PrecisionProperty); }
            set { SetValue(PrecisionProperty, value); }
        }

        private static object CoercePrecision(DependencyObject d, object value)
        {
            var precision = (int?)value;
            return (precision.HasValue && precision.Value < 0) ? 0 : precision;
        }

        private static void OnPrecisionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var numericBox = (NumericBox)d;
            var newPrecision = (int?)e.NewValue;

            var roundValue = numericBox.CorrectPrecision(newPrecision, numericBox.Value);

            if (DoubleUtil.AreClose(numericBox.Value, roundValue))
                numericBox.InternalSetText(roundValue);
            else
                numericBox.Value = roundValue;
        }

        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register("Minimum", typeof(double), _typeofSelf,
            new PropertyMetadata(double.MinValue, OnMinimumChanged));
        [Category("Common")]
        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        private static void OnMinimumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var numericBox = (NumericBox)d;

            numericBox.CoerceValue(MaximumProperty, numericBox.Maximum);
            numericBox.CoerceValue(ValueProperty, numericBox.Value);
        }

        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register("Maximum", typeof(double), _typeofSelf,
            new PropertyMetadata(double.MaxValue, OnMaximumChanged, CoerceMaximum));
        [Category("Common")]
        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        private static object CoerceMaximum(DependencyObject d, object value)
        {
            var minimum = ((NumericBox)d).Minimum;
            var val = (double)value;
            return DoubleUtil.LessThan(val, minimum) ? minimum : val;
        }

        private static void OnMaximumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var numericBox = (NumericBox)d;
            numericBox.CoerceValue(ValueProperty, numericBox.Value);
        }

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(double), _typeofSelf,
            new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged, CoerceValue));
        [Category("Common")]
        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        private static object CoerceValue(DependencyObject d, object value)
        {
            var numericBox = (NumericBox)d;
            var val = (double)value;

            if (DoubleUtil.LessThan(val, numericBox.Minimum))
                return numericBox.Minimum;

            if (DoubleUtil.GreaterThan(val, numericBox.Maximum))
                return numericBox.Maximum;

            return numericBox.CorrectPrecision(numericBox.Precision, val);
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var numericBox = (NumericBox)d;
            numericBox.OnValueChanged((double)e.OldValue, (double)e.NewValue);
        }

        #endregion

        #region Virtual

        protected virtual void OnValueChanged(double oldValue, double newValue)
        {
            InternalSetText(newValue);
            InvalidateRequerySuggested(newValue);

            if ((!_isBusy || !DisabledValueChangedWhileBusy) && !DoubleUtil.AreClose(oldValue, newValue))
            {
                Console.WriteLine("[ NumericBox ] ValueChanged, OldValue = {0}, NewValue = {1}, IsManual = {2}, IsBusy = {3}",
                     oldValue, newValue, _isManual, _isBusy);
                RaiseEvent(new NumericBoxValueChangedEventArgs<double>(oldValue, newValue, _isManual, _isBusy, ValueChangedEvent));
            }

            _isManual = false;
        }

        #endregion

        #region Override

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_valueTextBox != null)
            {
                _valueTextBox.LostFocus -= OnTextBoxLostFocus;
                _valueTextBox.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
                DataObject.RemovePastingHandler(_valueTextBox, OnValueTextBoxPaste);
            }

            if (_repeatUp != null)
                _repeatUp.PreviewMouseUp -= OnRepeatButtonPreviewMouseUp;

            if (_repeatDown != null)
                _repeatDown.PreviewMouseUp -= OnRepeatButtonPreviewMouseUp;

            _valueTextBox = GetTemplateChild(TextBoxTemplateName) as TextBox;
            _repeatUp = GetTemplateChild(NumericUpTemplateName) as RepeatButton;
            _repeatDown = GetTemplateChild(NumericDownTemplateName) as RepeatButton;

            if (_valueTextBox == null || _repeatUp == null || _repeatDown == null)
            {
                throw new NullReferenceException(string.Format("You have missed to specify {0}, {1} or {2} in your template", NumericUpTemplateName, NumericDownTemplateName, TextBoxTemplateName));
            }

            _repeatUp.PreviewMouseUp += OnRepeatButtonPreviewMouseUp;
            _repeatDown.PreviewMouseUp += OnRepeatButtonPreviewMouseUp;

            ToggleReadOnlyMode(IsReadOnly);
            OnValueChanged(Value, Value);
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);

            if (Focusable && !IsReadOnly)
            {
                Focused();
                SelectAll();
            }
        }

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            base.OnPreviewMouseWheel(e);

            if (e.Delta != 0 && (IsFocused || _valueTextBox.IsFocused))
                ContinueChangeValue(e.Delta < 0 ? false : true, false);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            switch (e.Key)
            {
                case Key.Enter:
                    DealInputText(_valueTextBox.Text);
                    SelectAll();
                    e.Handled = true;
                    break;

                case Key.Up:
                    ContinueChangeValue(true);
                    e.Handled = true;
                    break;

                case Key.Down:
                    ContinueChangeValue(false);
                    e.Handled = true;
                    break;
            }
        }

        protected override void OnPreviewKeyUp(KeyEventArgs e)
        {
            base.OnPreviewKeyUp(e);

            switch (e.Key)
            {
                case Key.Down:
                case Key.Up:
                    ResetInternal();
                    break;
            }
        }

        #endregion

        #region Event 

        private void OnRepeatButtonPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            ResetInternal();
        }

        private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Focusable && !IsReadOnly && !_valueTextBox.IsKeyboardFocusWithin)
            {
                e.Handled = true;
                Focused();
                SelectAll();
            }
        }

        private void OnTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            var tb = (TextBox)sender;
            DealInputText(tb.Text);
        }

        private void OnValueTextBoxPaste(object sender, DataObjectPastingEventArgs e)
        {
            var textBox = (TextBox)sender;
            var textPresent = textBox.Text;

            if (!e.SourceDataObject.GetDataPresent(DataFormats.Text, true))
                return;

            var text = e.SourceDataObject.GetData(DataFormats.Text) as string;
            var newText = string.Concat(textPresent.Substring(0, textBox.SelectionStart), text, textPresent.Substring(textBox.SelectionStart + textBox.SelectionLength));

            double number;
            if (!double.TryParse(newText, out number))
                e.CancelCommand();
        }

        #endregion

        #region Private

        private void Focused()
        {
            if (_valueTextBox != null)
                _valueTextBox.Focus();
        }

        private void SelectAll()
        {
            if (_valueTextBox != null)
                _valueTextBox.SelectAll();
        }

        private void DealInputText(string inputText)
        {
            double convertedValue;
            if (double.TryParse(inputText, out convertedValue))
            {
                if (DoubleUtil.AreClose(Value, convertedValue))
                {
                    //for Redo()
                    InternalSetText(Value);
                    return;
                }

                _isManual = true;

                if (convertedValue > Maximum)
                {
                    if (DoubleUtil.AreClose(Value, Maximum))
                        OnValueChanged(Value, Value);
                    else
                        Value = Maximum;
                }
                else if (convertedValue < Minimum)
                {
                    if (DoubleUtil.AreClose(Value, Minimum))
                        OnValueChanged(Value, Value);
                    else
                        Value = Minimum;
                }
                else
                    Value = convertedValue;
            }
            else
                InternalSetText(Value);
        }

        private void MoveFocus()
        {
            var request = new TraversalRequest((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ? FocusNavigationDirection.Previous : FocusNavigationDirection.Next);
            var elementWithFocus = Keyboard.FocusedElement as UIElement;

            if (elementWithFocus != null)
                elementWithFocus.MoveFocus(request);
        }

        private void ToggleReadOnlyMode(bool isReadOnly)
        {
            if (_valueTextBox == null)
                return;

            if (isReadOnly)
            {
                _valueTextBox.LostFocus -= OnTextBoxLostFocus;
                _valueTextBox.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
                DataObject.RemovePastingHandler(_valueTextBox, OnValueTextBoxPaste);
            }
            else
            {
                _valueTextBox.LostFocus += OnTextBoxLostFocus;
                _valueTextBox.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
                DataObject.AddPastingHandler(_valueTextBox, OnValueTextBoxPaste);
            }
        }

        private void InternalSetText(double newValue)
        {
            var text = newValue.ToString(GetPrecisionFormat());
            if (_valueTextBox != null && !Equals(text, _valueTextBox.Text))
                _valueTextBox.Text = text;
        }

        private string GetPrecisionFormat()
        {
            return Precision.HasValue == false
                ? "g"
                : (Precision.Value == 0
                    ? "#0"
                    : ("#0.0" + string.Join("", Enumerable.Repeat("#", Precision.Value - 1))));
        }

        private void CoerceValue(DependencyProperty dp, object localValue)
        {
            //Update local value firstly
            SetCurrentValue(dp, localValue);
            CoerceValue(dp);
        }

        private double CorrectPrecision(int? precision, double originValue)
        {
            return Math.Round(originValue, precision ?? 0, MidpointRounding.AwayFromZero);
        }

        private void ContinueChangeValue(bool isIncrease, bool isContinue = true)
        {
            if (IsReadOnly || !IsEnabled)
                return;

            if (isIncrease && DoubleUtil.LessThan(Value, Maximum))
            {
                if (!_isBusy && isContinue)
                {
                    _isBusy = true;

                    if (DisabledValueChangedWhileBusy)
                        _lastOldValue = Value;
                }

                _isManual = true;
                Value = (double)CoerceValue(this, Value + CalculateInterval(isContinue));
            }

            if (!isIncrease && DoubleUtil.GreaterThan(Value, Minimum))
            {
                if (!_isBusy && isContinue)
                {
                    _isBusy = true;

                    if (DisabledValueChangedWhileBusy)
                        _lastOldValue = Value;
                }

                _isManual = true;
                Value = (double)CoerceValue(this, Value - CalculateInterval(isContinue));
            }
        }

        private double CalculateInterval(bool isContinue = true)
        {
            if (!Speedup || !isContinue)
                return Interval;

            if (DoubleUtil.GreaterThan((_intervalValueSinceReset += _internalLargeChange), _internalLargeChange * 100))
                _internalLargeChange *= 10;

            return _internalLargeChange;
        }

        private void ResetInternal()
        {
            _internalLargeChange = Interval;
            _intervalValueSinceReset = 0;

            _isBusy = false;

            if (_lastOldValue.HasValue)
            {
                _isManual = true;
                OnValueChanged(_lastOldValue.Value, Value);
                _lastOldValue = null;
            }
        }

        private void InvalidateRequerySuggested(double value)
        {
            if (_repeatUp == null || _repeatDown == null)
                return;

            if (DoubleUtil.AreClose(value, Maximum) && _repeatUp.IsEnabled
               || DoubleUtil.AreClose(value, Minimum) && _repeatDown.IsEnabled)
                CommandManager.InvalidateRequerySuggested();
            else
            {
                if (!_repeatUp.IsEnabled || !_repeatDown.IsEnabled)
                    CommandManager.InvalidateRequerySuggested();
            }
        }

        #endregion


    }
    public class NumericBoxValueChangedEventArgs<T> : RoutedPropertyChangedEventArgs<T>
    {
        public bool IsManual { get; private set; }
        public bool IsBusy { get; private set; }

        public NumericBoxValueChangedEventArgs(T oldValue, T newValue, bool isManual, bool isBusy)
            : base(oldValue, newValue)
        {
            IsManual = isManual;
            IsBusy = isBusy;
        }

        public NumericBoxValueChangedEventArgs(T oldValue, T newValue, bool isManual, bool isBusy, RoutedEvent routedEvent)
            : base(oldValue, newValue, routedEvent)
        {
            IsManual = isManual;
            IsBusy = isBusy;
        }
    }
    public static class DoubleUtil
    {
        internal const double DBL_EPSILON = 2.2204460492503131e-016; /* smallest such that 1.0+DBL_EPSILON != 1.0 */
        internal const float FLT_MIN = 1.175494351e-38F; /* Number close to zero, where float.MinValue is -float.MaxValue */

        /// <summary>
        /// AreClose - Returns whether or not two doubles are "close".  That is, whether or 
        /// not they are within epsilon of each other.  Note that this epsilon is proportional
        /// to the numbers themselves to that AreClose survives scalar multiplication.
        /// There are plenty of ways for this to return false even for numbers which
        /// are theoretically identical, so no code calling this should fail to work if this 
        /// returns false.  This is important enough to repeat:
        /// NB: NO CODE CALLING THIS FUNCTION SHOULD DEPEND ON ACCURATE RESULTS - this should be
        /// used for optimizations *only*.
        /// </summary>
        /// <returns>
        /// bool - the result of the AreClose comparision.
        /// </returns>
        /// <param name="value1"> The first double to compare. </param>
        /// <param name="value2"> The second double to compare. </param>
        public static bool AreClose(double value1, double value2)
        {
            //in case they are Infinities (then epsilon check does not work)
            if (value1 == value2)
                return true;

            // This computes (|value1-value2| / (|value1| + |value2| + 10.0)) < DBL_EPSILON
            double eps = (Math.Abs(value1) + Math.Abs(value2) + 10.0) * DBL_EPSILON;
            double delta = value1 - value2;
            return (-eps < delta) && (eps > delta);
        }

        /// <summary>
        /// LessThan - Returns whether or not the first double is less than the second double.
        /// That is, whether or not the first is strictly less than *and* not within epsilon of
        /// the other number.  Note that this epsilon is proportional to the numbers themselves
        /// to that AreClose survives scalar multiplication.  Note,
        /// There are plenty of ways for this to return false even for numbers which
        /// are theoretically identical, so no code calling this should fail to work if this 
        /// returns false.  This is important enough to repeat:
        /// NB: NO CODE CALLING THIS FUNCTION SHOULD DEPEND ON ACCURATE RESULTS - this should be
        /// used for optimizations *only*.
        /// </summary>
        /// <returns>
        /// bool - the result of the LessThan comparision.
        /// </returns>
        /// <param name="value1"> The first double to compare. </param>
        /// <param name="value2"> The second double to compare. </param>
        public static bool LessThan(double value1, double value2)
        {
            return (value1 < value2) && !AreClose(value1, value2);
        }


        /// <summary>
        /// GreaterThan - Returns whether or not the first double is greater than the second double.
        /// That is, whether or not the first is strictly greater than *and* not within epsilon of
        /// the other number.  Note that this epsilon is proportional to the numbers themselves
        /// to that AreClose survives scalar multiplication.  Note,
        /// There are plenty of ways for this to return false even for numbers which
        /// are theoretically identical, so no code calling this should fail to work if this 
        /// returns false.  This is important enough to repeat:
        /// NB: NO CODE CALLING THIS FUNCTION SHOULD DEPEND ON ACCURATE RESULTS - this should be
        /// used for optimizations *only*.
        /// </summary>
        /// <returns>
        /// bool - the result of the GreaterThan comparision.
        /// </returns>
        /// <param name="value1"> The first double to compare. </param>
        /// <param name="value2"> The second double to compare. </param>
        public static bool GreaterThan(double value1, double value2)
        {
            return (value1 > value2) && !AreClose(value1, value2);
        }

        /// <summary>
        /// LessThanOrClose - Returns whether or not the first double is less than or close to
        /// the second double.  That is, whether or not the first is strictly less than or within
        /// epsilon of the other number.  Note that this epsilon is proportional to the numbers 
        /// themselves to that AreClose survives scalar multiplication.  Note,
        /// There are plenty of ways for this to return false even for numbers which
        /// are theoretically identical, so no code calling this should fail to work if this 
        /// returns false.  This is important enough to repeat:
        /// NB: NO CODE CALLING THIS FUNCTION SHOULD DEPEND ON ACCURATE RESULTS - this should be
        /// used for optimizations *only*.
        /// </summary>
        /// <returns>
        /// bool - the result of the LessThanOrClose comparision.
        /// </returns>
        /// <param name="value1"> The first double to compare. </param>
        /// <param name="value2"> The second double to compare. </param>
        public static bool LessThanOrClose(double value1, double value2)
        {
            return (value1 < value2) || AreClose(value1, value2);
        }

        /// <summary>
        /// GreaterThanOrClose - Returns whether or not the first double is greater than or close to
        /// the second double.  That is, whether or not the first is strictly greater than or within
        /// epsilon of the other number.  Note that this epsilon is proportional to the numbers 
        /// themselves to that AreClose survives scalar multiplication.  Note,
        /// There are plenty of ways for this to return false even for numbers which
        /// are theoretically identical, so no code calling this should fail to work if this 
        /// returns false.  This is important enough to repeat:
        /// NB: NO CODE CALLING THIS FUNCTION SHOULD DEPEND ON ACCURATE RESULTS - this should be
        /// used for optimizations *only*.
        /// </summary>
        /// <returns>
        /// bool - the result of the GreaterThanOrClose comparision.
        /// </returns>
        /// <param name="value1"> The first double to compare. </param>
        /// <param name="value2"> The second double to compare. </param>
        public static bool GreaterThanOrClose(double value1, double value2)
        {
            return (value1 > value2) || AreClose(value1, value2);
        }

        /// <summary>
        /// IsOne - Returns whether or not the double is "close" to 1.  Same as AreClose(double, 1),
        /// but this is faster.
        /// </summary>
        /// <returns>
        /// bool - the result of the AreClose comparision.
        /// </returns>
        /// <param name="value"> The double to compare to 1. </param>
        public static bool IsOne(double value)
        {
            return Math.Abs(value - 1.0) < 10.0 * DBL_EPSILON;
        }

        /// <summary>
        /// IsZero - Returns whether or not the double is "close" to 0.  Same as AreClose(double, 0),
        /// but this is faster.
        /// </summary>
        /// <returns>
        /// bool - the result of the AreClose comparision.
        /// </returns>
        /// <param name="value"> The double to compare to 0. </param>
        public static bool IsZero(double value)
        {
            return Math.Abs(value) < 10.0 * DBL_EPSILON;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static bool IsBetweenZeroAndOne(double val)
        {
            return (GreaterThanOrClose(val, 0) && LessThanOrClose(val, 1));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static int DoubleToInt(double val)
        {
            return (0 < val) ? (int)(val + 0.5) : (int)(val - 0.5);
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct NanUnion
        {
            [FieldOffset(0)]
            internal double DoubleValue;
            [FieldOffset(0)]
            internal UInt64 UintValue;
        }

        // The standard CLR double.IsNaN() function is approximately 100 times slower than our own wrapper,
        // so please make sure to use DoubleUtil.IsNaN() in performance sensitive code.
        // PS item that tracks the CLR improvement is DevDiv Schedule : 26916.
        // IEEE 754 : If the argument is any value in the range 0x7ff0000000000001L through 0x7fffffffffffffffL 
        // or in the range 0xfff0000000000001L through 0xffffffffffffffffL, the result will be NaN.         
        public static bool IsNaN(double value)
        {
            NanUnion t = new NanUnion();
            t.DoubleValue = value;

            UInt64 exp = t.UintValue & 0xfff0000000000000;
            UInt64 man = t.UintValue & 0x000fffffffffffff;

            return (exp == 0x7ff0000000000000 || exp == 0xfff0000000000000) && (man != 0);
        }

    }
     public class DoubleToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
            var oriValue = (double)value;
            var compareValue = parameter == null ? 0d : double.Parse(parameter.ToString());

            return DoubleUtil.GreaterThan(oriValue, compareValue) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public static class ResourceKeys
    {
        #region ButtonBase
        /// <summary>
        /// Style="{DynamicResource/StaticResource {x:Static themes:ResourceKeys.NoBgButtonStyleKey}}"
        /// </summary>
        public const string NoBgButtonBaseStyle = "NoBgButtonBaseStyle";
        public static ComponentResourceKey NoBgButtonBaseStyleKey
        {
            get { return new ComponentResourceKey(typeof(ResourceKeys), NoBgButtonBaseStyle); }
        }

        #endregion

        #region Button

        public const string NormalButtonStyle = "NormalButtonStyle";
        public static ComponentResourceKey NormalButtonStyleKey
        {
            get { return new ComponentResourceKey(typeof(ResourceKeys), NormalButtonStyle); }
        }

        public const string TitlebarButtonStyle = "TitlebarButtonStyle";
        public static ComponentResourceKey TitlebarButtonStyleKey
        {
            get { return new ComponentResourceKey(typeof(ResourceKeys), TitlebarButtonStyle); }
        }

        public const string FgWhiteCloseBtnStyle = "FgWhiteCloseBtnStyle";
        public static ComponentResourceKey FgWhiteCloseBtnStyleKey
        {
            get { return new ComponentResourceKey(typeof(ResourceKeys), FgWhiteCloseBtnStyle); }
        }

        public const string FgRedCloseBtnStyle = "FgRedCloseBtnStyle";
        public static ComponentResourceKey FgRedCloseBtnStyleKey
        {
            get { return new ComponentResourceKey(typeof(ResourceKeys), FgRedCloseBtnStyle); }
        }

        public const string OpacityButtonStyle = "OpacityButtonStyle";
        public static ComponentResourceKey OpacityButtonStyleKey
        {
            get { return new ComponentResourceKey(typeof(ResourceKeys), OpacityButtonStyle); }
        }

        #endregion

        #region RepeatButton

        public const string NormalRepeatButtonStyle = "NormalRepeatButtonStyle";
        public static ComponentResourceKey NormalRepeatButtonStyleKey
        {
            get { return new ComponentResourceKey(typeof(ResourceKeys), NormalRepeatButtonStyle); }
        }

        #endregion

        #region ToggleStatus

        public const string NoBgToggleStatusStyle = "NoBgToggleStatusStyle";
        public static ComponentResourceKey NoBgToggleStatusStyleKey
        {
            get { return new ComponentResourceKey(typeof(ResourceKeys), NoBgToggleStatusStyle); }
        }

        #endregion

        #region SpitButton

        public const string MenuItemStyle = "MenuItemStyle";
        public static ComponentResourceKey MenuItemStyleKey
        {
            get { return new ComponentResourceKey(typeof(ResourceKeys), MenuItemStyle); }
        }

        public const string SeparatorStyle = "SeparatorStyle";
        public static ComponentResourceKey SeparatorStyleKey
        {
            get { return new ComponentResourceKey(typeof(ResourceKeys), SeparatorStyle); }
        }

        public const string MenuItemSubmenuContent = "MenuItemSubmenuContent";
        public static ComponentResourceKey MenuItemSubmenuContentKey
        {
            get { return new ComponentResourceKey(typeof(ResourceKeys), MenuItemSubmenuContent); }
        }

        public const string MenuItemTopLevelHeaderTemplate = "MenuItemTopLevelHeaderTemplate";
        public static ComponentResourceKey MenuItemTopLevelHeaderTemplateKey
        {
            get { return new ComponentResourceKey(typeof(ResourceKeys), MenuItemTopLevelHeaderTemplate); }
        }

        public const string MenuItemTopLevelItemTemplate = "MenuItemTopLevelItemTemplate";
        public static ComponentResourceKey MenuItemTopLevelItemTemplateKey
        {
            get { return new ComponentResourceKey(typeof(ResourceKeys), MenuItemTopLevelItemTemplate); }
        }

        public const string MenuItemSubmenuHeaderTemplate = "MenuItemSubmenuHeaderTemplate";
        public static ComponentResourceKey MenuItemSubmenuHeaderTemplateKey
        {
            get { return new ComponentResourceKey(typeof(ResourceKeys), MenuItemSubmenuHeaderTemplate); }
        }

        public const string MenuItemSubmenuItemTemplate = "MenuItemSubmenuItemTemplate";
        public static ComponentResourceKey MenuItemSubmenuItemTemplateKey
        {
            get { return new ComponentResourceKey(typeof(ResourceKeys), MenuItemSubmenuItemTemplate); }
        }

        #endregion
    }
}
