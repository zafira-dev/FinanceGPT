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
using System.Windows.Shapes;
using System.Data;
using System.Data.SqlClient;

namespace FinanceGPT
{
    public partial class AddTransactionWindow : Window
    {
        private bool isIncome = false;

        public AddTransactionWindow()
        {
            InitializeComponent();
            LoadCategories();
            TransactionDatePicker.SelectedDate = DateTime.Now;

            // Set default to expense
            ExpenseButton_Click(null, null);
        }

        private void LoadCategories()
        {
            try
            {
                using (SqlConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string query = "SELECT CategoryID, Name, Icon FROM Categories WHERE IsActive = 1 AND Type = @Type ORDER BY Name";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Type", isIncome ? "Income" : "Expense");

                        SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        // Add display text with icon
                        dt.Columns.Add("DisplayText", typeof(string));
                        foreach (DataRow row in dt.Rows)
                        {
                            row["DisplayText"] = $"{row["Icon"]} {row["Name"]}";
                        }

                        CategoryComboBox.ItemsSource = dt.DefaultView;

                        if (dt.Rows.Count > 0)
                        {
                            CategoryComboBox.SelectedIndex = 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading categories: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void IncomeButton_Click(object sender, RoutedEventArgs e)
        {
            isIncome = true;

            // Update button styles
            IncomeButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#c6f6d5"));
            IncomeButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22543d"));
            IncomeButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#48bb78"));

            ExpenseButton.Background = Brushes.White;
            ExpenseButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#718096"));
            ExpenseButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e2e8f0"));

            LoadCategories();
        }

        private void ExpenseButton_Click(object sender, RoutedEventArgs e)
        {
            isIncome = false;

            // Update button styles
            ExpenseButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fed7d7"));
            ExpenseButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#742a2a"));
            ExpenseButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f56565"));

            IncomeButton.Background = Brushes.White;
            IncomeButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#718096"));
            IncomeButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e2e8f0"));

            LoadCategories();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(AmountTextBox.Text) || !decimal.TryParse(AmountTextBox.Text, out decimal amount))
            {
                MessageBox.Show("Please enter a valid amount.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (amount <= 0)
            {
                MessageBox.Show("Amount must be greater than 0.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(DescriptionTextBox.Text) ||
                DescriptionTextBox.Text == "Enter description...")
            {
                MessageBox.Show("Please enter a description.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CategoryComboBox.SelectedValue == null)
            {
                MessageBox.Show("Please select a category.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (TransactionDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Please select a date.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Save to database
            try
            {
                using (SqlConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string query = @"INSERT INTO Transactions (Date, Amount, Description, CategoryID, IsIncome, Notes, CreatedDate)
                                   VALUES (@Date, @Amount, @Description, @CategoryID, @IsIncome, @Notes, @CreatedDate)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Date", TransactionDatePicker.SelectedDate.Value);
                        cmd.Parameters.AddWithValue("@Amount", amount);
                        cmd.Parameters.AddWithValue("@Description", DescriptionTextBox.Text);
                        cmd.Parameters.AddWithValue("@CategoryID", CategoryComboBox.SelectedValue);
                        cmd.Parameters.AddWithValue("@IsIncome", isIncome);

                        string notes = NotesTextBox.Text;
                        if (notes == "Add any additional notes...")
                            notes = "";
                        cmd.Parameters.AddWithValue("@Notes", string.IsNullOrWhiteSpace(notes) ? (object)DBNull.Value : notes);
                        cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Transaction saved successfully!", "Success",
                                MessageBoxButton.OK, MessageBoxImage.Information);

                            // Close window and return success
                            this.DialogResult = true;
                            this.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving transaction: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}