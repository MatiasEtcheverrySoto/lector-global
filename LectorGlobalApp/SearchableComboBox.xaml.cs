using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace LectorGlobalApp
{
    public partial class SearchableComboBox : System.Windows.Controls.UserControl, INotifyPropertyChanged
    {
        public event SelectionChangedEventHandler SelectionChanged;

        public SearchableComboBox()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(SearchableComboBox), new PropertyMetadata(null, OnItemsSourceChanged));

        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchableComboBox control)
            {
                control.UpdateFilteredItems();
            }
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(object), typeof(SearchableComboBox), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));

        public object SelectedItem
        {
            get { return GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchableComboBox control)
            {
                // Dispara el evento SelectionChanged si es necesario
            }
        }

        public static readonly DependencyProperty SearchPlaceholderProperty =
            DependencyProperty.Register("SearchPlaceholder", typeof(string), typeof(SearchableComboBox), new PropertyMetadata("Search..."));

        public string SearchPlaceholder
        {
            get { return (string)GetValue(SearchPlaceholderProperty); }
            set { SetValue(SearchPlaceholderProperty, value); }
        }

        private IEnumerable _filteredItems;
        public IEnumerable FilteredItems
        {
            get { return _filteredItems; }
            set
            {
                _filteredItems = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FilteredItems)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void BtnToggle_Click(object sender, RoutedEventArgs e)
        {
            if (BtnToggle.IsChecked == true)
            {
                TxtSearch.Text = "";
                UpdateFilteredItems();
                TxtSearch.Focus();
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateFilteredItems();
        }

        private void UpdateFilteredItems()
        {
            if (ItemsSource == null)
            {
                FilteredItems = null;
                return;
            }

            string query = TxtSearch.Text?.ToLower() ?? "";
            
            var allItems = ItemsSource.Cast<object>().ToList();

            if (string.IsNullOrWhiteSpace(query))
            {
                FilteredItems = allItems;
            }
            else
            {
                FilteredItems = allItems.Where(i => i != null && i.ToString().ToLower().Contains(query)).ToList();
            }
        }

        private void LstItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstItems.SelectedItem != null)
            {
                SelectedItem = LstItems.SelectedItem;
                BtnToggle.IsChecked = false; // Cerrar el popup
                
                SelectionChanged?.Invoke(this, e);
            }
        }
    }
}
