using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace WishList.Services.Admin
{
    public static class RangeSliderBehavior
    {
        #region StartThumbDragDeltaCommand

        public static readonly DependencyProperty StartThumbDragDeltaCommandProperty =
            DependencyProperty.RegisterAttached(
                "StartThumbDragDeltaCommand",
                typeof(ICommand),
                typeof(RangeSliderBehavior),
                new PropertyMetadata(null, OnStartThumbDragDeltaCommandChanged));

        public static ICommand GetStartThumbDragDeltaCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(StartThumbDragDeltaCommandProperty);
        }

        public static void SetStartThumbDragDeltaCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(StartThumbDragDeltaCommandProperty, value);
        }

        private static void OnStartThumbDragDeltaCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Thumb thumb)
            {
                thumb.DragDelta -= Thumb_StartDragDelta;
                if (e.NewValue != null)
                {
                    thumb.DragDelta += Thumb_StartDragDelta;
                }
            }
        }

        private static void Thumb_StartDragDelta(object sender, DragDeltaEventArgs e)
        {
            var thumb = (Thumb)sender;
            var command = GetStartThumbDragDeltaCommand(thumb);
            if (command?.CanExecute(e) == true)
            {
                command.Execute(e);
            }
        }

        #endregion

        #region EndThumbDragDeltaCommand

        public static readonly DependencyProperty EndThumbDragDeltaCommandProperty =
            DependencyProperty.RegisterAttached(
                "EndThumbDragDeltaCommand",
                typeof(ICommand),
                typeof(RangeSliderBehavior),
                new PropertyMetadata(null, OnEndThumbDragDeltaCommandChanged));

        public static ICommand GetEndThumbDragDeltaCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(EndThumbDragDeltaCommandProperty);
        }

        public static void SetEndThumbDragDeltaCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(EndThumbDragDeltaCommandProperty, value);
        }

        private static void OnEndThumbDragDeltaCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Thumb thumb)
            {
                thumb.DragDelta -= Thumb_EndDragDelta;
                if (e.NewValue != null)
                {
                    thumb.DragDelta += Thumb_EndDragDelta;
                }
            }
        }

        private static void Thumb_EndDragDelta(object sender, DragDeltaEventArgs e)
        {
            var thumb = (Thumb)sender;
            var command = GetEndThumbDragDeltaCommand(thumb);
            if (command?.CanExecute(e) == true)
            {
                command.Execute(e);
            }
        }

        #endregion
    }
}