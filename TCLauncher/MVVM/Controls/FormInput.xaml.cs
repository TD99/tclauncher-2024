using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace TCLauncher.MVVM.Controls
{
    public partial class FormInput : UserControl
    {
        public FormInput()
        {
            InitializeComponent();

            ComboBox.SelectionChanged += (s, e) => ComboBoxSelectionChangedEvenHandler?.Invoke(s, e);
        }

        public string Hint
        {
            get => (string)GetValue(HintProperty);
            set => SetValue(HintProperty, value);
        }
        public static readonly DependencyProperty HintProperty =
            DependencyProperty.Register(nameof(Hint), typeof(string), typeof(FormInput));

        public string Caption
        {
            get => (string)GetValue(CaptionProperty);
            set => SetValue(CaptionProperty, value);
        }
        public static readonly DependencyProperty CaptionProperty =
            DependencyProperty.Register(nameof(Caption), typeof(string), typeof(FormInput));

        public string RequirementCaption
        {
            get => (string)GetValue(RequirementCaptionProperty);
            set => SetValue(RequirementCaptionProperty, value);
        }
        public static readonly DependencyProperty RequirementCaptionProperty =
            DependencyProperty.Register(nameof(RequirementCaption), typeof(string), typeof(FormInput));

        public string RequirementCaptionColor
        {
            get => (string)GetValue(RequirementCaptionColorProperty);
            set => SetValue(RequirementCaptionColorProperty, value);
        }
        public static readonly DependencyProperty RequirementCaptionColorProperty =
            DependencyProperty.Register(nameof(RequirementCaptionColor), typeof(string), typeof(FormInput), new PropertyMetadata("#3b4149"));


        public bool IsPassword
        {
            get => (bool)GetValue(IsPasswordProperty);
            set => SetValue(IsPasswordProperty, value);
        }
        public static readonly DependencyProperty IsPasswordProperty =
            DependencyProperty.Register(nameof(IsPassword), typeof(bool), typeof(FormInput));

        public string Text
        {
            get => TextBox.Text;
            set => TextBox.Text = value;
        }

        public Brush TextColor
        {
            get => (bool)GetValue(IsPasswordProperty) ? PasswordBox.Foreground : TextBox.Foreground;
            set => ((bool)GetValue(IsPasswordProperty) ? (Control)PasswordBox : (Control)TextBox).Foreground = value;
        }

        public Brush BackgroundColor
        {
            get => (bool)GetValue(IsPasswordProperty) ? PasswordBox.Background : TextBox.Background;
            set => ((bool)GetValue(IsPasswordProperty) ? (Control)PasswordBox : (Control)TextBox).Background = value;
        }

        public Brush BackgroundColorSolid
        {
            get
            {
                if (!(BackgroundColor is SolidColorBrush brush)) return Brushes.Black;
                var color = brush.Color;
                return new SolidColorBrush(Color.FromRgb(color.R, color.G, color.B));
            }
        }

        public Brush HintColor
        {
            get => HintBlock.Foreground;
            set => HintBlock.Foreground = value;
        }

        public Brush AccentColor
        {
            get => (Brush)GetValue(AccentColorProperty);
            set => SetValue(AccentColorProperty, value);
        }
        public static readonly DependencyProperty AccentColorProperty =
            DependencyProperty.Register(nameof(AccentColor), typeof(Brush), typeof(FormInput), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(30, 144, 255))));

        public Brush AccentHoverColor
        {
            get => (Brush)GetValue(AccentHoverColorProperty);
            set => SetValue(AccentHoverColorProperty, value);
        }
        public static readonly DependencyProperty AccentHoverColorProperty =
            DependencyProperty.Register(nameof(AccentHoverColor), typeof(Brush), typeof(FormInput), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(100, 178, 255))));

        public Brush AccentBlurColor
        {
            get => (Brush)GetValue(AccentBlurColorProperty);
            set => SetValue(AccentBlurColorProperty, value);
        }
        public static readonly DependencyProperty AccentBlurColorProperty =
            DependencyProperty.Register(nameof(AccentBlurColor), typeof(Brush), typeof(FormInput), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(101, 112, 124))));

        public Brush AccentContrastColor
        {
            get => (Brush)GetValue(AccentContrastColorProperty);
            set => SetValue(AccentContrastColorProperty, value);
        }
        public static readonly DependencyProperty AccentContrastColorProperty =
            DependencyProperty.Register(nameof(AccentContrastColor), typeof(Brush), typeof(FormInput), new PropertyMetadata(new SolidColorBrush(Color.FromArgb(30, 0, 0, 0))));

        public bool IsReadOnly
        {
            get => TextBox.IsReadOnly;
            set => TextBox.IsReadOnly = value;
        }

        public bool IsReadOnlyCaretVisible
        {
            get => TextBox.IsReadOnlyCaretVisible;
            set => TextBox.IsReadOnlyCaretVisible = value;
        }

        public string Password
        {
            get => (string)GetValue(PasswordProperty);
            set => SetValue(PasswordProperty, value);
        }
        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.Register(nameof(Password), typeof(string), typeof(FormInput));

        public bool IsRequired
        {
            get => (bool)GetValue(IsRequiredProperty);
            set => SetValue(IsRequiredProperty, value);
        }
        public static readonly DependencyProperty IsRequiredProperty =
            DependencyProperty.Register(nameof(IsRequired), typeof(bool), typeof(FormInput));

        public bool IsComboBox
        {
            get => (bool)GetValue(IsComboBoxProperty);
            set => SetValue(IsComboBoxProperty, value);
        }
        public static readonly DependencyProperty IsComboBoxProperty = 
            DependencyProperty.Register(nameof(IsComboBox), typeof(bool), typeof(FormInput));

        public IEnumerable ComboBoxItems
        {
            get => ComboBox.ItemsSource;
            set => ComboBox.ItemsSource = value;
        }

        public string ComboBoxDisplayMemberPath
        {
            get => ComboBox.DisplayMemberPath;
            set => ComboBox.DisplayMemberPath = value;
        }

        public string ComboBoxSelectedValuePath
        {
            get => ComboBox.SelectedValuePath;
            set => ComboBox.SelectedValuePath = value;
        }

        public event SelectionChangedEventHandler ComboBoxSelectionChangedEvenHandler;

        public object ComboBoxSelectedItem => ComboBox.SelectedItem;

        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            Password = PasswordBox.Password;
        }

        private void CaptionBlock_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input,
            new Action(delegate () {
                if ((bool)GetValue(IsPasswordProperty))
                {
                    PasswordBox.Focus();
                    Keyboard.Focus(PasswordBox);
                }
                else if ((bool)GetValue(IsComboBoxProperty))
                {
                    ComboBox.IsDropDownOpen = true;
                    Keyboard.Focus(ComboBox);
                }
                else
                {
                    TextBox.Focus();
                    Keyboard.Focus(TextBox);
                }
            }));
        }
    }
}
