﻿using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace GeneaGrab.Views
{
    /// <summary>An element for zooming and moving around an image</summary>
    public class ZoomPanel : Panel
    {
        public ZoomPanel() => Background = new SolidColorBrush(Windows.UI.Colors.Transparent); //Allows interaction with the element

        private bool Initialized;
        private UIElement child
        {
            get => _child;
            set { _child = value; Initialized = false; }
        }
        private UIElement _child;

        /// <summary>The user has moved the child</summary>
        public event System.Action<double, double> PositionChanged;
        /// <summary>The user has changed the zoom</summary>
        public event System.Action<double> ZoomChanged;

        protected override Size ArrangeOverride(Size finalSize)
        {
            Initialize()?.Arrange(new Rect(new Point(), finalSize)); //Initialise the element and place the child in it
            return finalSize;
        }
        public UIElement Initialize()
        {
            if (Initialized && child != null) return child;
            else if (!Initialized)
            {
                RightTapped += (s, e) => Reset();

                void SetClip() => Clip = new RectangleGeometry { Rect = new Rect(0, 0, ActualWidth, ActualHeight) }; //Prevents the child from being rendered out of the element
                SizeChanged += (s, e) => SetClip();
                SetClip();
            }

            child = Children.FirstOrDefault();
            if (child is null) return null;

            TransformGroup group = new TransformGroup();
            ScaleTransform st = new ScaleTransform();
            group.Children.Add(st);
            TranslateTransform tt = new TranslateTransform();
            group.Children.Add(tt);
            child.RenderTransform = group;
            child.RenderTransformOrigin = new Point(0.0, 0.0);
            child.PointerWheelChanged += child_MouseWheel;
            child.PointerPressed += child_MouseLeftButtonDown;
            child.PointerReleased += (s, e) => child_MouseLeftButtonUp();
            child.PointerMoved += child_MouseMove;

            Initialized = true;
            return child;
        }

        private TranslateTransform GetTranslateTransform(UIElement element) => (TranslateTransform)((TransformGroup)element.RenderTransform).Children.First(tr => tr is TranslateTransform);
        private ScaleTransform GetScaleTransform(UIElement element) => (ScaleTransform)((TransformGroup)element.RenderTransform).Children.First(tr => tr is ScaleTransform);

        /// <summary>Resets the child's position and zoom</summary>
        public void Reset()
        {
            if (child is null) return;

            // Reset zoom
            var st = GetScaleTransform(child);
            st.ScaleX = st.ScaleY = 1.0;

            // Reset position
            var tt = GetTranslateTransform(child);
            tt.X = tt.Y = 0.0;
        }

        #region Child Events

        private void child_MouseWheel(object _, PointerRoutedEventArgs e)
        {
            if (child is null) return;

            var st = GetScaleTransform(child);
            var tt = GetTranslateTransform(child);

            var delta = e.GetCurrentPoint(this).Properties.MouseWheelDelta;
            if (delta <= 0 && (st.ScaleX < .4 || st.ScaleY < .4)) return;
            double zoom = delta > 0 ? .2 : -.2;

            Point relative = e.GetCurrentPoint(child).Position;
            double absoluteX;
            double absoluteY;

            absoluteX = relative.X * st.ScaleX + tt.X;
            absoluteY = relative.Y * st.ScaleY + tt.Y;

            st.ScaleX += zoom;
            st.ScaleY += zoom;

            tt.X = absoluteX - relative.X * st.ScaleX;
            tt.Y = absoluteY - relative.Y * st.ScaleY;

            ZoomChanged?.Invoke(st.ScaleX);
        }


        private Point origin;
        private Point start;

        private void child_MouseLeftButtonDown(object _, PointerRoutedEventArgs e)
        {
            if (child is null) return;

            var tt = GetTranslateTransform(child);
            start = e.GetCurrentPoint(this).Position;
            origin = new Point(tt.X, tt.Y);
            Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Hand, 1);
            child.CapturePointer(e.Pointer);
        }

        private void child_MouseLeftButtonUp()
        {
            if (child is null) return;

            child.ReleasePointerCaptures();
            Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 1);
        }

        private void child_MouseMove(object _, PointerRoutedEventArgs e)
        {
            if (child is null || child.PointerCaptures is null || !child.PointerCaptures.Any()) return;

            var st = GetScaleTransform(child);
            var tt = GetTranslateTransform(child);
            var v = e.GetCurrentPoint(this).Position;


            var X = origin.X - start.X + v.X;
            var left = X - child.ActualSize.X / 2;
            var right = left + child.ActualSize.X * st.ScaleX;
            var minX = ActualWidth / -4;
            var maxX = ActualWidth / 4;
            if (left < maxX && right > minX) tt.X = X;

            var Y = origin.Y - start.Y + v.Y;
            var top = Y - child.ActualSize.Y / 2;
            var bottom = top + child.ActualSize.Y * st.ScaleY;
            var minY = ActualHeight / -4;
            var maxY = ActualHeight / 4;
            if (top < maxY && bottom > minY) tt.Y = Y;


            PositionChanged?.Invoke(tt.X, tt.Y);
        }

        #endregion
    }
}
