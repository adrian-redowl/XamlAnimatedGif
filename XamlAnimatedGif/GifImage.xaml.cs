using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace XamlAnimatedGif
{
    /// <summary>
    /// An animated GIF with a caption and the HausFin media-item ring.
    /// The ring is the 4px #FFFFE2A8 border a selected poster uses.
    /// <see cref="PlayWhen"/> chooses whether selection or the pointer runs the animation.
    /// </summary>
    public partial class GifImage : UserControl
    {
        // Same cream as HausFin FocusRingBrush on a media item.
        private static readonly SolidColorBrush SelectedBrush = Freeze(Color.FromRgb(0xFF, 0xE2, 0xA8));

        public GifImage()
        {
            InitializeComponent();
            Gif.Stretch = Stretch;
            MouseEnter += (_, _) => OnPointerChanged();
            MouseLeave += (_, _) => OnPointerChanged();
        }

        #region SourceUri

        public Uri SourceUri
        {
            get => (Uri)GetValue(SourceUriProperty);
            set => SetValue(SourceUriProperty, value);
        }

        public static readonly DependencyProperty SourceUriProperty =
            DependencyProperty.Register(
                nameof(SourceUri),
                typeof(Uri),
                typeof(GifImage),
                new PropertyMetadata(null, OnSourceUriChanged));

        private static void OnSourceUriChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var gif = (GifImage)d;
            if (gif.Gif == null)
                return;

            gif.ArmAutoStart();
            AnimationBehavior.SetSourceUri(gif.Gif, (Uri)e.NewValue);
        }

        #endregion

        #region SourceStream

        public Stream SourceStream
        {
            get => (Stream)GetValue(SourceStreamProperty);
            set => SetValue(SourceStreamProperty, value);
        }

        public static readonly DependencyProperty SourceStreamProperty =
            DependencyProperty.Register(
                nameof(SourceStream),
                typeof(Stream),
                typeof(GifImage),
                new PropertyMetadata(null, OnSourceStreamChanged));

        private static void OnSourceStreamChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var gif = (GifImage)d;
            if (gif.Gif == null)
                return;

            gif.ArmAutoStart();
            AnimationBehavior.SetSourceStream(gif.Gif, (Stream)e.NewValue);
        }

        #endregion

        #region Stretch

        public Stretch Stretch
        {
            get => (Stretch)GetValue(StretchProperty);
            set => SetValue(StretchProperty, value);
        }

        public static readonly DependencyProperty StretchProperty =
            DependencyProperty.Register(
                nameof(Stretch),
                typeof(Stretch),
                typeof(GifImage),
                new PropertyMetadata(Stretch.UniformToFill, OnStretchChanged));

        private static void OnStretchChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var gif = (GifImage)d;
            if (gif.Gif == null)
                return;

            gif.Gif.Stretch = (Stretch)e.NewValue;
        }

        #endregion

        #region IsSelected

        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register(
                nameof(IsSelected),
                typeof(bool),
                typeof(GifImage),
                new PropertyMetadata(false, OnIsSelectedChanged));

        private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((GifImage)d).ApplySelection();
        }

        #endregion

        #region ShowRing

        /// <summary>When false, selection still plays the GIF and leaves the border alone.</summary>
        public bool ShowRing
        {
            get => (bool)GetValue(ShowRingProperty);
            set => SetValue(ShowRingProperty, value);
        }

        public static readonly DependencyProperty ShowRingProperty =
            DependencyProperty.Register(
                nameof(ShowRing),
                typeof(bool),
                typeof(GifImage),
                new PropertyMetadata(true, OnShowRingChanged));

        private static void OnShowRingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((GifImage)d).ApplySelection();
        }

        #endregion

        #region PlayWhen

        public GifPlayWhen PlayWhen
        {
            get => (GifPlayWhen)GetValue(PlayWhenProperty);
            set => SetValue(PlayWhenProperty, value);
        }

        public static readonly DependencyProperty PlayWhenProperty =
            DependencyProperty.Register(
                nameof(PlayWhen),
                typeof(GifPlayWhen),
                typeof(GifImage),
                new PropertyMetadata(GifPlayWhen.Always, OnPlayWhenChanged));

        private static void OnPlayWhenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((GifImage)d).ApplyPlayback();
        }

        #endregion

        #region Caption

        public string Caption
        {
            get => (string)GetValue(CaptionProperty);
            set => SetValue(CaptionProperty, value);
        }

        public static readonly DependencyProperty CaptionProperty =
            DependencyProperty.Register(
                nameof(Caption),
                typeof(string),
                typeof(GifImage),
                new PropertyMetadata(null, OnCaptionChanged));

        private static void OnCaptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var gif = (GifImage)d;
            if (gif.CaptionText == null)
                return;

            var text = (string)e.NewValue;
            var hasText = !string.IsNullOrWhiteSpace(text);
            gif.CaptionText.Text = hasText ? text : "";
            gif.CaptionText.Visibility = hasText ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion

        private void ApplySelection()
        {
            if (Ring == null)
                return;

            Ring.BorderBrush = IsSelected && ShowRing ? SelectedBrush : Brushes.Transparent;
            if (PlayWhen == GifPlayWhen.Selected)
                ApplyPlayback();
        }

        private void OnPointerChanged()
        {
            if (PlayWhen == GifPlayWhen.MouseOver)
                ApplyPlayback();
        }

        /// <summary>
        /// Tell the loader whether to start. It reads this after the decode, so a
        /// selection change during load still wins.
        /// </summary>
        private void ArmAutoStart()
        {
            if (Gif == null)
                return;

            AnimationBehavior.SetAutoStart(Gif, WantsPlayback());
        }

        private void ApplyPlayback()
        {
            if (Gif == null)
                return;

            var play = WantsPlayback();
            AnimationBehavior.SetAutoStart(Gif, play);

            var animator = AnimationBehavior.GetAnimator(Gif);
            if (animator == null)
                return;

            if (play)
            {
                animator.Play();
                return;
            }

            animator.Pause();
            animator.Rewind();
        }

        private bool WantsPlayback()
        {
            switch (PlayWhen)
            {
                case GifPlayWhen.Selected:
                    return IsSelected;
                case GifPlayWhen.MouseOver:
                    return IsMouseOver;
                default:
                    return true;
            }
        }

        private static SolidColorBrush Freeze(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
    }
}
