using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Data;
using System.ComponentModel;
using System.Reflection;
using WishList.ViewModel.ManagerViewModel;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.IO;

namespace WishList.Services.Manager
{
    public static class TabItemDragDropBehavior
    {
        #region Attached Properties

        public static readonly DependencyProperty IsDragOverProperty =
            DependencyProperty.RegisterAttached(
                "IsDragOver",
                typeof(bool),
                typeof(TabItemDragDropBehavior),
                new PropertyMetadata(false));

        public static bool GetIsDragOver(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsDragOverProperty);
        }

        public static void SetIsDragOver(DependencyObject obj, bool value)
        {
            obj.SetValue(IsDragOverProperty, value);
        }

        public static readonly DependencyProperty IsDragDropEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsDragDropEnabled",
                typeof(bool),
                typeof(TabItemDragDropBehavior),
                new PropertyMetadata(false, OnIsDragDropEnabledChanged));

        public static bool GetIsDragDropEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsDragDropEnabledProperty);
        }

        public static void SetIsDragDropEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsDragDropEnabledProperty, value);
        }

        #endregion

        #region Private Fields

        private static Point _dragStartPoint;
        private static TabItem _draggedTabItem;
        private static TabControl _sourceTabControl;
        private static bool _isDragStarted = false;

        #endregion

        #region Event Handlers

        private static void OnIsDragDropEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tabControl = d as TabControl;
            if (tabControl == null) return;

            if ((bool)e.NewValue)
            {
                tabControl.PreviewMouseLeftButtonDown += TabControl_PreviewMouseLeftButtonDown;
                tabControl.PreviewMouseMove += TabControl_PreviewMouseMove;
                tabControl.PreviewDragOver += TabControl_PreviewDragOver;
                tabControl.Drop += TabControl_Drop;
                tabControl.AllowDrop = true;
            }
            else
            {
                tabControl.PreviewMouseLeftButtonDown -= TabControl_PreviewMouseLeftButtonDown;
                tabControl.PreviewMouseMove -= TabControl_PreviewMouseMove;
                tabControl.PreviewDragOver -= TabControl_PreviewDragOver;
                tabControl.Drop -= TabControl_Drop;
            }
        }

        private static void TabControl_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var tabControl = sender as TabControl;
            if (tabControl == null) return;

            _dragStartPoint = e.GetPosition(tabControl);
            _sourceTabControl = tabControl;
            _isDragStarted = false;

            var tabItem = FindTabItem(tabControl, e.GetPosition(tabControl));
            if (tabItem != null)
            {
                _draggedTabItem = tabItem;
            }
        }

        private static void TabControl_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            var tabControl = sender as TabControl;
            if (tabControl == null || _draggedTabItem == null || _isDragStarted) return;

            Point currentPosition = e.GetPosition(tabControl);
            Vector diff = _dragStartPoint - currentPosition;

            if (e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                 Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                _isDragStarted = true;

                DataObject data = new DataObject();
                data.SetData("TabItem", _draggedTabItem);
                data.SetData("TabControl", tabControl);
                data.SetData("TabItemContent", _draggedTabItem.Content);
                data.SetData("TabItemHeader", _draggedTabItem.Header);

                DragDrop.DoDragDrop(_draggedTabItem, data, DragDropEffects.Move | DragDropEffects.Copy);
                _draggedTabItem = null;
                _isDragStarted = false;
            }
        }

        private static void TabControl_PreviewDragOver(object sender, DragEventArgs e)
        {
            var tabControl = sender as TabControl;
            if (tabControl == null) return;

            // Проверяем, что перетаскивание идет в пределах TabControl
            Point dropPosition = e.GetPosition(tabControl);
            var targetTabItem = FindTabItem(tabControl, dropPosition);

            if (targetTabItem != null)
            {
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                // Если перетаскивание за пределы TabControl, предлагаем создать окно
                e.Effects = DragDropEffects.Copy;
            }

            e.Handled = true;
        }

        private static void TabControl_Drop(object sender, DragEventArgs e)
        {
            var targetTabControl = sender as TabControl;
            var sourceTabItem = e.Data.GetData("TabItem") as TabItem;
            var sourceTabControl = e.Data.GetData("TabControl") as TabControl;

            if (sourceTabItem == null || targetTabControl == null) return;

            // Проверяем позицию курсора относительно TabControl
            Point dropPosition = e.GetPosition(targetTabControl);
            var targetTabItem = FindTabItem(targetTabControl, dropPosition);

            if (sourceTabControl == targetTabControl)
            {
                // Перетаскивание в пределах одного TabControl - меняем порядок
                if (targetTabItem != null && sourceTabItem != targetTabItem)
                {
                    int sourceIndex = targetTabControl.Items.IndexOf(sourceTabItem);
                    int targetIndex = targetTabControl.Items.IndexOf(targetTabItem);

                    if (sourceIndex >= 0 && targetIndex >= 0)
                    {
                        targetTabControl.Items.RemoveAt(sourceIndex);
                        targetTabControl.Items.Insert(targetIndex, sourceTabItem);
                        sourceTabItem.IsSelected = true;
                    }
                }
            }
            else if (sourceTabControl != null && targetTabItem == null)
            {
                // Перетаскивание из отдельного окна обратно в TabControl
                // ИЛИ перетаскивание за пределы вкладок текущего TabControl
                if (sourceTabControl.Parent is DetachedTabWindow detachedWindow)
                {
                    detachedWindow.CloseWithoutReturn();
                }

                sourceTabControl.Items.Remove(sourceTabItem);
                targetTabControl.Items.Add(sourceTabItem);
                sourceTabItem.IsSelected = true;
            }

            ResetDragState();
        }

        #endregion

        #region Window Detach Methods

        public static void HandleWindowDragDrop(object sender, DragEventArgs e)
        {
            var sourceTabItem = e.Data.GetData("TabItem") as TabItem;
            var sourceTabControl = e.Data.GetData("TabControl") as TabControl;

            if (sourceTabItem != null && sourceTabControl != null)
            {
                // Проверяем, что перетаскивание идет из главного окна, а не из отдельного
                if (sourceTabControl.Parent is DetachedTabWindow)
                {
                    e.Effects = DragDropEffects.None;
                    e.Handled = true;
                    return;
                }

                // Получаем позицию относительно окна
                var window = Application.Current.MainWindow;
                Point screenPosition = e.GetPosition(window);

                // Проверяем, что перетаскивание действительно за пределы TabControl
                Point tabControlPosition = e.GetPosition(sourceTabControl);
                var tabItemAtPosition = FindTabItem(sourceTabControl, tabControlPosition);

                // Если не над вкладкой, создаем окно
                if (tabItemAtPosition == null)
                {
                    DetachTabToWindow(sourceTabItem, sourceTabControl, screenPosition);
                    e.Handled = true;
                }
            }
        }

        public static void DetachTabToWindow(TabItem tabItem, TabControl sourceTabControl, Point screenPosition)
        {
            try
            {
                // Получаем DataContext из исходной вкладки
                var originalDataContext = GetDataContextFromTabItem(tabItem);

                // Создаем точную копию содержимого вкладки
                var contentCopy = CreateExactContentCopy(tabItem, originalDataContext);
                var headerCopy = CreateHeaderCopy(tabItem.Header);

                sourceTabControl.Items.Remove(tabItem);

                var newWindow = CreateDetachedWindow(contentCopy, headerCopy, tabItem.Style, sourceTabControl, screenPosition, originalDataContext);
                newWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании окна: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static Window CreateDetachedWindow(object content, object header, Style style, TabControl originalTabControl, Point screenPosition, object dataContext)
        {
            var window = new DetachedTabWindow(content, header, style, originalTabControl, dataContext)
            {
                Title = header?.ToString() ?? "Новая вкладка",
                Width = 1200,
                Height = 800,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = Math.Max(0, screenPosition.X - 100),
                Top = Math.Max(0, screenPosition.Y - 50),
                WindowStyle = WindowStyle.SingleBorderWindow,
                ResizeMode = ResizeMode.CanResize,
                Topmost = true // Временно делаем поверх всех окон
            };

            // Убираем Topmost после показа
            window.Loaded += (s, e) =>
            {
                window.Topmost = false;
                window.Activate(); // Активируем окно
            };

            return window;
        }


        // Получение DataContext из TabItem
        private static object GetDataContextFromTabItem(TabItem tabItem)
        {
            if (tabItem.Content is FrameworkElement contentElement)
            {
                return contentElement.DataContext;
            }
            return null;
        }

        // Создание точной копии содержимого вкладки
        public static UIElement CreateExactContentCopy(TabItem tabItem, object dataContext)
        {
            try
            {
                // Получаем оригинальное содержимое
                var originalContent = tabItem.Content as FrameworkElement;
                if (originalContent == null)
                    return CreateFallbackContent(tabItem.Header?.ToString() ?? "");

                // Создаем копию на основе типа содержимого
                var header = tabItem.Header?.ToString() ?? "";

                if (header.Contains("👥 Клиенты"))
                {
                    return CreateExactClientsContent(dataContext);
                }
                else if (header.Contains("📦 Заказы"))
                {
                    return CreateExactOrdersContent(dataContext);
                }
                else if (header.Contains("📋 Планы работ"))
                {
                    return CreateExactWorkPlansContent(dataContext);
                }
                else
                {
                    // Для неизвестных типов создаем упрощенную копию
                    return CreateSimplifiedContentCopy(originalContent, dataContext);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при создании копии содержимого: {ex.Message}");
                return CreateFallbackContent(tabItem.Header?.ToString() ?? "");
            }
        }

        // Создание упрощенной копии содержимого (запасной вариант)
        private static UIElement CreateSimplifiedContentCopy(FrameworkElement original, object dataContext)
        {
            //var header = tabItem.Header?.ToString() ?? "";

            //if (header.Contains("👥 Клиенты"))
            //{
            //    return CreateExactClientsContent(dataContext);
            //}
            //else if (header.Contains("📦 Заказы"))
            //{
            //    return CreateExactOrdersContent(dataContext);
            //}
            //else if (header.Contains("📋 Планы работ"))
            //{
            //    return CreateExactWorkPlansContent(dataContext);
            //}

            //return CreateFallbackContent(header);

            var grid = new Grid
            {
                DataContext = dataContext,
                Margin = new Thickness(8)
            };

            // Копируем основные свойства
            if (original is Panel originalPanel)
            {
                CopyPanelStructure(originalPanel, grid, dataContext);
            }
            else
            {
                var textBlock = new TextBlock
                {
                    Text = $"Копия: {original.GetType().Name}",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 16
                };
                grid.Children.Add(textBlock);
            }

            return grid;
        }

        private static UIElement CreateFallbackContent(string header)
        {
            return new TextBlock
            {
                Text = $"Содержимое вкладки: {header}",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 16
            };
        }

        // Восстановление привязок команд
        private static void RestoreCommandBindings(UIElement element, object dataContext)
        {
            if (element == null || dataContext == null) return;

            // Рекурсивно обходим все элементы
            RestoreCommandBindingsRecursive(element, dataContext);
        }

        // Восстановление привязок видимости форм
        private static void RestoreFormVisibilityBindings(FrameworkElement element, object dataContext)
        {
            if (element == null || dataContext == null) return;

            // Ищем все Border элементы, которые могут содержать формы
            var borders = FindVisualChildren<Border>(element);
            foreach (var border in borders)
            {
                var visibilityBinding = border.GetBindingExpression(Border.VisibilityProperty);
                if (visibilityBinding == null)
                {
                    // Восстанавливаем привязки видимости на основе контекста
                    var tag = border.Tag?.ToString() ?? "";
                    var name = border.Name ?? "";

                    if (tag.Contains("ClientForm") || name.Contains("ClientForm") ||
                        (border.Child is Grid childGrid &&
                         (childGrid.Children.OfType<TextBlock>().Any(tb => tb.Text.Contains("клиент")) ||
                          childGrid.Children.OfType<Button>().Any(btn => btn.Content?.ToString()?.Contains("клиент") == true))))
                    {
                        SetBinding(border, Border.VisibilityProperty, "IsClientFormVisible", dataContext,
                                  BindingMode.OneWay, UpdateSourceTrigger.PropertyChanged,
                                  new BooleanToVisibilityConverter());
                    }
                    else if (tag.Contains("OrderForm") || name.Contains("OrderForm") ||
                             (border.Child is Grid childGrid2 &&
                              (childGrid2.Children.OfType<TextBlock>().Any(tb => tb.Text.Contains("заказ")) ||
                               childGrid2.Children.OfType<Button>().Any(btn => btn.Content?.ToString()?.Contains("заказ") == true))))
                    {
                        SetBinding(border, Border.VisibilityProperty, "IsOrderFormVisible", dataContext,
                                  BindingMode.OneWay, UpdateSourceTrigger.PropertyChanged,
                                  new BooleanToVisibilityConverter());
                    }
                    else if (tag.Contains("WorkPlanForm") || name.Contains("WorkPlanForm") ||
                             (border.Child is Grid childGrid3 &&
                              (childGrid3.Children.OfType<TextBlock>().Any(tb => tb.Text.Contains("план")) ||
                               childGrid3.Children.OfType<Button>().Any(btn => btn.Content?.ToString()?.Contains("план") == true))))
                    {
                        SetBinding(border, Border.VisibilityProperty, "IsWorkPlanFormVisible", dataContext,
                                  BindingMode.OneWay, UpdateSourceTrigger.PropertyChanged,
                                  new BooleanToVisibilityConverter());
                    }
                }
            }
        }

        // Универсальный метод для восстановления всех привязок команд и видимости
        private static void RestoreAllCommandBindings(FrameworkElement element, object dataContext)
        {
            if (element == null || dataContext == null) return;

            // Восстанавливаем привязки команд
            RestoreCommandBindingsRecursive(element, dataContext);

            // Восстанавливаем привязки видимости форм
            RestoreFormVisibilityBindings(element, dataContext);
        }

        // Вспомогательный метод для поиска дочерних элементов определенного типа
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) yield break;

            var queue = new Queue<DependencyObject>();
            queue.Enqueue(parent);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current is T found)
                    yield return found;

                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(current); i++)
                {
                    queue.Enqueue(VisualTreeHelper.GetChild(current, i));
                }
            }
        }

        private static void RestoreCommandBindingsRecursive(DependencyObject parent, object dataContext)
        {
            if (parent == null) return;

            var childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is Button button)
                {
                    RestoreButtonCommand(button, dataContext);
                }
                else if (child is TextBox textBox)
                {
                    RestoreTextBoxBindings(textBox, dataContext);
                }
                else if (child is ComboBox comboBox)
                {
                    RestoreComboBoxBindings(comboBox, dataContext);
                }
                else if (child is DataGrid dataGrid)
                {
                    RestoreDataGridBindings(dataGrid, dataContext);
                }

                // Рекурсивный обход дочерних элементов
                RestoreCommandBindingsRecursive(child, dataContext);
            }
        }

        // Копирование структуры панели
        private static void CopyPanelStructure(Panel original, Panel copy, object dataContext)
        {
            // Копируем строки и колонки для Grid
            if (original is Grid originalGrid && copy is Grid copyGrid)
            {
                foreach (var rowDef in originalGrid.RowDefinitions)
                {
                    copyGrid.RowDefinitions.Add(new RowDefinition { Height = rowDef.Height });
                }
                foreach (var colDef in originalGrid.ColumnDefinitions)
                {
                    copyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = colDef.Width });
                }
            }

            // Рекурсивно копируем дочерние элементы
            foreach (var child in original.Children)
            {
                if (child is FrameworkElement childElement)
                {
                    var childCopy = CreateElementCopy(childElement, dataContext);
                    if (childCopy != null)
                    {
                        CopyElementProperties(childElement, childCopy);

                        // Копируем привязки строк и колонок для Grid
                        if (original is Grid)
                        {
                            Grid.SetRow(childCopy, Grid.GetRow(childElement));
                            Grid.SetColumn(childCopy, Grid.GetColumn(childElement));
                            Grid.SetRowSpan(childCopy, Grid.GetRowSpan(childElement));
                            Grid.SetColumnSpan(childCopy, Grid.GetColumnSpan(childElement));
                        }

                        copy.Children.Add(childCopy);
                    }
                }
            }
        }

        // Создание копии элемента
        private static FrameworkElement CreateElementCopy(FrameworkElement original, object dataContext)
        {
            if (original is TextBlock textBlock)
            {
                return new TextBlock
                {
                    Text = textBlock.Text,
                    FontSize = textBlock.FontSize,
                    FontWeight = textBlock.FontWeight,
                    Foreground = textBlock.Foreground,
                    HorizontalAlignment = textBlock.HorizontalAlignment,
                    VerticalAlignment = textBlock.VerticalAlignment,
                    Margin = textBlock.Margin
                };
            }
            else if (original is Button button)
            {
                var newButton = new Button
                {
                    Content = button.Content,
                    Style = button.Style,
                    Background = button.Background,
                    Foreground = button.Foreground,
                    FontSize = button.FontSize,
                    FontWeight = button.FontWeight,
                    Padding = button.Padding,
                    Margin = button.Margin,
                    MinWidth = button.MinWidth,
                    HorizontalAlignment = button.HorizontalAlignment,
                    VerticalAlignment = button.VerticalAlignment
                };

                // Восстанавливаем команду
                RestoreButtonCommand(newButton, dataContext);
                return newButton;
            }
            else if (original is TextBox textBox)
            {
                var newTextBox = new TextBox
                {
                    Text = textBox.Text,
                    Style = textBox.Style,
                    Tag = textBox.Tag,
                    MinWidth = textBox.MinWidth,
                    Margin = textBox.Margin,
                    Height = textBox.Height,
                    TextWrapping = textBox.TextWrapping,
                    AcceptsReturn = textBox.AcceptsReturn,
                    VerticalScrollBarVisibility = textBox.VerticalScrollBarVisibility
                };

                // Восстанавливаем привязку
                RestoreTextBoxBindings(newTextBox, dataContext);
                return newTextBox;
            }
            else if (original is DataGrid dataGrid)
            {
                var newDataGrid = new DataGrid
                {
                    Style = dataGrid.Style,
                    ColumnHeaderStyle = dataGrid.ColumnHeaderStyle,
                    RowStyle = dataGrid.RowStyle,
                    CellStyle = dataGrid.CellStyle,
                    AutoGenerateColumns = dataGrid.AutoGenerateColumns,
                    VerticalAlignment = dataGrid.VerticalAlignment,
                    Margin = dataGrid.Margin
                };

                // Восстанавливаем привязку ItemsSource
                RestoreDataGridBindings(newDataGrid, dataContext);

                // Копируем колонки
                foreach (var column in dataGrid.Columns)
                {
                    if (column is DataGridTextColumn textColumn)
                    {
                        var newColumn = new DataGridTextColumn
                        {
                            Header = textColumn.Header,
                            Binding = textColumn.Binding,
                            Width = textColumn.Width,
                            MinWidth = textColumn.MinWidth
                        };
                        newDataGrid.Columns.Add(newColumn);
                    }
                    else if (column is DataGridTemplateColumn templateColumn)
                    {
                        var newColumn = new DataGridTemplateColumn
                        {
                            Header = templateColumn.Header,
                            Width = templateColumn.Width,
                            MinWidth = templateColumn.MinWidth,
                            CellTemplate = templateColumn.CellTemplate
                        };
                        newDataGrid.Columns.Add(newColumn);
                    }
                }

                return newDataGrid;
            }
            else if (original is Border border)
            {
                var newBorder = new Border
                {
                    Background = border.Background,
                    BorderBrush = border.BorderBrush,
                    BorderThickness = border.BorderThickness,
                    CornerRadius = border.CornerRadius,
                    Padding = border.Padding,
                    Margin = border.Margin
                };

                if (border.Child is FrameworkElement child)
                {
                    var childCopy = CreateElementCopy(child, dataContext);
                    if (childCopy != null)
                    {
                        newBorder.Child = childCopy;
                    }
                }

                // Восстанавливаем привязку Visibility
                if (border.GetBindingExpression(Border.VisibilityProperty) != null)
                {
                    var binding = border.GetBindingExpression(Border.VisibilityProperty)?.ParentBinding;
                    if (binding != null)
                    {
                        BindingOperations.SetBinding(newBorder, Border.VisibilityProperty, binding);
                    }
                }

                return newBorder;
            }
            else if (original is StackPanel stackPanel)
            {
                var newStackPanel = new StackPanel
                {
                    Orientation = stackPanel.Orientation,
                    HorizontalAlignment = stackPanel.HorizontalAlignment,
                    VerticalAlignment = stackPanel.VerticalAlignment,
                    Margin = stackPanel.Margin
                };

                foreach (var child in stackPanel.Children)
                {
                    if (child is FrameworkElement childElement)
                    {
                        var childCopy = CreateElementCopy(childElement, dataContext);
                        if (childCopy != null)
                        {
                            newStackPanel.Children.Add(childCopy);
                        }
                    }
                }

                return newStackPanel;
            }
            else if (original is Grid grid)
            {
                var newGrid = new Grid
                {
                    Margin = grid.Margin
                };

                CopyPanelStructure(grid, newGrid, dataContext);
                return newGrid;
            }

            return null;
        }

        private static void CopyElementProperties(FrameworkElement original, FrameworkElement copy)
        {
            copy.Width = original.Width;
            copy.Height = original.Height;
            copy.HorizontalAlignment = original.HorizontalAlignment;
            copy.VerticalAlignment = original.VerticalAlignment;
            copy.Margin = original.Margin;
            copy.DataContext = original.DataContext;
        }

        private static void RestoreButtonCommand(Button button, object dataContext)
        {
            //var content = button.Content?.ToString() ?? "";

            //// Определяем команду по содержимому кнопки
            //ICommand command = null;
            //var viewModel = dataContext as ManagerWindowViewModel;

            //if (viewModel != null)
            //{
            //    if (content.Contains("Добавить клиента") || content.Contains("➕"))
            //        command = viewModel.AddClientCommand;
            //    else if (content.Contains("Обновить") || content.Contains("🔄"))
            //        command = viewModel.RefreshClientsCommand;
            //    else if (content.Contains("Сохранить") || content.Contains("💾"))
            //        command = viewModel.SaveClientCommand;
            //    else if (content.Contains("Отмена") || content.Contains("❌"))
            //        command = viewModel.CancelClientCommand;
            //    else if (content.Contains("Создать заказ"))
            //        button.Command = viewModel.AddOrderCommand;
            //    else if (content.Contains("План работ"))
            //        command = viewModel.ShowWorkPlanCommand;
            //    else if (content.Contains("Сохранить заказ"))
            //        command = viewModel.SaveOrderCommand;
            //    else if (content.Contains("Создать план"))
            //        command = viewModel.AddWorkPlanCommand;
            //    else if (content.Contains("Сохранить план"))
            //        command = viewModel.SaveWorkPlanCommand;
            //}

            //if (command != null)
            //{
            //    button.Command = command;
            //}

            //var content = button.Content?.ToString() ?? "";
            //var viewModel = dataContext as ManagerWindowViewModel;

            //if (viewModel != null)
            //{
            //    if (content.Contains("Добавить клиента") || content.Contains("➕"))
            //        button.Command = viewModel.AddClientCommand;
            //    else if (content.Contains("Обновить") || content.Contains("🔄"))
            //        button.Command = viewModel.RefreshClientsCommand;
            //    else if (content.Contains("Сохранить") && content.Contains("клиент"))
            //        button.Command = viewModel.SaveClientCommand;
            //    else if (content.Contains("Отмена") && content.Contains("клиент"))
            //        button.Command = viewModel.CancelClientCommand;
            //    else if (content.Contains("Создать заказ"))
            //        button.Command = viewModel.AddOrderCommand;
            //    else if (content.Contains("План работ"))
            //        button.Command = viewModel.ShowWorkPlanCommand;
            //    else if (content.Contains("Сохранить") && content.Contains("заказ"))
            //        button.Command = viewModel.SaveOrderCommand;
            //    else if (content.Contains("Отмена") && content.Contains("заказ"))
            //        button.Command = viewModel.CancelOrderCommand;
            //    else if (content.Contains("Создать план"))
            //        button.Command = viewModel.AddWorkPlanCommand;
            //    else if (content.Contains("Сохранить") && content.Contains("план"))
            //        button.Command = viewModel.SaveWorkPlanCommand;
            //    else if (content.Contains("Отмена") && content.Contains("план"))
            //        button.Command = viewModel.CancelWorkPlanCommand;
            //}

            var content = button.Content?.ToString() ?? "";
            var viewModel = dataContext as ManagerWindowViewModel;

            if (viewModel == null) return;

            // Определяем команду по содержимому кнопки
            if (content.Contains("Добавить клиента") || (content.Contains("➕") && content.Contains("клиента")))
            {
                button.Command = viewModel.AddClientCommand;
            }
            else if (content.Contains("Добавить заказ") || (content.Contains("➕") && content.Contains("заказ")))
            {
                button.Command = viewModel.AddOrderCommand;
            }
            else if (content.Contains("Добавить план") || (content.Contains("➕") && content.Contains("план")))
            {
                button.Command = viewModel.AddWorkPlanCommand;
            }
            else if (content.Contains("Обновить") || content.Contains("🔄"))
            {
                if (content.Contains("клиент") || button.Name?.Contains("Client") == true)
                    button.Command = viewModel.RefreshClientsCommand;
                else if (content.Contains("заказ") || button.Name?.Contains("Order") == true)
                    button.Command = viewModel.RefreshOrdersCommand;
                else if (content.Contains("план") || button.Name?.Contains("WorkPlan") == true)
                    button.Command = viewModel.RefreshWorkPlansCommand;
            }
            else if (content.Contains("Сохранить") && content.Contains("клиент"))
            {
                button.Command = viewModel.SaveClientCommand;
            }
            else if (content.Contains("Отмена") && content.Contains("клиент"))
            {
                button.Command = viewModel.CancelClientCommand;
            }
            else if (content.Contains("Создать заказ") || (content.Contains("➕") && content.Contains("заказ")))
            {
                button.Command = viewModel.AddOrderCommand;
            }
            else if (content.Contains("План работ"))
            {
                button.Command = viewModel.ShowWorkPlanCommand;
            }
            else if (content.Contains("Сохранить") && content.Contains("заказ"))
            {
                button.Command = viewModel.SaveOrderCommand;
            }
            else if (content.Contains("Отмена") && content.Contains("заказ"))
            {
                button.Command = viewModel.CancelOrderCommand;
            }
            else if (content.Contains("Создать план") || (content.Contains("➕") && content.Contains("план")))
            {
                button.Command = viewModel.AddWorkPlanCommand;
            }
            else if (content.Contains("Сохранить") && content.Contains("план"))
            {
                button.Command = viewModel.SaveWorkPlanCommand;
            }
            else if (content.Contains("Отмена") && content.Contains("план"))
            {
                button.Command = viewModel.CancelWorkPlanCommand;
            }
            else if (content.Contains("✏️") || content.Contains("Редактировать"))
            {
                // Для кнопок редактирования в DataGrid
                var commandBinding = button.GetBindingExpression(Button.CommandProperty);
                if (commandBinding == null)
                {
                    // Восстанавливаем привязку команды редактирования
                    var parentDataGrid = FindParent<DataGrid>(button);
                    if (parentDataGrid != null)
                    {
                        if (parentDataGrid.Name?.Contains("Client") == true ||
                            parentDataGrid.Tag?.ToString()?.Contains("клиент") == true)
                        {
                            button.Command = viewModel.EditClientCommand;
                        }
                        else if (parentDataGrid.Name?.Contains("Order") == true ||
                                 parentDataGrid.Tag?.ToString()?.Contains("заказ") == true)
                        {
                            button.Command = viewModel.EditOrderCommand;
                        }
                        else if (parentDataGrid.Name?.Contains("WorkPlan") == true ||
                                 parentDataGrid.Tag?.ToString()?.Contains("план") == true)
                        {
                            button.Command = viewModel.EditWorkPlanCommand;
                        }
                    }
                }
            }

            else if (content.Contains("🗑️") || content.Contains("Удалить"))
            {
                // Для кнопок удаления в DataGrid
                var commandBinding = button.GetBindingExpression(Button.CommandProperty);
                if (commandBinding == null)
                {
                    // Восстанавливаем привязку команды удаления
                    var parentDataGrid = FindParent<DataGrid>(button);
                    if (parentDataGrid != null)
                    {
                        if (parentDataGrid.Name?.Contains("Client") == true ||
                            parentDataGrid.Tag?.ToString()?.Contains("клиент") == true)
                        {
                            button.Command = viewModel.DeleteClientCommand;
                        }
                        else if (parentDataGrid.Name?.Contains("Order") == true ||
                                 parentDataGrid.Tag?.ToString()?.Contains("заказ") == true)
                        {
                            button.Command = viewModel.DeleteOrderCommand;
                        }
                        else if (parentDataGrid.Name?.Contains("WorkPlan") == true ||
                                 parentDataGrid.Tag?.ToString()?.Contains("план") == true)
                        {
                            button.Command = viewModel.DeleteWorkPlanCommand;
                        }
                    }
                }
            }
        }

        // Вспомогательный метод для поиска родительского элемента определенного типа
        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            if (child == null) return null;

            DependencyObject parent = VisualTreeHelper.GetParent(child);

            while (parent != null)
            {
                if (parent is T parentOfType)
                {
                    return parentOfType;
                }
                parent = VisualTreeHelper.GetParent(parent);
            }

            return null;
        }

        private static void RestoreTextBoxBindings(TextBox textBox, object dataContext)
        {
            var tag = textBox.Tag?.ToString() ?? "";
            var viewModel = dataContext as ManagerWindowViewModel;

            if (viewModel != null)
            {
                if (tag.Contains("компании") || tag.Contains("компанию"))
                    SetBinding(textBox, TextBox.TextProperty, "CurrentClient.CompanyName", dataContext, BindingMode.TwoWay);
                else if (tag.Contains("контактного лица") || tag.Contains("ФИО"))
                    SetBinding(textBox, TextBox.TextProperty, "CurrentClient.ContactPerson", dataContext, BindingMode.TwoWay);
                else if (tag.Contains("email") || tag.Contains("Email"))
                    SetBinding(textBox, TextBox.TextProperty, "CurrentClient.Email", dataContext, BindingMode.TwoWay);
                else if (tag.Contains("телефон") || tag.Contains("Телефон"))
                    SetBinding(textBox, TextBox.TextProperty, "CurrentClient.Phone", dataContext, BindingMode.TwoWay);
                else if (tag.Contains("адрес") || tag.Contains("Адрес"))
                    SetBinding(textBox, TextBox.TextProperty, "CurrentClient.Address", dataContext, BindingMode.TwoWay);
                else if (tag.Contains("название заказа"))
                    SetBinding(textBox, TextBox.TextProperty, "CurrentOrder.Title", dataContext, BindingMode.TwoWay);
                else if (tag.Contains("описание заказа"))
                    SetBinding(textBox, TextBox.TextProperty, "CurrentOrder.Description", dataContext, BindingMode.TwoWay);
                else if (tag.Contains("часы") || tag.Contains("Часы"))
                    SetBinding(textBox, TextBox.TextProperty, "CurrentOrder.EstimatedHours", dataContext, BindingMode.TwoWay);
                else if (tag.Contains("описание плана"))
                    SetBinding(textBox, TextBox.TextProperty, "CurrentWorkPlan.PlanDescription", dataContext, BindingMode.TwoWay);
                else if (tag.Contains("шаги тестирования"))
                    SetBinding(textBox, TextBox.TextProperty, "CurrentWorkPlan.TestSteps", dataContext, BindingMode.TwoWay);
                else if (tag.Contains("Поиск по компании"))
                    SetBinding(textBox, TextBox.TextProperty, "ClientSearchText", dataContext, BindingMode.TwoWay);
                else if (tag.Contains("Поиск по названию"))
                    SetBinding(textBox, TextBox.TextProperty, "OrderSearchText", dataContext, BindingMode.TwoWay);
                else if (tag.Contains("Поиск по названию или плану"))
                    SetBinding(textBox, TextBox.TextProperty, "WorkPlanSearchText", dataContext, BindingMode.TwoWay);
            }
        }

        private static void RestoreComboBoxBindings(ComboBox comboBox, object dataContext)
        {
            // Восстанавливаем привязки для ComboBox на основе контекста
            SetBinding(comboBox, ComboBox.ItemsSourceProperty, "AllClients", dataContext);
            SetBinding(comboBox, ComboBox.SelectedValueProperty, "CurrentOrder.ClientId", dataContext, BindingMode.TwoWay);
        }

        private static void RestoreDataGridBindings(DataGrid dataGrid, object dataContext)
        {
            var header = dataGrid.Tag?.ToString() ?? "";

            if (header.Contains("клиент") || dataGrid.Name?.Contains("Client") == true)
            {
                SetBinding(dataGrid, DataGrid.ItemsSourceProperty, "FilteredClients", dataContext);
            }
            else if (header.Contains("заказ") || dataGrid.Name?.Contains("Order") == true)
            {
                SetBinding(dataGrid, DataGrid.ItemsSourceProperty, "FilteredOrders", dataContext);
            }
            else if (header.Contains("план") || dataGrid.Name?.Contains("WorkPlan") == true)
            {
                SetBinding(dataGrid, DataGrid.ItemsSourceProperty, "FilteredWorkPlans", dataContext);
            }
        }

        // Создание точной копии содержимого для вкладки "Клиенты"
        public static UIElement CreateExactClientsContent(object dataContext)
        {
            try
            {
                var grid = new Grid
                {
                    Margin = new Thickness(8),
                    UseLayoutRounding = true,
                    DataContext = dataContext
                };

                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                // Панель управления клиентами
                var controlGrid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
                controlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                controlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                controlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var titleText = new TextBlock
                {
                    Text = "Управление клиентами",
                    Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 16, 0)
                };
                Grid.SetColumn(titleText, 0);
                controlGrid.Children.Add(titleText);

                var buttonsPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                Grid.SetColumn(buttonsPanel, 2);

                var addButton = new Button
                {
                    Content = "➕ Добавить клиента",
                    Style = (Style)Application.Current.FindResource("ModernButton"),
                    Background = (Brush)Application.Current.FindResource("SuccessBrush"),
                    Margin = new Thickness(0, 0, 8, 0),
                    MinWidth = 120,
                    Padding = new Thickness(12, 8, 12, 8)
                };
                SetBinding(addButton, Button.CommandProperty, "AddClientCommand", dataContext);

                var refreshButton = new Button
                {
                    Content = "🔄 Обновить",
                    Style = (Style)Application.Current.FindResource("ModernButton"),
                    Background = (Brush)Application.Current.FindResource("WarningBrush"),
                    MinWidth = 100,
                    Padding = new Thickness(12, 8, 12, 8)
                };
                SetBinding(refreshButton, Button.CommandProperty, "RefreshClientsCommand", dataContext);

                buttonsPanel.Children.Add(addButton);
                buttonsPanel.Children.Add(refreshButton);
                controlGrid.Children.Add(buttonsPanel);

                Grid.SetRow(controlGrid, 0);
                grid.Children.Add(controlGrid);

                // Поиск клиентов
                var searchBorder = new Border
                {
                    Background = (Brush)Application.Current.FindResource("SecondaryBrush"),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8),
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var searchGrid = new Grid();
                searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var searchIcon = new TextBlock
                {
                    Text = "🔍",
                    FontSize = 13,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(4, 0, 8, 0)
                };
                Grid.SetColumn(searchIcon, 0);
                searchGrid.Children.Add(searchIcon);

                var searchBox = new TextBox
                {
                    Style = (Style)Application.Current.FindResource("ModernTextBox"),
                    Tag = "Поиск по компании, контактному лицу, email или телефону...",
                    MinWidth = 200,
                    Margin = new Thickness(0, 0, 8, 0)
                };
                SetBinding(searchBox, TextBox.TextProperty, "ClientSearchText", dataContext, BindingMode.TwoWay, UpdateSourceTrigger.PropertyChanged);
                Grid.SetColumn(searchBox, 1);
                searchGrid.Children.Add(searchBox);

                var searchResultText = new TextBlock
                {
                    Foreground = (Brush)Application.Current.FindResource("TextSecondaryBrush"),
                    FontSize = 11,
                    VerticalAlignment = VerticalAlignment.Center
                };
                SetBinding(searchResultText, TextBlock.TextProperty, "FilteredClients.Count", dataContext, BindingMode.OneWay, UpdateSourceTrigger.PropertyChanged,
                    new StringFormatConverter() { StringFormat = "Найдено: {0}" });
                Grid.SetColumn(searchResultText, 2);
                searchGrid.Children.Add(searchResultText);

                searchBorder.Child = searchGrid;
                Grid.SetRow(searchBorder, 1);
                grid.Children.Add(searchBorder);

                // Таблица клиентов
                var dataGrid = new DataGrid
                {
                    Style = (Style)Application.Current.FindResource("ModernDataGrid"),
                    ColumnHeaderStyle = (Style)Application.Current.FindResource("ModernDataGridColumnHeader"),
                    RowStyle = (Style)Application.Current.FindResource("ModernDataGridRow"),
                    CellStyle = (Style)Application.Current.FindResource("ModernDataGridCell"),
                    AutoGenerateColumns = false,
                    VerticalAlignment = VerticalAlignment.Stretch
                };
                SetBinding(dataGrid, DataGrid.ItemsSourceProperty, "FilteredClients", dataContext);

                // Колонки таблицы
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "№", Binding = new Binding("OrderNumber"), Width = 60 });
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "Компания", Binding = new Binding("CompanyName"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 150 });
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "Контактное лицо", Binding = new Binding("ContactPerson"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 120 });
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "Email", Binding = new Binding("Email"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 150 });
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "Телефон", Binding = new Binding("Phone"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 100 });
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "Адрес", Binding = new Binding("Address"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 120 });
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "Дата регистрации", Binding = new Binding("CreatedDate") { StringFormat = "dd.MM.yyyy HH:mm" }, Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 120 });

                // Колонка действий
                var actionsColumn = new DataGridTemplateColumn
                {
                    Header = "Действия",
                    Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                    MinWidth = 100
                };

                var actionsTemplate = new DataTemplate();
                var actionsFactory = new FrameworkElementFactory(typeof(StackPanel));
                actionsFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
                actionsFactory.SetValue(StackPanel.HorizontalAlignmentProperty, HorizontalAlignment.Center);

                // Кнопка редактирования
                var editButtonFactory = new FrameworkElementFactory(typeof(Button));
                editButtonFactory.SetValue(Button.ContentProperty, "✏️");
                editButtonFactory.SetValue(Button.StyleProperty, Application.Current.FindResource("TextButton"));
                editButtonFactory.SetValue(Button.ToolTipProperty, "Редактировать");
                editButtonFactory.SetValue(Button.PaddingProperty, new Thickness(6, 3, 6, 3));

                var editCommandBinding = new Binding("DataContext.EditClientCommand");
                editCommandBinding.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGrid), 1);
                editButtonFactory.SetBinding(Button.CommandProperty, editCommandBinding);

                var editParameterBinding = new Binding("Id");
                editButtonFactory.SetBinding(Button.CommandParameterProperty, editParameterBinding);

                // Кнопка удаления
                var deleteButtonFactory = new FrameworkElementFactory(typeof(Button));
                deleteButtonFactory.SetValue(Button.ContentProperty, "🗑️");
                deleteButtonFactory.SetValue(Button.StyleProperty, Application.Current.FindResource("TextButton"));
                deleteButtonFactory.SetValue(Button.ToolTipProperty, "Удалить");
                deleteButtonFactory.SetValue(Button.PaddingProperty, new Thickness(6, 3, 6, 3));
                deleteButtonFactory.SetValue(Button.MarginProperty, new Thickness(4, 0, 0, 0));
                deleteButtonFactory.SetValue(Button.ForegroundProperty, Application.Current.FindResource("ErrorBrush"));

                var deleteCommandBinding = new Binding("DataContext.DeleteClientCommand");
                deleteCommandBinding.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGrid), 1);
                deleteButtonFactory.SetBinding(Button.CommandProperty, deleteCommandBinding);

                var deleteParameterBinding = new Binding("Id");
                deleteButtonFactory.SetBinding(Button.CommandParameterProperty, deleteParameterBinding);

                actionsFactory.AppendChild(editButtonFactory);
                actionsFactory.AppendChild(deleteButtonFactory);
                actionsTemplate.VisualTree = actionsFactory;
                actionsColumn.CellTemplate = actionsTemplate;

                dataGrid.Columns.Add(actionsColumn);

                Grid.SetRow(dataGrid, 3);
                grid.Children.Add(dataGrid);

                return grid;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при создании содержимого клиентов: {ex.Message}");
                return CreateFallbackContent("👥 Клиенты");
            }
        }

        // Создание точной копии содержимого для вкладки "Заказы"
        public static UIElement CreateExactOrdersContent(object dataContext)
        {
            var grid = new Grid
            {
                Margin = new Thickness(8),
                UseLayoutRounding = true,
                DataContext = dataContext
            };

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Адаптивная панель управления заказами
            var controlPanelGrid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            controlPanelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            controlPanelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            controlPanelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleText = new TextBlock
            {
                Text = "Управление заказами",
                Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 16, 0)
            };
            Grid.SetColumn(titleText, 0);
            controlPanelGrid.Children.Add(titleText);

            var buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(buttonsPanel, 2);

            var addButton = new Button
            {
                Content = "➕ Создать заказ",
                Style = (Style)Application.Current.FindResource("ModernButton"),
                Background = (Brush)Application.Current.FindResource("AccentBrush"),
                Margin = new Thickness(0, 0, 8, 0),
                MinWidth = 120,
                Padding = new Thickness(12, 8, 12, 8)
            };
            SetBinding(addButton, Button.CommandProperty, "AddOrderCommand", dataContext);

            var refreshButton = new Button
            {
                Content = "🔄 Обновить",
                Style = (Style)Application.Current.FindResource("ModernButton"),
                Background = (Brush)Application.Current.FindResource("WarningBrush"),
                Margin = new Thickness(0, 0, 8, 0),
                MinWidth = 100,
                Padding = new Thickness(12, 8, 12, 8)
            };
            SetBinding(refreshButton, Button.CommandProperty, "RefreshOrdersCommand", dataContext);

            var workPlanButton = new Button
            {
                Content = "📋 План работ",
                Style = (Style)Application.Current.FindResource("ModernButton"),
                Background = (Brush)Application.Current.FindResource("InfoBrush"),
                MinWidth = 100,
                Padding = new Thickness(12, 8, 12, 8)
            };
            SetBinding(workPlanButton, Button.CommandProperty, "ShowWorkPlanCommand", dataContext);

            buttonsPanel.Children.Add(addButton);
            buttonsPanel.Children.Add(refreshButton);
            buttonsPanel.Children.Add(workPlanButton);
            controlPanelGrid.Children.Add(buttonsPanel);

            Grid.SetRow(controlPanelGrid, 0);
            grid.Children.Add(controlPanelGrid);

            // Поиск заказов
            var searchBorder = new Border
            {
                Background = (Brush)Application.Current.FindResource("SecondaryBrush"),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var searchGrid = new Grid();
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var searchIcon = new TextBlock
            {
                Text = "🔍",
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 8, 0)
            };
            Grid.SetColumn(searchIcon, 0);
            searchGrid.Children.Add(searchIcon);

            var searchBox = new TextBox
            {
                Style = (Style)Application.Current.FindResource("ModernTextBox"),
                Tag = "Поиск по названию, клиенту, категории или приоритету...",
                MinWidth = 200,
                Margin = new Thickness(0, 0, 8, 0)
            };
            SetBinding(searchBox, TextBox.TextProperty, "OrderSearchText", dataContext, BindingMode.TwoWay, UpdateSourceTrigger.PropertyChanged);
            Grid.SetColumn(searchBox, 1);
            searchGrid.Children.Add(searchBox);

            var searchResultText = new TextBlock
            {
                Foreground = (Brush)Application.Current.FindResource("TextSecondaryBrush"),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            };
            SetBinding(searchResultText, TextBlock.TextProperty, "FilteredOrders.Count", dataContext, BindingMode.OneWay, UpdateSourceTrigger.PropertyChanged,
                new StringFormatConverter() { StringFormat = "Найдено: {0}" });
            Grid.SetColumn(searchResultText, 2);
            searchGrid.Children.Add(searchResultText);

            searchBorder.Child = searchGrid;
            Grid.SetRow(searchBorder, 1);
            grid.Children.Add(searchBorder);

            // Форма заказа
            var formBorder = new Border
            {
                Background = (Brush)Application.Current.FindResource("SecondaryBrush"),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 12),
                BorderThickness = new Thickness(1),
                BorderBrush = (Brush)Application.Current.FindResource("BorderBrush")
            };
            SetBinding(formBorder, Border.VisibilityProperty, "IsOrderFormVisible", dataContext, converter: new BooleanToVisibilityConverter());

            var formGrid = new Grid();
            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Первая строка формы
            var firstRow = new UniformGrid { Columns = 4, Margin = new Thickness(0, 0, 0, 8) };

            var clientStack = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
            var clientLabel = new TextBlock
            {
                Text = "Клиент *",
                Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            var clientComboBox = new ComboBox
            {
                Style = (Style)Application.Current.FindResource("ModernComboBox"),
                DisplayMemberPath = "CompanyName",
                SelectedValuePath = "Id"
            };
            SetBinding(clientComboBox, ComboBox.ItemsSourceProperty, "AllClients", dataContext);
            SetBinding(clientComboBox, ComboBox.SelectedValueProperty, "CurrentOrder.ClientId", dataContext, BindingMode.TwoWay);
            clientStack.Children.Add(clientLabel);
            clientStack.Children.Add(clientComboBox);

            var categoryStack = new StackPanel { Margin = new Thickness(4, 0, 4, 0) };
            var categoryLabel = new TextBlock
            {
                Text = "Категория *",
                Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            var categoryComboBox = new ComboBox
            {
                Style = (Style)Application.Current.FindResource("ModernComboBox"),
                DisplayMemberPath = "Name",
                SelectedValuePath = "Id"
            };
            SetBinding(categoryComboBox, ComboBox.ItemsSourceProperty, "AllCategories", dataContext);
            SetBinding(categoryComboBox, ComboBox.SelectedValueProperty, "CurrentOrder.CategoryId", dataContext, BindingMode.TwoWay);
            categoryStack.Children.Add(categoryLabel);
            categoryStack.Children.Add(categoryComboBox);

            var priorityStack = new StackPanel { Margin = new Thickness(4, 0, 4, 0) };
            var priorityLabel = new TextBlock
            {
                Text = "Приоритет *",
                Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            var priorityComboBox = new ComboBox
            {
                Style = (Style)Application.Current.FindResource("ModernComboBox"),
                DisplayMemberPath = "Name",
                SelectedValuePath = "Id"
            };
            SetBinding(priorityComboBox, ComboBox.ItemsSourceProperty, "AllPriorities", dataContext);
            SetBinding(priorityComboBox, ComboBox.SelectedValueProperty, "CurrentOrder.PriorityId", dataContext, BindingMode.TwoWay);
            priorityStack.Children.Add(priorityLabel);
            priorityStack.Children.Add(priorityComboBox);

            var statusStack = new StackPanel { Margin = new Thickness(8, 0, 0, 0) };
            var statusLabel = new TextBlock
            {
                Text = "Статус",
                Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            var statusComboBox = new ComboBox
            {
                Style = (Style)Application.Current.FindResource("ModernComboBox"),
                DisplayMemberPath = "Name",
                SelectedValuePath = "Id"
            };
            SetBinding(statusComboBox, ComboBox.ItemsSourceProperty, "AllStatuses", dataContext);
            SetBinding(statusComboBox, ComboBox.SelectedValueProperty, "CurrentOrder.StatusId", dataContext, BindingMode.TwoWay);
            statusStack.Children.Add(statusLabel);
            statusStack.Children.Add(statusComboBox);

            firstRow.Children.Add(clientStack);
            firstRow.Children.Add(categoryStack);
            firstRow.Children.Add(priorityStack);
            firstRow.Children.Add(statusStack);
            Grid.SetRow(firstRow, 0);
            formGrid.Children.Add(firstRow);

            // Вторая строка формы
            var secondRow = new Grid();
            secondRow.Margin = new Thickness(0, 0, 0, 8);
            secondRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            secondRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleStack = new StackPanel { Margin = new Thickness(0, 0, 12, 0) };
            var titleLabel = new TextBlock
            {
                Text = "Название заказа *",
                Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            var titleTextBox = new TextBox
            {
                Style = (Style)Application.Current.FindResource("ModernTextBox"),
                Tag = "Введите название заказа"
            };
            SetBinding(titleTextBox, TextBox.TextProperty, "CurrentOrder.Title", dataContext, BindingMode.TwoWay, UpdateSourceTrigger.PropertyChanged);
            titleStack.Children.Add(titleLabel);
            titleStack.Children.Add(titleTextBox);
            Grid.SetColumn(titleStack, 0);
            secondRow.Children.Add(titleStack);

            var dueDateStack = new StackPanel { Width = 200 };
            var dueDateLabel = new TextBlock
            {
                Text = "Срок выполнения",
                Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            var dueDatePicker = new DatePicker
            {
                Style = (Style)Application.Current.FindResource("ModernDatePicker")
            };
            SetBinding(dueDatePicker, DatePicker.SelectedDateProperty, "CurrentOrder.DueDate", dataContext, BindingMode.TwoWay);
            dueDateStack.Children.Add(dueDateLabel);
            dueDateStack.Children.Add(dueDatePicker);
            Grid.SetColumn(dueDateStack, 1);
            secondRow.Children.Add(dueDateStack);

            Grid.SetRow(secondRow, 1);
            formGrid.Children.Add(secondRow);

            // Третья строка формы
            var thirdRow = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
            var descriptionLabel = new TextBlock
            {
                Text = "Описание",
                Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            var descriptionTextBox = new TextBox
            {
                Style = (Style)Application.Current.FindResource("ModernTextBox"),
                Tag = "Введите описание заказа",
                Height = 60,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            SetBinding(descriptionTextBox, TextBox.TextProperty, "CurrentOrder.Description", dataContext, BindingMode.TwoWay, UpdateSourceTrigger.PropertyChanged);
            thirdRow.Children.Add(descriptionLabel);
            thirdRow.Children.Add(descriptionTextBox);
            Grid.SetRow(thirdRow, 2);
            formGrid.Children.Add(thirdRow);

            // Четвертая строка - кнопки
            var buttonsRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var saveButton = new Button
            {
                Content = "💾 Сохранить",
                Style = (Style)Application.Current.FindResource("ModernButton"),
                Background = (Brush)Application.Current.FindResource("SuccessBrush"),
                Margin = new Thickness(0, 0, 8, 0),
                MinWidth = 100,
                Padding = new Thickness(12, 8, 12, 8)
            };
            SetBinding(saveButton, Button.CommandProperty, "SaveOrderCommand", dataContext);

            var cancelButton = new Button
            {
                Content = "❌ Отмена",
                Style = (Style)Application.Current.FindResource("ModernButton"),
                Background = (Brush)Application.Current.FindResource("ErrorBrush"),
                MinWidth = 80,
                Padding = new Thickness(12, 8, 12, 8)
            };
            SetBinding(cancelButton, Button.CommandProperty, "CancelOrderCommand", dataContext);

            buttonsRow.Children.Add(saveButton);
            buttonsRow.Children.Add(cancelButton);
            Grid.SetRow(buttonsRow, 3);
            formGrid.Children.Add(buttonsRow);

            formBorder.Child = formGrid;
            Grid.SetRow(formBorder, 2);
            grid.Children.Add(formBorder);

            // Таблица заказов
            var dataGrid = new DataGrid
            {
                Style = (Style)Application.Current.FindResource("ModernDataGrid"),
                ColumnHeaderStyle = (Style)Application.Current.FindResource("ModernDataGridColumnHeader"),
                RowStyle = (Style)Application.Current.FindResource("ModernDataGridRow"),
                CellStyle = (Style)Application.Current.FindResource("ModernDataGridCell"),
                AutoGenerateColumns = false,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            SetBinding(dataGrid, DataGrid.ItemsSourceProperty, "FilteredOrders", dataContext);

            // Колонки таблицы
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "№", Binding = new Binding("OrderNumber"), Width = 60 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Название", Binding = new Binding("Title"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 150 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Клиент", Binding = new Binding("Client.CompanyName"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 120 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Категория", Binding = new Binding("Category.Name"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 100 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Приоритет", Binding = new Binding("Priority.Name"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 100 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Статус", Binding = new Binding("Status.Name"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 100 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Срок выполнения", Binding = new Binding("DueDate") { StringFormat = "dd.MM.yyyy" }, Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 110 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Дата создания", Binding = new Binding("CreatedDate") { StringFormat = "dd.MM.yyyy HH:mm" }, Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 120 });

            // Колонка действий
            var actionsColumn = new DataGridTemplateColumn
            {
                Header = "Действия",
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                MinWidth = 100
            };

            var actionsTemplate = new DataTemplate();
            var actionsFactory = new FrameworkElementFactory(typeof(StackPanel));
            actionsFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            actionsFactory.SetValue(StackPanel.HorizontalAlignmentProperty, HorizontalAlignment.Center);

            // Кнопка редактирования
            var editButtonFactory = new FrameworkElementFactory(typeof(Button));
            editButtonFactory.SetValue(Button.ContentProperty, "✏️");
            editButtonFactory.SetValue(Button.StyleProperty, Application.Current.FindResource("TextButton"));
            editButtonFactory.SetValue(Button.ToolTipProperty, "Редактировать");
            editButtonFactory.SetValue(Button.PaddingProperty, new Thickness(6, 3, 6, 3));

            var editCommandBinding = new Binding("DataContext.EditOrderCommand");
            editCommandBinding.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGrid), 1);
            editButtonFactory.SetBinding(Button.CommandProperty, editCommandBinding);

            var editParameterBinding = new Binding("Id");
            editButtonFactory.SetBinding(Button.CommandParameterProperty, editParameterBinding);

            // Кнопка удаления
            var deleteButtonFactory = new FrameworkElementFactory(typeof(Button));
            deleteButtonFactory.SetValue(Button.ContentProperty, "🗑️");
            deleteButtonFactory.SetValue(Button.StyleProperty, Application.Current.FindResource("TextButton"));
            deleteButtonFactory.SetValue(Button.ToolTipProperty, "Удалить");
            deleteButtonFactory.SetValue(Button.PaddingProperty, new Thickness(6, 3, 6, 3));
            deleteButtonFactory.SetValue(Button.MarginProperty, new Thickness(4, 0, 0, 0));
            deleteButtonFactory.SetValue(Button.ForegroundProperty, Application.Current.FindResource("ErrorBrush"));

            var deleteCommandBinding = new Binding("DataContext.DeleteOrderCommand");
            deleteCommandBinding.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGrid), 1);
            deleteButtonFactory.SetBinding(Button.CommandProperty, deleteCommandBinding);

            var deleteParameterBinding = new Binding("Id");
            deleteButtonFactory.SetBinding(Button.CommandParameterProperty, deleteParameterBinding);

            actionsFactory.AppendChild(editButtonFactory);
            actionsFactory.AppendChild(deleteButtonFactory);
            actionsTemplate.VisualTree = actionsFactory;
            actionsColumn.CellTemplate = actionsTemplate;

            dataGrid.Columns.Add(actionsColumn);

            Grid.SetRow(dataGrid, 3);
            grid.Children.Add(dataGrid);

            return grid;
        }

        // Создание точной копии содержимого для вкладки "Планы работ"
        public static UIElement CreateExactWorkPlansContent(object dataContext)
        {
            var grid = new Grid
            {
                Margin = new Thickness(8),
                UseLayoutRounding = true,
                DataContext = dataContext
            };

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Адаптивная панель управления планами работ
            var controlPanelGrid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            controlPanelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            controlPanelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            controlPanelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleText = new TextBlock
            {
                Text = "Управление планами работ",
                Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 16, 0)
            };
            Grid.SetColumn(titleText, 0);
            controlPanelGrid.Children.Add(titleText);

            var buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(buttonsPanel, 2);

            var addButton = new Button
            {
                Content = "➕ Создать план",
                Style = (Style)Application.Current.FindResource("ModernButton"),
                Background = (Brush)Application.Current.FindResource("SuccessBrush"),
                Margin = new Thickness(0, 0, 8, 0),
                MinWidth = 120,
                Padding = new Thickness(12, 8, 12, 8)
            };
            SetBinding(addButton, Button.CommandProperty, "AddWorkPlanCommand", dataContext);

            var refreshButton = new Button
            {
                Content = "🔄 Обновить",
                Style = (Style)Application.Current.FindResource("ModernButton"),
                Background = (Brush)Application.Current.FindResource("WarningBrush"),
                MinWidth = 100,
                Padding = new Thickness(12, 8, 12, 8)
            };
            SetBinding(refreshButton, Button.CommandProperty, "RefreshWorkPlansCommand", dataContext);

            buttonsPanel.Children.Add(addButton);
            buttonsPanel.Children.Add(refreshButton);
            controlPanelGrid.Children.Add(buttonsPanel);

            Grid.SetRow(controlPanelGrid, 0);
            grid.Children.Add(controlPanelGrid);

            // Поиск планов работ
            var searchBorder = new Border
            {
                Background = (Brush)Application.Current.FindResource("SecondaryBrush"),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var searchGrid = new Grid();
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var searchIcon = new TextBlock
            {
                Text = "🔍",
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 8, 0)
            };
            Grid.SetColumn(searchIcon, 0);
            searchGrid.Children.Add(searchIcon);

            var searchBox = new TextBox
            {
                Style = (Style)Application.Current.FindResource("ModernTextBox"),
                Tag = "Поиск по названию или плану действий...",
                MinWidth = 200,
                Margin = new Thickness(0, 0, 8, 0)
            };
            SetBinding(searchBox, TextBox.TextProperty, "WorkPlanSearchText", dataContext, BindingMode.TwoWay, UpdateSourceTrigger.PropertyChanged);
            Grid.SetColumn(searchBox, 1);
            searchGrid.Children.Add(searchBox);

            var searchResultText = new TextBlock
            {
                Foreground = (Brush)Application.Current.FindResource("TextSecondaryBrush"),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            };
            SetBinding(searchResultText, TextBlock.TextProperty, "FilteredWorkPlans.Count", dataContext, BindingMode.OneWay, UpdateSourceTrigger.PropertyChanged,
                new StringFormatConverter() { StringFormat = "Найдено: {0}" });
            Grid.SetColumn(searchResultText, 2);
            searchGrid.Children.Add(searchResultText);

            searchBorder.Child = searchGrid;
            Grid.SetRow(searchBorder, 1);
            grid.Children.Add(searchBorder);

            // Форма плана работ
            var formBorder = new Border
            {
                Background = (Brush)Application.Current.FindResource("SecondaryBrush"),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 12),
                BorderThickness = new Thickness(1),
                BorderBrush = (Brush)Application.Current.FindResource("BorderBrush")
            };
            SetBinding(formBorder, Border.VisibilityProperty, "IsWorkPlanFormVisible", dataContext, converter: new BooleanToVisibilityConverter());

            var formGrid = new Grid();
            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Первая строка формы
            var firstRow = new Grid();
            firstRow.Margin = new Thickness(0, 0, 0, 8);
            firstRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            firstRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var descriptionStack = new StackPanel { Margin = new Thickness(0, 0, 12, 0) };
            var descriptionLabel = new TextBlock
            {
                Text = "Описание плана *",
                Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            var descriptionTextBox = new TextBox
            {
                Style = (Style)Application.Current.FindResource("ModernTextBox"),
                Tag = "Введите описание плана работ"
            };
            SetBinding(descriptionTextBox, TextBox.TextProperty, "CurrentWorkPlan.PlanDescription", dataContext, BindingMode.TwoWay, UpdateSourceTrigger.PropertyChanged);
            descriptionStack.Children.Add(descriptionLabel);
            descriptionStack.Children.Add(descriptionTextBox);
            Grid.SetColumn(descriptionStack, 0);
            firstRow.Children.Add(descriptionStack);

            var hoursStack = new StackPanel { Width = 120 };
            var hoursLabel = new TextBlock
            {
                Text = "Часы *",
                Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            var hoursTextBox = new TextBox
            {
                Style = (Style)Application.Current.FindResource("ModernTextBox"),
                Tag = "Часы"
            };
            SetBinding(hoursTextBox, TextBox.TextProperty, "CurrentWorkPlan.EstimatedHours", dataContext, BindingMode.TwoWay, UpdateSourceTrigger.PropertyChanged);
            hoursStack.Children.Add(hoursLabel);
            hoursStack.Children.Add(hoursTextBox);
            Grid.SetColumn(hoursStack, 1);
            firstRow.Children.Add(hoursStack);

            Grid.SetRow(firstRow, 0);
            formGrid.Children.Add(firstRow);

            // Вторая строка формы
            var secondRow = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
            var stepsLabel = new TextBlock
            {
                Text = "Шаги тестирования",
                Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            var stepsTextBox = new TextBox
            {
                Style = (Style)Application.Current.FindResource("ModernTextBox"),
                Tag = "Опишите шаги тестирования...",
                Height = 80,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            SetBinding(stepsTextBox, TextBox.TextProperty, "CurrentWorkPlan.TestSteps", dataContext, BindingMode.TwoWay, UpdateSourceTrigger.PropertyChanged);
            secondRow.Children.Add(stepsLabel);
            secondRow.Children.Add(stepsTextBox);
            Grid.SetRow(secondRow, 1);
            formGrid.Children.Add(secondRow);

            // Третья строка формы
            var thirdRow = new UniformGrid { Columns = 3, Margin = new Thickness(0, 0, 0, 8) };

            var typeStack = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
            var typeLabel = new TextBlock
            {
                Text = "Тип плана",
                Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            var typeComboBox = new ComboBox
            {
                Style = (Style)Application.Current.FindResource("ModernComboBox"),
                DisplayMemberPath = "Name",
                SelectedValuePath = "Id"
            };
            SetBinding(typeComboBox, ComboBox.ItemsSourceProperty, "AllWorkPlanTypes", dataContext);
            SetBinding(typeComboBox, ComboBox.SelectedValueProperty, "CurrentWorkPlan.TypeId", dataContext, BindingMode.TwoWay);
            typeStack.Children.Add(typeLabel);
            typeStack.Children.Add(typeComboBox);

            var complexityStack = new StackPanel { Margin = new Thickness(4, 0, 4, 0) };
            var complexityLabel = new TextBlock
            {
                Text = "Сложность",
                Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            var complexityComboBox = new ComboBox
            {
                Style = (Style)Application.Current.FindResource("ModernComboBox"),
                DisplayMemberPath = "Name",
                SelectedValuePath = "Id"
            };
            SetBinding(complexityComboBox, ComboBox.ItemsSourceProperty, "AllComplexities", dataContext);
            SetBinding(complexityComboBox, ComboBox.SelectedValueProperty, "CurrentWorkPlan.ComplexityId", dataContext, BindingMode.TwoWay);
            complexityStack.Children.Add(complexityLabel);
            complexityStack.Children.Add(complexityComboBox);

            var statusStack = new StackPanel { Margin = new Thickness(8, 0, 0, 0) };
            var statusLabel = new TextBlock
            {
                Text = "Статус",
                Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            var statusComboBox = new ComboBox
            {
                Style = (Style)Application.Current.FindResource("ModernComboBox"),
                DisplayMemberPath = "Name",
                SelectedValuePath = "Id"
            };
            SetBinding(statusComboBox, ComboBox.ItemsSourceProperty, "AllWorkPlanStatuses", dataContext);
            SetBinding(statusComboBox, ComboBox.SelectedValueProperty, "CurrentWorkPlan.StatusId", dataContext, BindingMode.TwoWay);
            statusStack.Children.Add(statusLabel);
            statusStack.Children.Add(statusComboBox);

            thirdRow.Children.Add(typeStack);
            thirdRow.Children.Add(complexityStack);
            thirdRow.Children.Add(statusStack);
            Grid.SetRow(thirdRow, 2);
            formGrid.Children.Add(thirdRow);

            // Четвертая строка - кнопки
            var buttonsRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var saveButton = new Button
            {
                Content = "💾 Сохранить",
                Style = (Style)Application.Current.FindResource("ModernButton"),
                Background = (Brush)Application.Current.FindResource("SuccessBrush"),
                Margin = new Thickness(0, 0, 8, 0),
                MinWidth = 100,
                Padding = new Thickness(12, 8, 12, 8)
            };
            SetBinding(saveButton, Button.CommandProperty, "SaveWorkPlanCommand", dataContext);

            var cancelButton = new Button
            {
                Content = "❌ Отмена",
                Style = (Style)Application.Current.FindResource("ModernButton"),
                Background = (Brush)Application.Current.FindResource("ErrorBrush"),
                MinWidth = 80,
                Padding = new Thickness(12, 8, 12, 8)
            };
            SetBinding(cancelButton, Button.CommandProperty, "CancelWorkPlanCommand", dataContext);

            buttonsRow.Children.Add(saveButton);
            buttonsRow.Children.Add(cancelButton);
            Grid.SetRow(buttonsRow, 3);
            formGrid.Children.Add(buttonsRow);

            formBorder.Child = formGrid;
            Grid.SetRow(formBorder, 2);
            grid.Children.Add(formBorder);

            // Таблица планов работ
            var dataGrid = new DataGrid
            {
                Style = (Style)Application.Current.FindResource("ModernDataGrid"),
                ColumnHeaderStyle = (Style)Application.Current.FindResource("ModernDataGridColumnHeader"),
                RowStyle = (Style)Application.Current.FindResource("ModernDataGridRow"),
                CellStyle = (Style)Application.Current.FindResource("ModernDataGridCell"),
                AutoGenerateColumns = false,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            SetBinding(dataGrid, DataGrid.ItemsSourceProperty, "FilteredWorkPlans", dataContext);

            // Колонки таблицы
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "№", Binding = new Binding("OrderNumber"), Width = 60 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Описание", Binding = new Binding("PlanDescription"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 200 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Шаги плана", Binding = new Binding("TestSteps"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 150 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Часы", Binding = new Binding("EstimatedHours"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 80 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Дата создания", Binding = new Binding("CreatedDate") { StringFormat = "dd.MM.yyyy HH:mm" }, Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 120 });

            // Колонка действий
            var actionsColumn = new DataGridTemplateColumn
            {
                Header = "Действия",
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                MinWidth = 100
            };

            var actionsTemplate = new DataTemplate();
            var actionsFactory = new FrameworkElementFactory(typeof(StackPanel));
            actionsFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            actionsFactory.SetValue(StackPanel.HorizontalAlignmentProperty, HorizontalAlignment.Center);

            // Кнопка редактирования
            var editButtonFactory = new FrameworkElementFactory(typeof(Button));
            editButtonFactory.SetValue(Button.ContentProperty, "✏️");
            editButtonFactory.SetValue(Button.StyleProperty, Application.Current.FindResource("TextButton"));
            editButtonFactory.SetValue(Button.ToolTipProperty, "Редактировать");
            editButtonFactory.SetValue(Button.PaddingProperty, new Thickness(6, 3, 6, 3));

            var editCommandBinding = new Binding("DataContext.EditWorkPlanCommand");
            editCommandBinding.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGrid), 1);
            editButtonFactory.SetBinding(Button.CommandProperty, editCommandBinding);

            var editParameterBinding = new Binding("Id");
            editButtonFactory.SetBinding(Button.CommandParameterProperty, editParameterBinding);

            // Кнопка удаления
            var deleteButtonFactory = new FrameworkElementFactory(typeof(Button));
            deleteButtonFactory.SetValue(Button.ContentProperty, "🗑️");
            deleteButtonFactory.SetValue(Button.StyleProperty, Application.Current.FindResource("TextButton"));
            deleteButtonFactory.SetValue(Button.ToolTipProperty, "Удалить");
            deleteButtonFactory.SetValue(Button.PaddingProperty, new Thickness(6, 3, 6, 3));
            deleteButtonFactory.SetValue(Button.MarginProperty, new Thickness(4, 0, 0, 0));
            deleteButtonFactory.SetValue(Button.ForegroundProperty, Application.Current.FindResource("ErrorBrush"));

            var deleteCommandBinding = new Binding("DataContext.DeleteWorkPlanCommand");
            deleteCommandBinding.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGrid), 1);
            deleteButtonFactory.SetBinding(Button.CommandProperty, deleteCommandBinding);

            var deleteParameterBinding = new Binding("Id");
            deleteButtonFactory.SetBinding(Button.CommandParameterProperty, deleteParameterBinding);

            actionsFactory.AppendChild(editButtonFactory);
            actionsFactory.AppendChild(deleteButtonFactory);
            actionsTemplate.VisualTree = actionsFactory;
            actionsColumn.CellTemplate = actionsTemplate;

            dataGrid.Columns.Add(actionsColumn);

            Grid.SetRow(dataGrid, 3);
            grid.Children.Add(dataGrid);

            return grid;
        }

        // Вспомогательный метод для установки привязок
        private static void SetBinding(DependencyObject target, DependencyProperty property, string path, object source,
            BindingMode mode = BindingMode.OneWay, UpdateSourceTrigger trigger = UpdateSourceTrigger.PropertyChanged,
            IValueConverter converter = null)
        {
            var binding = new Binding(path)
            {
                Source = source,
                Mode = mode,
                UpdateSourceTrigger = trigger
            };

            if (converter != null)
            {
                binding.Converter = converter;
            }

            BindingOperations.SetBinding(target, property, binding);
        }

        // Создание копии заголовка
        private static object CreateHeaderCopy(object originalHeader)
        {
            if (originalHeader is string headerText)
            {
                return headerText;
            }
            return originalHeader;
        }

        #endregion

        #region Helper Methods

        private static TabItem FindTabItem(TabControl tabControl, Point point)
        {
            for (int i = 0; i < tabControl.Items.Count; i++)
            {
                var tabItem = tabControl.ItemContainerGenerator.ContainerFromIndex(i) as TabItem;
                if (tabItem != null)
                {
                    var tabItemBounds = new Rect(tabItem.TranslatePoint(new Point(0, 0), tabControl),
                                               new Size(tabItem.ActualWidth, tabItem.ActualHeight));
                    if (tabItemBounds.Contains(point))
                    {
                        return tabItem;
                    }
                }
            }
            return null;
        }

        private static void ResetDragState()
        {
            _draggedTabItem = null;
            _sourceTabControl = null;
            _isDragStarted = false;
        }

        #endregion
    }

    // Конвертер для форматирования строк
    public class StringFormatConverter : IValueConverter
    {
        public string StringFormat { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return null;

            if (!string.IsNullOrEmpty(StringFormat))
                return string.Format(culture, StringFormat, value);

            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Вспомогательный класс для создания глубоких копий через XAML
    public static class XamlCopyHelper
    {
        public static UIElement CreateDeepCopy(UIElement original)
        {
            try
            {
                // Сериализуем элемент в XAML
                string xaml = XamlWriter.Save(original);

                // Десериализуем обратно для создания копии
                StringReader stringReader = new StringReader(xaml);
                System.Xml.XmlReader xmlReader = System.Xml.XmlReader.Create(stringReader);
                return (UIElement)XamlReader.Load(xmlReader);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при создании XAML копии: {ex.Message}");
                return null;
            }
        }
    }

    // Класс для отдельного окна с вкладкой
    // Класс для отдельного окна с вкладкой
    public class DetachedTabWindow : Window
    {
        private readonly object _content;
        private readonly object _header;
        private readonly Style _style;
        private readonly TabControl _originalTabControl;
        private readonly object _dataContext;
        private bool _isReturned = false;

        public DetachedTabWindow(object content, object header, Style style, TabControl originalTabControl, object dataContext)
        {
            _content = content;
            _header = header;
            _style = style;
            _originalTabControl = originalTabControl;
            _dataContext = dataContext;

            InitializeWindow();

            // Гарантируем, что окно будет активировано
            this.Activated += (s, e) => { };
            this.Focusable = true;
        }



        private void InitializeWindow()
        {
            Background = (Brush)Application.Current.FindResource("BackgroundBrush");

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Панель управления
            var controlPanel = CreateControlPanel();
            mainGrid.Children.Add(controlPanel);
            Grid.SetRow(controlPanel, 0);

            // Содержимое вкладки
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(10)
            };

            if (_content is UIElement content)
            {
                scrollViewer.Content = content;
            }
            else
            {
                scrollViewer.Content = new TextBlock
                {
                    Text = "Содержимое вкладки",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }

            mainGrid.Children.Add(scrollViewer);
            Grid.SetRow(scrollViewer, 1);

            Content = mainGrid;
            DataContext = _dataContext;

            // Отключаем перетаскивание для этого окна
            AllowDrop = false;
        }

        private UIElement CreateControlPanel()
        {
            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Background = (Brush)Application.Current.FindResource("PrimaryBrush"),
                Margin = new Thickness(0, 0, 0, 1)
            };

            // Кнопка возврата
            var returnButton = new Button
            {
                Content = "⬅ Вернуть в главное окно",
                Margin = new Thickness(10, 8, 5, 8),
                Padding = new Thickness(15, 8, 15, 8),
                Background = (Brush)Application.Current.FindResource("AccentBrush"),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Cursor = Cursors.Hand,
                ToolTip = "Вернуть эту вкладку обратно в главное окно"
            };

            returnButton.Click += (s, e) => ReturnToMainWindow();

            // Кнопка закрытия
            var closeButton = new Button
            {
                Content = "✕ Закрыть окно",
                Margin = new Thickness(5, 8, 10, 8),
                Padding = new Thickness(15, 8, 15, 8),
                Background = (Brush)Application.Current.FindResource("ErrorBrush"),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Cursor = Cursors.Hand,
                ToolTip = "Закрыть это окно (вкладка вернется в главное окно)"
            };

            closeButton.Click += (s, e) => Close();

            stackPanel.Children.Add(returnButton);
            stackPanel.Children.Add(closeButton);

            return stackPanel;
        }

        private void ReturnToMainWindow()
        {
            if (_isReturned) return;

            try
            {
                _isReturned = true;

                // Создаем новое содержимое для возврата на основе заголовка
                var contentCopy = CreateContentFromHeader(_header, _dataContext);

                var newTabItem = new TabItem
                {
                    Content = contentCopy,
                    Header = _header,
                    Style = _style
                };

                // Добавляем вкладку обратно в оригинальный TabControl
                _originalTabControl.Items.Add(newTabItem);
                newTabItem.IsSelected = true;

                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при возврате вкладки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _isReturned = false;
            }
        }

        // Метод для создания содержимого на основе заголовка вкладки
        private static UIElement CreateContentFromHeader(object header, object dataContext)
        {
            var headerText = header?.ToString() ?? "";

            try
            {
                if (headerText.Contains("👥 Клиенты"))
                {
                    return CreateFullClientsContent(dataContext);
                }
                else if (headerText.Contains("📦 Заказы"))
                {
                    return CreateFullOrdersContent(dataContext);
                }
                else if (headerText.Contains("📋 Планы работ"))
                {
                    return CreateFullWorkPlansContent(dataContext);
                }
                else
                {
                    return CreateFallbackContent(headerText);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при создании содержимого: {ex.Message}");
                return CreateFallbackContent(headerText);
            }
        }

        // Полное содержимое для вкладки "Клиенты"
        private static UIElement CreateFullClientsContent(object dataContext)
        {
            var grid = new Grid
            {
                Margin = new Thickness(8),
                UseLayoutRounding = true,
                DataContext = dataContext
            };

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // 1. Панель управления
            var controlGrid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            controlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            controlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            controlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleText = new TextBlock
            {
                Text = "Управление клиентами",
                Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 16, 0)
            };
            Grid.SetColumn(titleText, 0);
            controlGrid.Children.Add(titleText);

            var buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(buttonsPanel, 2);

            var addButton = new Button
            {
                Content = "➕ Добавить клиента",
                Style = (Style)Application.Current.FindResource("ModernButton"),
                Background = (Brush)Application.Current.FindResource("SuccessBrush"),
                Margin = new Thickness(0, 0, 8, 0),
                MinWidth = 120,
                Padding = new Thickness(12, 8, 12, 8)
            };
            SetBinding(addButton, Button.CommandProperty, "AddClientCommand", dataContext);

            var refreshButton = new Button
            {
                Content = "🔄 Обновить",
                Style = (Style)Application.Current.FindResource("ModernButton"),
                Background = (Brush)Application.Current.FindResource("WarningBrush"),
                MinWidth = 100,
                Padding = new Thickness(12, 8, 12, 8)
            };
            SetBinding(refreshButton, Button.CommandProperty, "RefreshClientsCommand", dataContext);

            buttonsPanel.Children.Add(addButton);
            buttonsPanel.Children.Add(refreshButton);
            controlGrid.Children.Add(buttonsPanel);

            Grid.SetRow(controlGrid, 0);
            grid.Children.Add(controlGrid);

            // 2. Поиск
            var searchBorder = new Border
            {
                Background = (Brush)Application.Current.FindResource("SecondaryBrush"),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var searchGrid = new Grid();
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var searchIcon = new TextBlock
            {
                Text = "🔍",
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 8, 0)
            };
            Grid.SetColumn(searchIcon, 0);
            searchGrid.Children.Add(searchIcon);

            var searchBox = new TextBox
            {
                Style = (Style)Application.Current.FindResource("ModernTextBox"),
                Tag = "Поиск по компании, контактному лицу, email или телефону...",
                MinWidth = 200,
                Margin = new Thickness(0, 0, 8, 0)
            };
            SetBinding(searchBox, TextBox.TextProperty, "ClientSearchText", dataContext, BindingMode.TwoWay, UpdateSourceTrigger.PropertyChanged);
            Grid.SetColumn(searchBox, 1);
            searchGrid.Children.Add(searchBox);

            var searchResultText = new TextBlock
            {
                Foreground = (Brush)Application.Current.FindResource("TextSecondaryBrush"),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            };
            SetBinding(searchResultText, TextBlock.TextProperty, "FilteredClients.Count", dataContext, BindingMode.OneWay, UpdateSourceTrigger.PropertyChanged,
                new StringFormatConverter() { StringFormat = "Найдено: {0}" });
            Grid.SetColumn(searchResultText, 2);
            searchGrid.Children.Add(searchResultText);

            searchBorder.Child = searchGrid;
            Grid.SetRow(searchBorder, 1);
            grid.Children.Add(searchBorder);

            // 3. Форма клиента
            var formBorder = new Border
            {
                Background = (Brush)Application.Current.FindResource("SecondaryBrush"),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 12),
                BorderThickness = new Thickness(1),
                BorderBrush = (Brush)Application.Current.FindResource("BorderBrush")
            };
            SetBinding(formBorder, Border.VisibilityProperty, "IsClientFormVisible", dataContext, converter: new BooleanToVisibilityConverter());

            var formGrid = new Grid();
            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Первая строка формы
            var firstRow = new UniformGrid { Columns = 2, Margin = new Thickness(0, 0, 0, 8) };

            var companyStack = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
            var companyLabel = new TextBlock
            {
                Text = "Компания *",
                Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            var companyTextBox = new TextBox
            {
                Style = (Style)Application.Current.FindResource("ModernTextBox"),
                Tag = "Введите название компании"
            };
            SetBinding(companyTextBox, TextBox.TextProperty, "CurrentClient.CompanyName", dataContext, BindingMode.TwoWay, UpdateSourceTrigger.PropertyChanged);
            companyStack.Children.Add(companyLabel);
            companyStack.Children.Add(companyTextBox);

            var contactStack = new StackPanel { Margin = new Thickness(8, 0, 0, 0) };
            var contactLabel = new TextBlock
            {
                Text = "Контактное лицо *",
                Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            var contactTextBox = new TextBox
            {
                Style = (Style)Application.Current.FindResource("ModernTextBox"),
                Tag = "Введите ФИО контактного лица"
            };
            SetBinding(contactTextBox, TextBox.TextProperty, "CurrentClient.ContactPerson", dataContext, BindingMode.TwoWay, UpdateSourceTrigger.PropertyChanged);
            contactStack.Children.Add(contactLabel);
            contactStack.Children.Add(contactTextBox);

            firstRow.Children.Add(companyStack);
            firstRow.Children.Add(contactStack);
            Grid.SetRow(firstRow, 0);
            formGrid.Children.Add(firstRow);

            // Вторая строка формы
            var secondRow = new UniformGrid { Columns = 3, Margin = new Thickness(0, 0, 0, 8) };

            var emailStack = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
            var emailLabel = new TextBlock
            {
                Text = "Email",
                Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            var emailTextBox = new TextBox
            {
                Style = (Style)Application.Current.FindResource("ModernTextBox"),
                Tag = "Введите email"
            };
            SetBinding(emailTextBox, TextBox.TextProperty, "CurrentClient.Email", dataContext, BindingMode.TwoWay, UpdateSourceTrigger.PropertyChanged);
            emailStack.Children.Add(emailLabel);
            emailStack.Children.Add(emailTextBox);

            var phoneStack = new StackPanel { Margin = new Thickness(4, 0, 4, 0) };
            var phoneLabel = new TextBlock
            {
                Text = "Телефон",
                Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            var phoneTextBox = new TextBox
            {
                Style = (Style)Application.Current.FindResource("ModernTextBox"),
                Tag = "Введите телефон"
            };
            SetBinding(phoneTextBox, TextBox.TextProperty, "CurrentClient.Phone", dataContext, BindingMode.TwoWay, UpdateSourceTrigger.PropertyChanged);
            phoneStack.Children.Add(phoneLabel);
            phoneStack.Children.Add(phoneTextBox);

            var addressStack = new StackPanel { Margin = new Thickness(8, 0, 0, 0) };
            var addressLabel = new TextBlock
            {
                Text = "Адрес",
                Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            var addressTextBox = new TextBox
            {
                Style = (Style)Application.Current.FindResource("ModernTextBox"),
                Tag = "Введите адрес"
            };
            SetBinding(addressTextBox, TextBox.TextProperty, "CurrentClient.Address", dataContext, BindingMode.TwoWay, UpdateSourceTrigger.PropertyChanged);
            addressStack.Children.Add(addressLabel);
            addressStack.Children.Add(addressTextBox);

            secondRow.Children.Add(emailStack);
            secondRow.Children.Add(phoneStack);
            secondRow.Children.Add(addressStack);
            Grid.SetRow(secondRow, 1);
            formGrid.Children.Add(secondRow);

            // Третья строка - кнопки
            var buttonsRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var saveButton = new Button
            {
                Content = "💾 Сохранить",
                Style = (Style)Application.Current.FindResource("ModernButton"),
                Background = (Brush)Application.Current.FindResource("SuccessBrush"),
                Margin = new Thickness(0, 0, 8, 0),
                MinWidth = 100,
                Padding = new Thickness(12, 8, 12, 8)
            };
            SetBinding(saveButton, Button.CommandProperty, "SaveClientCommand", dataContext);

            var cancelButton = new Button
            {
                Content = "❌ Отмена",
                Style = (Style)Application.Current.FindResource("ModernButton"),
                Background = (Brush)Application.Current.FindResource("ErrorBrush"),
                MinWidth = 80,
                Padding = new Thickness(12, 8, 12, 8)
            };
            SetBinding(cancelButton, Button.CommandProperty, "CancelClientCommand", dataContext);

            buttonsRow.Children.Add(saveButton);
            buttonsRow.Children.Add(cancelButton);
            Grid.SetRow(buttonsRow, 2);
            formGrid.Children.Add(buttonsRow);

            formBorder.Child = formGrid;
            Grid.SetRow(formBorder, 2);
            grid.Children.Add(formBorder);

            // 4. Таблица клиентов
            var dataGrid = new DataGrid
            {
                Style = (Style)Application.Current.FindResource("ModernDataGrid"),
                ColumnHeaderStyle = (Style)Application.Current.FindResource("ModernDataGridColumnHeader"),
                RowStyle = (Style)Application.Current.FindResource("ModernDataGridRow"),
                CellStyle = (Style)Application.Current.FindResource("ModernDataGridCell"),
                AutoGenerateColumns = false,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            SetBinding(dataGrid, DataGrid.ItemsSourceProperty, "FilteredClients", dataContext);

            // Колонки таблицы
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "№", Binding = new Binding("OrderNumber"), Width = 60 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Компания", Binding = new Binding("CompanyName"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 150 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Контактное лицо", Binding = new Binding("ContactPerson"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 120 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Email", Binding = new Binding("Email"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 150 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Телефон", Binding = new Binding("Phone"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 100 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Адрес", Binding = new Binding("Address"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 120 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Дата регистрации", Binding = new Binding("CreatedDate") { StringFormat = "dd.MM.yyyy HH:mm" }, Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 120 });

            // Колонка действий
            var actionsColumn = new DataGridTemplateColumn
            {
                Header = "Действия",
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                MinWidth = 100
            };

            var actionsTemplate = new DataTemplate();
            var actionsFactory = new FrameworkElementFactory(typeof(StackPanel));
            actionsFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            actionsFactory.SetValue(StackPanel.HorizontalAlignmentProperty, HorizontalAlignment.Center);

            // Кнопка редактирования
            var editButtonFactory = new FrameworkElementFactory(typeof(Button));
            editButtonFactory.SetValue(Button.ContentProperty, "✏️");
            editButtonFactory.SetValue(Button.StyleProperty, Application.Current.FindResource("TextButton"));
            editButtonFactory.SetValue(Button.ToolTipProperty, "Редактировать");
            editButtonFactory.SetValue(Button.PaddingProperty, new Thickness(6, 3, 6, 3));

            var editCommandBinding = new Binding("DataContext.EditClientCommand");
            editCommandBinding.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGrid), 1);
            editButtonFactory.SetBinding(Button.CommandProperty, editCommandBinding);

            var editParameterBinding = new Binding("Id");
            editButtonFactory.SetBinding(Button.CommandParameterProperty, editParameterBinding);

            // Кнопка удаления
            var deleteButtonFactory = new FrameworkElementFactory(typeof(Button));
            deleteButtonFactory.SetValue(Button.ContentProperty, "🗑️");
            deleteButtonFactory.SetValue(Button.StyleProperty, Application.Current.FindResource("TextButton"));
            deleteButtonFactory.SetValue(Button.ToolTipProperty, "Удалить");
            deleteButtonFactory.SetValue(Button.PaddingProperty, new Thickness(6, 3, 6, 3));
            deleteButtonFactory.SetValue(Button.MarginProperty, new Thickness(4, 0, 0, 0));
            deleteButtonFactory.SetValue(Button.ForegroundProperty, Application.Current.FindResource("ErrorBrush"));

            var deleteCommandBinding = new Binding("DataContext.DeleteClientCommand");
            deleteCommandBinding.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGrid), 1);
            deleteButtonFactory.SetBinding(Button.CommandProperty, deleteCommandBinding);

            var deleteParameterBinding = new Binding("Id");
            deleteButtonFactory.SetBinding(Button.CommandParameterProperty, deleteParameterBinding);

            actionsFactory.AppendChild(editButtonFactory);
            actionsFactory.AppendChild(deleteButtonFactory);
            actionsTemplate.VisualTree = actionsFactory;
            actionsColumn.CellTemplate = actionsTemplate;

            dataGrid.Columns.Add(actionsColumn);

            Grid.SetRow(dataGrid, 3);
            grid.Children.Add(dataGrid);

            return grid;
        }

        // Полное содержимое для вкладки "Заказы" с формой редактирования
        private static UIElement CreateFullOrdersContent(object dataContext)
        {
            var grid = new Grid
            {
                Margin = new Thickness(8),
                UseLayoutRounding = true,
                DataContext = dataContext
            };

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // 1. Панель управления
            var controlGrid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            controlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            controlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            controlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleText = new TextBlock
            {
                Text = "Управление заказами",
                Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 16, 0)
            };
            Grid.SetColumn(titleText, 0);
            controlGrid.Children.Add(titleText);

            var buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(buttonsPanel, 2);

            var addButton = new Button
            {
                Content = "➕ Создать заказ",
                Style = (Style)Application.Current.FindResource("ModernButton"),
                Background = (Brush)Application.Current.FindResource("AccentBrush"),
                Margin = new Thickness(0, 0, 8, 0),
                MinWidth = 120,
                Padding = new Thickness(12, 8, 12, 8)
            };
            SetBinding(addButton, Button.CommandProperty, "AddOrderCommand", dataContext);

            var refreshButton = new Button
            {
                Content = "🔄 Обновить",
                Style = (Style)Application.Current.FindResource("ModernButton"),
                Background = (Brush)Application.Current.FindResource("WarningBrush"),
                Margin = new Thickness(0, 0, 8, 0),
                MinWidth = 100,
                Padding = new Thickness(12, 8, 12, 8)
            };
            SetBinding(refreshButton, Button.CommandProperty, "RefreshOrdersCommand", dataContext);

            var workPlanButton = new Button
            {
                Content = "📋 План работ",
                Style = (Style)Application.Current.FindResource("ModernButton"),
                Background = (Brush)Application.Current.FindResource("InfoBrush"),
                MinWidth = 100,
                Padding = new Thickness(12, 8, 12, 8)
            };
            SetBinding(workPlanButton, Button.CommandProperty, "ShowWorkPlanCommand", dataContext);

            buttonsPanel.Children.Add(addButton);
            buttonsPanel.Children.Add(refreshButton);
            buttonsPanel.Children.Add(workPlanButton);
            controlGrid.Children.Add(buttonsPanel);

            Grid.SetRow(controlGrid, 0);
            grid.Children.Add(controlGrid);

            // 2. Поиск
            var searchBorder = new Border
            {
                Background = (Brush)Application.Current.FindResource("SecondaryBrush"),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var searchGrid = new Grid();
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var searchIcon = new TextBlock
            {
                Text = "🔍",
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 8, 0)
            };
            Grid.SetColumn(searchIcon, 0);
            searchGrid.Children.Add(searchIcon);

            var searchBox = new TextBox
            {
                Style = (Style)Application.Current.FindResource("ModernTextBox"),
                Tag = "Поиск по названию, клиенту, категории или приоритету...",
                MinWidth = 200,
                Margin = new Thickness(0, 0, 8, 0)
            };
            SetBinding(searchBox, TextBox.TextProperty, "OrderSearchText", dataContext, BindingMode.TwoWay, UpdateSourceTrigger.PropertyChanged);
            Grid.SetColumn(searchBox, 1);
            searchGrid.Children.Add(searchBox);

            var searchResultText = new TextBlock
            {
                Foreground = (Brush)Application.Current.FindResource("TextSecondaryBrush"),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            };
            SetBinding(searchResultText, TextBlock.TextProperty, "FilteredOrders.Count", dataContext, BindingMode.OneWay, UpdateSourceTrigger.PropertyChanged,
                new StringFormatConverter() { StringFormat = "Найдено: {0}" });
            Grid.SetColumn(searchResultText, 2);
            searchGrid.Children.Add(searchResultText);

            searchBorder.Child = searchGrid;
            Grid.SetRow(searchBorder, 1);
            grid.Children.Add(searchBorder);

            // 3. Форма заказа (добавляем форму редактирования)
            var formBorder = new Border
            {
                Background = (Brush)Application.Current.FindResource("SecondaryBrush"),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 12),
                BorderThickness = new Thickness(1),
                BorderBrush = (Brush)Application.Current.FindResource("BorderBrush")
            };
            SetBinding(formBorder, Border.VisibilityProperty, "IsOrderFormVisible", dataContext, converter: new BooleanToVisibilityConverter());

            var formGrid = new Grid();
            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Первая строка формы
            var firstRow = new UniformGrid { Columns = 4, Margin = new Thickness(0, 0, 0, 8) };

            var clientStack = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
            var clientLabel = new TextBlock
            {
                Text = "Клиент *",
                Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            var clientComboBox = new ComboBox
            {
                Style = (Style)Application.Current.FindResource("ModernComboBox"),
                DisplayMemberPath = "CompanyName",
                SelectedValuePath = "Id"
            };
            SetBinding(clientComboBox, ComboBox.ItemsSourceProperty, "AllClients", dataContext);
            SetBinding(clientComboBox, ComboBox.SelectedValueProperty, "CurrentOrder.ClientId", dataContext, BindingMode.TwoWay);
            clientStack.Children.Add(clientLabel);
            clientStack.Children.Add(clientComboBox);

            var categoryStack = new StackPanel { Margin = new Thickness(4, 0, 4, 0) };
            var categoryLabel = new TextBlock
            {
                Text = "Категория *",
                Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            var categoryComboBox = new ComboBox
            {
                Style = (Style)Application.Current.FindResource("ModernComboBox"),
                DisplayMemberPath = "Name",
                SelectedValuePath = "Id"
            };
            SetBinding(categoryComboBox, ComboBox.ItemsSourceProperty, "AllCategories", dataContext);
            SetBinding(categoryComboBox, ComboBox.SelectedValueProperty, "CurrentOrder.CategoryId", dataContext, BindingMode.TwoWay);
            categoryStack.Children.Add(categoryLabel);
            categoryStack.Children.Add(categoryComboBox);

            var priorityStack = new StackPanel { Margin = new Thickness(4, 0, 4, 0) };
            var priorityLabel = new TextBlock
            {
                Text = "Приоритет *",
                Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            var priorityComboBox = new ComboBox
            {
                Style = (Style)Application.Current.FindResource("ModernComboBox"),
                DisplayMemberPath = "Name",
                SelectedValuePath = "Id"
            };
            SetBinding(priorityComboBox, ComboBox.ItemsSourceProperty, "AllPriorities", dataContext);
            SetBinding(priorityComboBox, ComboBox.SelectedValueProperty, "CurrentOrder.PriorityId", dataContext, BindingMode.TwoWay);
            priorityStack.Children.Add(priorityLabel);
            priorityStack.Children.Add(priorityComboBox);

            var statusStack = new StackPanel { Margin = new Thickness(8, 0, 0, 0) };
            var statusLabel = new TextBlock
            {
                Text = "Статус",
                Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            var statusComboBox = new ComboBox
            {
                Style = (Style)Application.Current.FindResource("ModernComboBox"),
                DisplayMemberPath = "Name",
                SelectedValuePath = "Id"
            };
            SetBinding(statusComboBox, ComboBox.ItemsSourceProperty, "AllStatuses", dataContext);
            SetBinding(statusComboBox, ComboBox.SelectedValueProperty, "CurrentOrder.StatusId", dataContext, BindingMode.TwoWay);
            statusStack.Children.Add(statusLabel);
            statusStack.Children.Add(statusComboBox);

            firstRow.Children.Add(clientStack);
            firstRow.Children.Add(categoryStack);
            firstRow.Children.Add(priorityStack);
            firstRow.Children.Add(statusStack);
            Grid.SetRow(firstRow, 0);
            formGrid.Children.Add(firstRow);

            // Вторая строка формы
            var secondRow = new Grid();
            secondRow.Margin = new Thickness(0, 0, 0, 8);
            secondRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            secondRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleStack = new StackPanel { Margin = new Thickness(0, 0, 12, 0) };
            var titleLabel = new TextBlock
            {
                Text = "Название заказа *",
                Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            var titleTextBox = new TextBox
            {
                Style = (Style)Application.Current.FindResource("ModernTextBox"),
                Tag = "Введите название заказа"
            };
            SetBinding(titleTextBox, TextBox.TextProperty, "CurrentOrder.Title", dataContext, BindingMode.TwoWay, UpdateSourceTrigger.PropertyChanged);
            titleStack.Children.Add(titleLabel);
            titleStack.Children.Add(titleTextBox);
            Grid.SetColumn(titleStack, 0);
            secondRow.Children.Add(titleStack);

            var dueDateStack = new StackPanel { Width = 200 };
            var dueDateLabel = new TextBlock
            {
                Text = "Срок выполнения",
                Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            var dueDatePicker = new DatePicker
            {
                Style = (Style)Application.Current.FindResource("ModernDatePicker")
            };
            SetBinding(dueDatePicker, DatePicker.SelectedDateProperty, "CurrentOrder.DueDate", dataContext, BindingMode.TwoWay);
            dueDateStack.Children.Add(dueDateLabel);
            dueDateStack.Children.Add(dueDatePicker);
            Grid.SetColumn(dueDateStack, 1);
            secondRow.Children.Add(dueDateStack);

            Grid.SetRow(secondRow, 1);
            formGrid.Children.Add(secondRow);

            // Третья строка формы
            var thirdRow = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
            var descriptionLabel = new TextBlock
            {
                Text = "Описание",
                Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            var descriptionTextBox = new TextBox
            {
                Style = (Style)Application.Current.FindResource("ModernTextBox"),
                Tag = "Введите описание заказа",
                Height = 60,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            SetBinding(descriptionTextBox, TextBox.TextProperty, "CurrentOrder.Description", dataContext, BindingMode.TwoWay, UpdateSourceTrigger.PropertyChanged);
            thirdRow.Children.Add(descriptionLabel);
            thirdRow.Children.Add(descriptionTextBox);
            Grid.SetRow(thirdRow, 2);
            formGrid.Children.Add(thirdRow);

            // Четвертая строка - кнопки
            var buttonsRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var saveButton = new Button
            {
                Content = "💾 Сохранить",
                Style = (Style)Application.Current.FindResource("ModernButton"),
                Background = (Brush)Application.Current.FindResource("SuccessBrush"),
                Margin = new Thickness(0, 0, 8, 0),
                MinWidth = 100,
                Padding = new Thickness(12, 8, 12, 8)
            };
            SetBinding(saveButton, Button.CommandProperty, "SaveOrderCommand", dataContext);

            var cancelButton = new Button
            {
                Content = "❌ Отмена",
                Style = (Style)Application.Current.FindResource("ModernButton"),
                Background = (Brush)Application.Current.FindResource("ErrorBrush"),
                MinWidth = 80,
                Padding = new Thickness(12, 8, 12, 8)
            };
            SetBinding(cancelButton, Button.CommandProperty, "CancelOrderCommand", dataContext);

            buttonsRow.Children.Add(saveButton);
            buttonsRow.Children.Add(cancelButton);
            Grid.SetRow(buttonsRow, 3);
            formGrid.Children.Add(buttonsRow);

            formBorder.Child = formGrid;
            Grid.SetRow(formBorder, 2);
            grid.Children.Add(formBorder);

            // 4. Таблица заказов
            var dataGrid = new DataGrid
            {
                Style = (Style)Application.Current.FindResource("ModernDataGrid"),
                ColumnHeaderStyle = (Style)Application.Current.FindResource("ModernDataGridColumnHeader"),
                RowStyle = (Style)Application.Current.FindResource("ModernDataGridRow"),
                CellStyle = (Style)Application.Current.FindResource("ModernDataGridCell"),
                AutoGenerateColumns = false,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            SetBinding(dataGrid, DataGrid.ItemsSourceProperty, "FilteredOrders", dataContext);

            // Колонки таблицы
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "№", Binding = new Binding("OrderNumber"), Width = 60 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Название", Binding = new Binding("Title"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 150 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Клиент", Binding = new Binding("Client.CompanyName"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 120 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Категория", Binding = new Binding("Category.Name"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 100 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Приоритет", Binding = new Binding("Priority.Name"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 100 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Статус", Binding = new Binding("Status.Name"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 100 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Срок выполнения", Binding = new Binding("DueDate") { StringFormat = "dd.MM.yyyy" }, Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 110 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Дата создания", Binding = new Binding("CreatedDate") { StringFormat = "dd.MM.yyyy HH:mm" }, Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 120 });

            // Колонка действий
            var actionsColumn = new DataGridTemplateColumn
            {
                Header = "Действия",
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                MinWidth = 100
            };

            var actionsTemplate = new DataTemplate();
            var actionsFactory = new FrameworkElementFactory(typeof(StackPanel));
            actionsFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            actionsFactory.SetValue(StackPanel.HorizontalAlignmentProperty, HorizontalAlignment.Center);

            // Кнопка редактирования
            var editButtonFactory = new FrameworkElementFactory(typeof(Button));
            editButtonFactory.SetValue(Button.ContentProperty, "✏️");
            editButtonFactory.SetValue(Button.StyleProperty, Application.Current.FindResource("TextButton"));
            editButtonFactory.SetValue(Button.ToolTipProperty, "Редактировать");
            editButtonFactory.SetValue(Button.PaddingProperty, new Thickness(6, 3, 6, 3));

            var editCommandBinding = new Binding("DataContext.EditOrderCommand");
            editCommandBinding.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGrid), 1);
            editButtonFactory.SetBinding(Button.CommandProperty, editCommandBinding);

            var editParameterBinding = new Binding("Id");
            editButtonFactory.SetBinding(Button.CommandParameterProperty, editParameterBinding);

            // Кнопка удаления
            var deleteButtonFactory = new FrameworkElementFactory(typeof(Button));
            deleteButtonFactory.SetValue(Button.ContentProperty, "🗑️");
            deleteButtonFactory.SetValue(Button.StyleProperty, Application.Current.FindResource("TextButton"));
            deleteButtonFactory.SetValue(Button.ToolTipProperty, "Удалить");
            deleteButtonFactory.SetValue(Button.PaddingProperty, new Thickness(6, 3, 6, 3));
            deleteButtonFactory.SetValue(Button.MarginProperty, new Thickness(4, 0, 0, 0));
            deleteButtonFactory.SetValue(Button.ForegroundProperty, Application.Current.FindResource("ErrorBrush"));

            var deleteCommandBinding = new Binding("DataContext.DeleteOrderCommand");
            deleteCommandBinding.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGrid), 1);
            deleteButtonFactory.SetBinding(Button.CommandProperty, deleteCommandBinding);

            var deleteParameterBinding = new Binding("Id");
            deleteButtonFactory.SetBinding(Button.CommandParameterProperty, deleteParameterBinding);

            actionsFactory.AppendChild(editButtonFactory);
            actionsFactory.AppendChild(deleteButtonFactory);
            actionsTemplate.VisualTree = actionsFactory;
            actionsColumn.CellTemplate = actionsTemplate;

            dataGrid.Columns.Add(actionsColumn);

            Grid.SetRow(dataGrid, 3);
            grid.Children.Add(dataGrid);

            return grid;
        }



        // Полное содержимое для вкладки "Планы работ"
        // Полное содержимое для вкладки "Планы работ" с формой редактирования
        private static UIElement CreateFullWorkPlansContent(object dataContext)
        {
            var grid = new Grid
            {
                Margin = new Thickness(8),
                UseLayoutRounding = true,
                DataContext = dataContext
            };

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // 1. Панель управления
            var controlGrid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            controlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            controlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            controlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleText = new TextBlock
            {
                Text = "Управление планами работ",
                Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 16, 0)
            };
            Grid.SetColumn(titleText, 0);
            controlGrid.Children.Add(titleText);

            var buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(buttonsPanel, 2);

            var addButton = new Button
            {
                Content = "➕ Создать план",
                Style = (Style)Application.Current.FindResource("ModernButton"),
                Background = (Brush)Application.Current.FindResource("SuccessBrush"),
                Margin = new Thickness(0, 0, 8, 0),
                MinWidth = 120,
                Padding = new Thickness(12, 8, 12, 8)
            };
            SetBinding(addButton, Button.CommandProperty, "AddWorkPlanCommand", dataContext);

            var refreshButton = new Button
            {
                Content = "🔄 Обновить",
                Style = (Style)Application.Current.FindResource("ModernButton"),
                Background = (Brush)Application.Current.FindResource("WarningBrush"),
                MinWidth = 100,
                Padding = new Thickness(12, 8, 12, 8)
            };
            SetBinding(refreshButton, Button.CommandProperty, "RefreshWorkPlansCommand", dataContext);

            buttonsPanel.Children.Add(addButton);
            buttonsPanel.Children.Add(refreshButton);
            controlGrid.Children.Add(buttonsPanel);

            Grid.SetRow(controlGrid, 0);
            grid.Children.Add(controlGrid);

            // 2. Поиск
            var searchBorder = new Border
            {
                Background = (Brush)Application.Current.FindResource("SecondaryBrush"),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var searchGrid = new Grid();
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var searchIcon = new TextBlock
            {
                Text = "🔍",
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 8, 0)
            };
            Grid.SetColumn(searchIcon, 0);
            searchGrid.Children.Add(searchIcon);

            var searchBox = new TextBox
            {
                Style = (Style)Application.Current.FindResource("ModernTextBox"),
                Tag = "Поиск по названию или плану действий...",
                MinWidth = 200,
                Margin = new Thickness(0, 0, 8, 0)
            };
            SetBinding(searchBox, TextBox.TextProperty, "WorkPlanSearchText", dataContext, BindingMode.TwoWay, UpdateSourceTrigger.PropertyChanged);
            Grid.SetColumn(searchBox, 1);
            searchGrid.Children.Add(searchBox);

            var searchResultText = new TextBlock
            {
                Foreground = (Brush)Application.Current.FindResource("TextSecondaryBrush"),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            };
            SetBinding(searchResultText, TextBlock.TextProperty, "FilteredWorkPlans.Count", dataContext, BindingMode.OneWay, UpdateSourceTrigger.PropertyChanged,
                new StringFormatConverter() { StringFormat = "Найдено: {0}" });
            Grid.SetColumn(searchResultText, 2);
            searchGrid.Children.Add(searchResultText);

            searchBorder.Child = searchGrid;
            Grid.SetRow(searchBorder, 1);
            grid.Children.Add(searchBorder);

            // 3. Форма плана работ
            var formBorder = new Border
            {
                Background = (Brush)Application.Current.FindResource("SecondaryBrush"),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 12),
                BorderThickness = new Thickness(1),
                BorderBrush = (Brush)Application.Current.FindResource("BorderBrush")
            };
            SetBinding(formBorder, Border.VisibilityProperty, "IsWorkPlanFormVisible", dataContext, converter: new BooleanToVisibilityConverter());

            var formGrid = new Grid();
            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Первая строка формы
            var firstRow = new Grid();
            firstRow.Margin = new Thickness(0, 0, 0, 8);
            firstRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            firstRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var descriptionStack = new StackPanel { Margin = new Thickness(0, 0, 12, 0) };
            var descriptionLabel = new TextBlock
            {
                Text = "Описание плана *",
                Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            var descriptionTextBox = new TextBox
            {
                Style = (Style)Application.Current.FindResource("ModernTextBox"),
                Tag = "Введите описание плана работ"
            };
            SetBinding(descriptionTextBox, TextBox.TextProperty, "CurrentWorkPlan.PlanDescription", dataContext, BindingMode.TwoWay, UpdateSourceTrigger.PropertyChanged);
            descriptionStack.Children.Add(descriptionLabel);
            descriptionStack.Children.Add(descriptionTextBox);
            Grid.SetColumn(descriptionStack, 0);
            firstRow.Children.Add(descriptionStack);

            var hoursStack = new StackPanel { Width = 120 };
            var hoursLabel = new TextBlock
            {
                Text = "Часы *",
                Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            var hoursTextBox = new TextBox
            {
                Style = (Style)Application.Current.FindResource("ModernTextBox"),
                Tag = "Часы"
            };
            SetBinding(hoursTextBox, TextBox.TextProperty, "CurrentWorkPlan.EstimatedHours", dataContext, BindingMode.TwoWay, UpdateSourceTrigger.PropertyChanged);
            hoursStack.Children.Add(hoursLabel);
            hoursStack.Children.Add(hoursTextBox);
            Grid.SetColumn(hoursStack, 1);
            firstRow.Children.Add(hoursStack);

            Grid.SetRow(firstRow, 0);
            formGrid.Children.Add(firstRow);

            // Вторая строка формы
            var secondRow = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
            var stepsLabel = new TextBlock
            {
                Text = "Шаги тестирования",
                Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            var stepsTextBox = new TextBox
            {
                Style = (Style)Application.Current.FindResource("ModernTextBox"),
                Tag = "Опишите шаги тестирования...",
                Height = 80,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            SetBinding(stepsTextBox, TextBox.TextProperty, "CurrentWorkPlan.TestSteps", dataContext, BindingMode.TwoWay, UpdateSourceTrigger.PropertyChanged);
            secondRow.Children.Add(stepsLabel);
            secondRow.Children.Add(stepsTextBox);
            Grid.SetRow(secondRow, 1);
            formGrid.Children.Add(secondRow);

            // Третья строка формы
            var thirdRow = new UniformGrid { Columns = 3, Margin = new Thickness(0, 0, 0, 8) };

            var typeStack = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
            var typeLabel = new TextBlock
            {
                Text = "Тип плана",
                Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            var typeComboBox = new ComboBox
            {
                Style = (Style)Application.Current.FindResource("ModernComboBox"),
                DisplayMemberPath = "Name",
                SelectedValuePath = "Id"
            };
            SetBinding(typeComboBox, ComboBox.ItemsSourceProperty, "AllWorkPlanTypes", dataContext);
            SetBinding(typeComboBox, ComboBox.SelectedValueProperty, "CurrentWorkPlan.TypeId", dataContext, BindingMode.TwoWay);
            typeStack.Children.Add(typeLabel);
            typeStack.Children.Add(typeComboBox);

            var complexityStack = new StackPanel { Margin = new Thickness(4, 0, 4, 0) };
            var complexityLabel = new TextBlock
            {
                Text = "Сложность",
                Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            var complexityComboBox = new ComboBox
            {
                Style = (Style)Application.Current.FindResource("ModernComboBox"),
                DisplayMemberPath = "Name",
                SelectedValuePath = "Id"
            };
            SetBinding(complexityComboBox, ComboBox.ItemsSourceProperty, "AllComplexities", dataContext);
            SetBinding(complexityComboBox, ComboBox.SelectedValueProperty, "CurrentWorkPlan.ComplexityId", dataContext, BindingMode.TwoWay);
            complexityStack.Children.Add(complexityLabel);
            complexityStack.Children.Add(complexityComboBox);

            var statusStack = new StackPanel { Margin = new Thickness(8, 0, 0, 0) };
            var statusLabel = new TextBlock
            {
                Text = "Статус",
                Foreground = (Brush)Application.Current.FindResource("TextBrush"),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            var statusComboBox = new ComboBox
            {
                Style = (Style)Application.Current.FindResource("ModernComboBox"),
                DisplayMemberPath = "Name",
                SelectedValuePath = "Id"
            };
            SetBinding(statusComboBox, ComboBox.ItemsSourceProperty, "AllWorkPlanStatuses", dataContext);
            SetBinding(statusComboBox, ComboBox.SelectedValueProperty, "CurrentWorkPlan.StatusId", dataContext, BindingMode.TwoWay);
            statusStack.Children.Add(statusLabel);
            statusStack.Children.Add(statusComboBox);

            thirdRow.Children.Add(typeStack);
            thirdRow.Children.Add(complexityStack);
            thirdRow.Children.Add(statusStack);
            Grid.SetRow(thirdRow, 2);
            formGrid.Children.Add(thirdRow);

            // Четвертая строка - кнопки
            var buttonsRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var saveButton = new Button
            {
                Content = "💾 Сохранить",
                Style = (Style)Application.Current.FindResource("ModernButton"),
                Background = (Brush)Application.Current.FindResource("SuccessBrush"),
                Margin = new Thickness(0, 0, 8, 0),
                MinWidth = 100,
                Padding = new Thickness(12, 8, 12, 8)
            };
            SetBinding(saveButton, Button.CommandProperty, "SaveWorkPlanCommand", dataContext);

            var cancelButton = new Button
            {
                Content = "❌ Отмена",
                Style = (Style)Application.Current.FindResource("ModernButton"),
                Background = (Brush)Application.Current.FindResource("ErrorBrush"),
                MinWidth = 80,
                Padding = new Thickness(12, 8, 12, 8)
            };
            SetBinding(cancelButton, Button.CommandProperty, "CancelWorkPlanCommand", dataContext);

            buttonsRow.Children.Add(saveButton);
            buttonsRow.Children.Add(cancelButton);
            Grid.SetRow(buttonsRow, 3);
            formGrid.Children.Add(buttonsRow);

            formBorder.Child = formGrid;
            Grid.SetRow(formBorder, 2);
            grid.Children.Add(formBorder);

            // 4. Таблица планов работ
            var dataGrid = new DataGrid
            {
                Style = (Style)Application.Current.FindResource("ModernDataGrid"),
                ColumnHeaderStyle = (Style)Application.Current.FindResource("ModernDataGridColumnHeader"),
                RowStyle = (Style)Application.Current.FindResource("ModernDataGridRow"),
                CellStyle = (Style)Application.Current.FindResource("ModernDataGridCell"),
                AutoGenerateColumns = false,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            SetBinding(dataGrid, DataGrid.ItemsSourceProperty, "FilteredWorkPlans", dataContext);

            // Колонки таблицы
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "№", Binding = new Binding("OrderNumber"), Width = 60 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Описание", Binding = new Binding("PlanDescription"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 200 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Шаги плана", Binding = new Binding("TestSteps"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 150 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Часы", Binding = new Binding("EstimatedHours"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 80 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Тип", Binding = new Binding("Type.Name"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 100 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Сложность", Binding = new Binding("Complexity.Name"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 100 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Статус", Binding = new Binding("Status.Name"), Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 100 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Дата создания", Binding = new Binding("CreatedDate") { StringFormat = "dd.MM.yyyy HH:mm" }, Width = new DataGridLength(1, DataGridLengthUnitType.Star), MinWidth = 120 });

            // Колонка действий
            var actionsColumn = new DataGridTemplateColumn
            {
                Header = "Действия",
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                MinWidth = 100
            };

            var actionsTemplate = new DataTemplate();
            var actionsFactory = new FrameworkElementFactory(typeof(StackPanel));
            actionsFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            actionsFactory.SetValue(StackPanel.HorizontalAlignmentProperty, HorizontalAlignment.Center);

            // Кнопка редактирования
            var editButtonFactory = new FrameworkElementFactory(typeof(Button));
            editButtonFactory.SetValue(Button.ContentProperty, "✏️");
            editButtonFactory.SetValue(Button.StyleProperty, Application.Current.FindResource("TextButton"));
            editButtonFactory.SetValue(Button.ToolTipProperty, "Редактировать");
            editButtonFactory.SetValue(Button.PaddingProperty, new Thickness(6, 3, 6, 3));

            var editCommandBinding = new Binding("DataContext.EditWorkPlanCommand");
            editCommandBinding.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGrid), 1);
            editButtonFactory.SetBinding(Button.CommandProperty, editCommandBinding);

            var editParameterBinding = new Binding("Id");
            editButtonFactory.SetBinding(Button.CommandParameterProperty, editParameterBinding);

            // Кнопка удаления
            var deleteButtonFactory = new FrameworkElementFactory(typeof(Button));
            deleteButtonFactory.SetValue(Button.ContentProperty, "🗑️");
            deleteButtonFactory.SetValue(Button.StyleProperty, Application.Current.FindResource("TextButton"));
            deleteButtonFactory.SetValue(Button.ToolTipProperty, "Удалить");
            deleteButtonFactory.SetValue(Button.PaddingProperty, new Thickness(6, 3, 6, 3));
            deleteButtonFactory.SetValue(Button.MarginProperty, new Thickness(4, 0, 0, 0));
            deleteButtonFactory.SetValue(Button.ForegroundProperty, Application.Current.FindResource("ErrorBrush"));

            var deleteCommandBinding = new Binding("DataContext.DeleteWorkPlanCommand");
            deleteCommandBinding.RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGrid), 1);
            deleteButtonFactory.SetBinding(Button.CommandProperty, deleteCommandBinding);

            var deleteParameterBinding = new Binding("Id");
            deleteButtonFactory.SetBinding(Button.CommandParameterProperty, deleteParameterBinding);

            actionsFactory.AppendChild(editButtonFactory);
            actionsFactory.AppendChild(deleteButtonFactory);
            actionsTemplate.VisualTree = actionsFactory;
            actionsColumn.CellTemplate = actionsTemplate;

            dataGrid.Columns.Add(actionsColumn);

            Grid.SetRow(dataGrid, 3);
            grid.Children.Add(dataGrid);

            return grid;
        }

        private static UIElement CreateFallbackContent(string header)
        {
            return new TextBlock
            {
                Text = $"Содержимое вкладки: {header}",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 16
            };
        }

        // Вспомогательный метод для установки привязок
        // Вспомогательный метод для установки привязок
        private static void SetBinding(DependencyObject target, DependencyProperty property, string path, object source,
            BindingMode mode = BindingMode.OneWay, UpdateSourceTrigger trigger = UpdateSourceTrigger.PropertyChanged,
            IValueConverter converter = null)
        {
            var binding = new Binding(path)
            {
                Source = source,
                Mode = mode,
                UpdateSourceTrigger = trigger
            };

            if (converter != null)
            {
                binding.Converter = converter;
            }

            BindingOperations.SetBinding(target, property, binding);
        }

        public void CloseWithoutReturn()
        {
            _isReturned = true;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            // При закрытии окна возвращаем вкладку, если она еще не возвращена
            if (!_isReturned && _originalTabControl != null)
            {
                try
                {
                    // Создаем новое содержимое для возврата на основе заголовка
                    var contentCopy = CreateContentFromHeader(_header, _dataContext);

                    var newTabItem = new TabItem
                    {
                        Content = contentCopy,
                        Header = _header,
                        Style = _style
                    };

                    // Добавляем вкладку обратно в оригинальный TabControl
                    _originalTabControl.Items.Add(newTabItem);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при закрытии окна: {ex.Message}");
                }
            }

            base.OnClosed(e);
        }
    }
}