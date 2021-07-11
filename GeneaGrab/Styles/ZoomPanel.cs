using System.Linq;
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

        /// <summary>The image component</summary>
        public UIElement Child
        {
            get => _child;
            private set { _child = value; Initialized = false; }
        }
        private bool Initialized;
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
            if (Initialized && Child != null) return Child;
            else if (!Initialized)
            {
                RightTapped += (s, e) => Reset();

                void SetClip() => Clip = new RectangleGeometry { Rect = new Rect(0, 0, ActualWidth, ActualHeight) }; //Prevents the child from being rendered out of the element
                SizeChanged += (s, e) => SetClip();
                SetClip();
            }

            Child = Children.FirstOrDefault();
            if (Child is null) return null;

            TransformGroup group = new TransformGroup();
            ScaleTransform st = new ScaleTransform();
            group.Children.Add(st);
            TranslateTransform tt = new TranslateTransform();
            group.Children.Add(tt);
            Child.RenderTransform = group;
            Child.RenderTransformOrigin = new Point(0.0, 0.0);
            Child.PointerWheelChanged += child_MouseWheel;
            Child.PointerPressed += child_MouseLeftButtonDown;
            Child.PointerReleased += (s, e) => child_MouseLeftButtonUp();
            Child.PointerMoved += child_MouseMove;

            Initialized = true;
            return Child;
        }

        private TranslateTransform GetTranslateTransform(UIElement element) => (TranslateTransform)((TransformGroup)element.RenderTransform).Children.First(tr => tr is TranslateTransform);
        private ScaleTransform GetScaleTransform(UIElement element) => (ScaleTransform)((TransformGroup)element.RenderTransform).Children.First(tr => tr is ScaleTransform);

        /// <summary>Resets the child's position and zoom</summary>
        public void Reset()
        {
            if (Child is null) return;

            // Reset zoom
            var st = GetScaleTransform(Child);
            st.ScaleX = st.ScaleY = 1.0;

            // Reset position
            var tt = GetTranslateTransform(Child);
            tt.X = tt.Y = 0.0;
        }

        #region Child Events

        private void child_MouseWheel(object _, PointerRoutedEventArgs e)
        {
            if (Child is null) return;

            var st = GetScaleTransform(Child);
            var tt = GetTranslateTransform(Child);

            var delta = e.GetCurrentPoint(this).Properties.MouseWheelDelta;
            if (delta <= 0 && (st.ScaleX < .4 || st.ScaleY < .4)) return;
            double zoom = delta > 0 ? .2 : -.2;

            Point relative = e.GetCurrentPoint(Child).Position;
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
            if (Child is null) return;

            var tt = GetTranslateTransform(Child);
            start = e.GetCurrentPoint(this).Position;
            origin = new Point(tt.X, tt.Y);
            Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Hand, 1);
            Child.CapturePointer(e.Pointer);
        }

        private void child_MouseLeftButtonUp()
        {
            if (Child is null) return;

            Child.ReleasePointerCaptures();
            Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 1);
        }

        private void child_MouseMove(object _, PointerRoutedEventArgs e)
        {
            if (Child is null || Child.PointerCaptures is null || !Child.PointerCaptures.Any()) return;

            var st = GetScaleTransform(Child);
            var tt = GetTranslateTransform(Child);
            var v = e.GetCurrentPoint(this).Position;


            var X = origin.X - start.X + v.X;
            var left = X - Child.ActualSize.X / 2;
            var right = left + Child.ActualSize.X * st.ScaleX;
            var minX = ActualWidth / -4;
            var maxX = ActualWidth / 4;
            if (left < maxX && right > minX) tt.X = X;

            var Y = origin.Y - start.Y + v.Y;
            var top = Y - Child.ActualSize.Y / 2;
            var bottom = top + Child.ActualSize.Y * st.ScaleY;
            var minY = ActualHeight / -4;
            var maxY = ActualHeight / 4;
            if (top < maxY && bottom > minY) tt.Y = Y;


            PositionChanged?.Invoke(tt.X, tt.Y);
        }

        #endregion
    }
}
