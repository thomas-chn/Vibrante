using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Vibrante.UserControls
{
    /// <summary>
    /// Logique d'interaction pour NumericalTextBox.xaml
    /// </summary>
    public partial class NumericTextBox : UserControl
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(NumericTextBox), new PropertyMetadata("0"));
        public static readonly DependencyProperty AllowDecimalProperty = DependencyProperty.Register("AllowDecimal", typeof(bool), typeof(NumericTextBox), new PropertyMetadata(true));
        public static readonly DependencyProperty AllowNegativeProperty = DependencyProperty.Register("AllowNegative", typeof(bool), typeof(NumericTextBox), new PropertyMetadata(false));
        public static readonly DependencyProperty AllowZeroProperty = DependencyProperty.Register("AllowZero", typeof(bool), typeof(NumericTextBox), new PropertyMetadata(false));
        public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register("MinValue", typeof(float), typeof(NumericTextBox), new PropertyMetadata(float.NaN));
        public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register("MaxValue", typeof(float), typeof(NumericTextBox), new PropertyMetadata(float.NaN));
        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent("ValueChanged", RoutingStrategy.Bubble, typeof(EventHandler<RoutedEventArgs>), typeof(NumericTextBox));
        
        /// <summary>
        /// Text displayed in the text box.
        /// </summary>
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        /// <summary>
        /// Value taken by the text box when the input is invalid.
        /// </summary>
        public float DefaultValue { get; set; }

        /// <summary>
        /// Allow the user to enter decimal numbers.
        /// </summary>
        public bool AllowDecimal
        {
            get { return (bool)GetValue(AllowDecimalProperty); }
            set { SetValue(AllowDecimalProperty, value); }
        }

        /// <summary>
        /// Allow the user to enter negative numbers.
        /// </summary>
        public bool AllowNegative
        {
            get { return (bool)GetValue(AllowNegativeProperty); }
            set { SetValue(AllowNegativeProperty, value); }
        }

        /// <summary>
        /// Allow the user to enter zero.
        /// </summary>
        public bool AllowZero
        {
            get { return (bool)GetValue(AllowZeroProperty); }
            set { SetValue(AllowZeroProperty, value); }
        }

        /// <summary>
        /// Minimum value allowed.
        /// </summary>
        public float MinValue
        {
            get { return (float)GetValue(MinValueProperty); }
            set { SetValue(MinValueProperty, value); }
        }

        /// <summary>
        /// Maximum value allowed.
        /// </summary>
        public float MaxValue
        {
            get { return (float)GetValue(MaxValueProperty); }
            set { SetValue(MaxValueProperty, value); }
        }
        
        /// <summary>
        /// Value of the text box.
        /// </summary>
        public float Value { get; set; }

        public event RoutedEventHandler ValueChanged
        {
            add { this.AddHandler(ValueChangedEvent, value); }
            remove { this.RemoveHandler(ValueChangedEvent, value); }
        }

        private string lastValidText;
        private float lastValidValue;

        public NumericTextBox()
        {
            InitializeComponent();
            textbox.DataContext = this;

            textbox.Loaded += ((object sender, RoutedEventArgs e) =>
            {
                if (isTextValid(Text, out string formatted_text, out float value))
                {
                    Value = value;
                    lastValidText = formatted_text;
                    lastValidValue = value;
                }
            });

        }

        /// <summary>
        /// Check if a text is valid according to the current properties.
        /// </summary>
        /// <param name="text">Text to check.</param>
        /// <param name="formatted_text">If the text is valid, reformatted text. Otherwise empty text.</param>
        /// <param name="float_value">If the text is valid, the value of the text. Otherwise 0.</param>
        private bool isTextValid(string text, out string formatted_text, out float float_value)
        {
            if (!AllowNegative)
            {
                MinValue = 0;
            }

            formatted_text = "";
            float_value = 0;

            float value;
            bool isNumeric = float.TryParse(text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out value);

            int int_value = (int)value;
            bool isInteger = (int_value == value);

            if (!isNumeric || (!isInteger && !AllowDecimal) || (value == 0 && !AllowZero))
            {
                return false;
            }
            
            else if (MinValue != double.NaN && value < MinValue)
            {
                value = MinValue;
                int_value = (int)value;
                isInteger = (int_value == value);
            }
            else if (MaxValue != double.NaN && value > MaxValue)
            {
                value = MaxValue;
                int_value = (int)value;
                isInteger = (int_value == value);
            }

            float_value = value;

            if (isInteger)
            {
                formatted_text = int_value.ToString();
            }
            else
            {
                formatted_text = value.ToString();
            }

            return true;

        }

        /// <summary>
        /// If the text is valid, set Text and Value to current value. Otherwise, set them to default values.
        /// </summary>
        private void apply(bool replace_if_invalid = true)
        {
            // If the text is valid, update the value of the textbox
            if (isTextValid(textbox.Text, out string formatted_text, out float value))
            {
                lastValidText = Text;
                lastValidValue = Value;

                Text = formatted_text;
                textbox.Text = Text;
                Value = value;

                this.RaiseEvent(new RoutedEventArgs(ValueChangedEvent));
            }
            
            // Otherwise put the default values
            else if (replace_if_invalid)
            {
                Text = lastValidText;
                textbox.Text = Text;
                Value = lastValidValue;
            }
        }

        private void textbox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Prevent entering non-numeric values
            if (!char.IsDigit(e.Text, e.Text.Length - 1))
            {
                // Prevent entering a period if decimals are not allowed or if there is already one
                if (e.Text != "." || !AllowDecimal || textbox.Text.Contains('.'))
                {
                    // Prevent entering a minus if negative numbers are not allowed or if there is already one or if it is not in first position
                    if (e.Text != "-" || !AllowNegative || textbox.Text.Count(f => f == '-') != 0 || textbox.CaretIndex != 0)
                    {
                        e.Handled = true;
                        return;
                    }
                }
            }
        }

        private void textbox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Disable paste if the text to paste is invalid
            if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && e.Key == Key.V)
            {
                if (!isTextValid(Clipboard.GetText(), out string _, out float _))
                {
                    e.Handled = true;
                }
            }
            else if (Keyboard.IsKeyDown(Key.Enter))
            {
                apply();
                MainWindow.keyboardfocus.Focus();
            }
        }

        private void textbox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            apply();
        }

        private void textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            apply(false);
        }

        private void textbox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            textbox.Text = (Value + Math.Sign(e.Delta)).ToString();
            apply();
        }
    }
}
