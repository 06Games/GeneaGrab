using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace GeneaGrab.Views
{
    /// <summary>An element for zooming and moving around an image</summary>
    public class ZoomPanel : Panel
    {
        public ZoomPanel() => Background = new SolidColorBrush(Colors.Transparent); //Allows interaction with the element

        /// <summary>The image component</summary>
        private Control? Child
        {
            get => _child;
            set
            {
                _child = value;
                initialized = false;
            }
        }
        private bool initialized;
        private Control? _child;

        /// <summary>The user has moved the child</summary>
        public event System.Action<double, double>? PositionChanged;
        /// <summary>The user has changed the zoom</summary>
        public event System.Action<double>? ZoomChanged;

        protected override Size ArrangeOverride(Size finalSize)
        {
            Initialize()?.Arrange(new Rect(new Point(), finalSize)); //Initialise the element and place the child in it
            return finalSize;
        }
        private Control? Initialize()
        {
            if (initialized && Child != null) return Child;
            if (!initialized)
            {
                PointerPressed += (_, e) =>
                {
                    var point = e.GetCurrentPoint(this);
                    if (point.Properties.IsRightButtonPressed) Reset();
                };

                void SetClip() => Clip = new RectangleGeometry { Rect = new Rect(0, 0, Bounds.Width, Bounds.Height) }; //Prevents the child from being rendered out of the element
                LayoutUpdated += (_, _) => SetClip();
                SetClip();
            }

            Child = Children.FirstOrDefault() as Control;
            if (Child is null) return null;

            var group = new TransformGroup();
            var st = new ScaleTransform();
            group.Children.Add(st);
            var tt = new TranslateTransform();
            group.Children.Add(tt);
            Child.RenderTransform = group;
            Child.RenderTransformOrigin = new RelativePoint(new Point(0.0, 0.0), RelativeUnit.Absolute);
            Child.PointerWheelChanged += child_MouseWheel;
            Child.PointerPressed += child_MouseLeftButtonDown;
            Child.PointerReleased += child_MouseLeftButtonUp;
            Child.PointerMoved += child_MouseMove;

            initialized = true;
            return Child;
        }

        private static TranslateTransform? GetTranslateTransform(IVisual element)
        {
            if (element.RenderTransform is TransformGroup group) return (TranslateTransform)group.Children.First(tr => tr is TranslateTransform);
            return null;
        }
        private static ScaleTransform? GetScaleTransform(IVisual element)
        {
            if (element.RenderTransform is TransformGroup group) return (ScaleTransform)group.Children.First(tr => tr is ScaleTransform);
            return null;
        }

        /// <summary>Resets the child's position and zoom</summary>
        public void Reset()
        {
            if (Child is null) return;

            // Reset zoom
            var st = GetScaleTransform(Child);
            if (st != null) st.ScaleX = st.ScaleY = 1.0;

            // Reset position
            var tt = GetTranslateTransform(Child);
            if (tt != null) tt.X = tt.Y = 0.0;
        }

        #region Child Events

        private void child_MouseWheel(object? _, PointerWheelEventArgs e)
        {
            if (Child is null) return;

            var st = GetScaleTransform(Child);
            var tt = GetTranslateTransform(Child);
            if (st == null || tt == null) return;

            var delta = e.Delta.Y;
            if (delta <= 0 && (st.ScaleX < .4 || st.ScaleY < .4)) return;
            var zoom = delta > 0 ? .2 : -.2;

            var (relativeX, relativeY) = e.GetCurrentPoint(Child).Position;
            var absoluteX = relativeX * st.ScaleX + tt.X;
            var absoluteY = relativeY * st.ScaleY + tt.Y;

            st.ScaleX += zoom;
            st.ScaleY += zoom;

            tt.X = absoluteX - relativeX * st.ScaleX;
            tt.Y = absoluteY - relativeY * st.ScaleY;

            ZoomChanged?.Invoke(st.ScaleX);
        }


        private Point origin;
        private Point start;

        private void child_MouseLeftButtonDown(object? _, PointerPressedEventArgs e)
        {
            if (Child is null) return;

            var tt = GetTranslateTransform(Child);
            if (tt == null) return;

            start = e.GetCurrentPoint(this).Position;
            origin = new Point(tt.X, tt.Y);
            Cursor = new Cursor(StandardCursorType.Hand);
            e.Pointer.Capture(Child);
        }

        private void child_MouseLeftButtonUp(object? _, PointerReleasedEventArgs e)
        {
            if (Child is null) return;

            e.Pointer.Capture(null);
            Cursor = new Cursor(StandardCursorType.Arrow);
        }

        private void child_MouseMove(object? _, PointerEventArgs e)
        {
            if (Child is null || !Equals(e.Pointer.Captured, Child)) return;

            var st = GetScaleTransform(Child);
            var tt = GetTranslateTransform(Child);
            if (st == null || tt == null) return;
            var (mouseX, mouseY) = e.GetCurrentPoint(this).Position;


            var x = origin.X - start.X + mouseX;
            var left = x - Child.Bounds.Width / 2;
            var right = left + Child.Bounds.Width * st.ScaleX;
            var minX = Bounds.Width / -4;
            var maxX = Bounds.Width / 4;
            if (left < maxX && right > minX) tt.X = x;

            var y = origin.Y - start.Y + mouseY;
            var top = y - Child.Bounds.Height / 2;
            var bottom = top + Child.Bounds.Height * st.ScaleY;
            var minY = Bounds.Height / -4;
            var maxY = Bounds.Height / 4;
            if (top < maxY && bottom > minY) tt.Y = y;


            PositionChanged?.Invoke(tt.X, tt.Y);
        }

        #endregion
    }
}
