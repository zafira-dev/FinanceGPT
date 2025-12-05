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
using System.Data;
using System.Data.SqlClient;


namespace FinanceGPT
{
    public partial class MainWindow : Window
    {
        private OllamaService _ollamaService;
        private bool _isProcessing = false;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize Ollama service
            _ollamaService = new OllamaService("http://localhost:11434", "llama2");

            // Test database connection
            if (!DatabaseHelper.TestConnection(out string error))
            {
                MessageBox.Show($"Database connection failed: {error}\n\nPlease check your connection string in DatabaseHelper.cs",
                    "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            LoadDashboardData();
            LoadRecentTransactions();
            CheckOllamaStatus();

            // Wire up chat functionality
            SendChatButton.Click += SendChatButton_Click;
            ChatInput.KeyDown += ChatInput_KeyDown;
            ChatInput.GotFocus += ChatInput_GotFocus;
            ChatInput.LostFocus += ChatInput_LostFocus;
        }

        private async void CheckOllamaStatus()
        {
            bool isRunning = await _ollamaService.IsOllamaRunningAsync();

            if (!isRunning)
            {
                AddAIMessage("⚠️ Ollama is not running. Please start Ollama to use AI features.\n\nTo start Ollama:\n1. Open terminal/command prompt\n2. Run: ollama serve\n3. Or start Ollama application", "#fff3cd", "#856404");
                SendChatButton.IsEnabled = false;
            }
        }

        private void ChatInput_GotFocus(object sender, RoutedEventArgs e)
        {
            if (ChatInput.Text == "Ask anything about your finances...")
            {
                ChatInput.Text = "";
                ChatInput.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2d3748"));
            }
        }

        private void ChatInput_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ChatInput.Text))
            {
                ChatInput.Text = "Ask anything about your finances...";
                ChatInput.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#a0aec0"));
            }
        }

        private void ChatInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !_isProcessing)
            {
                SendChatButton_Click(sender, e);
            }
        }

        private async void SendChatButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessing) return;

            string userMessage = ChatInput.Text.Trim();

            if (string.IsNullOrWhiteSpace(userMessage) || userMessage == "Ask anything about your finances...")
            {
                return;
            }

            _isProcessing = true;
            SendChatButton.IsEnabled = false;
            ChatInput.IsEnabled = false;

            // Add user message to chat
            AddUserMessage(userMessage);
            ChatInput.Text = "";

            // Scroll to bottom
            ChatMessagesScroll.ScrollToBottom();

            try
            {
                // Get financial context
                string financialContext = FinanceDataService.GetFinancialContext();

                // Create prompt with context
                string prompt = $@"You are a helpful personal finance assistant analyzing a user's financial data.

Here is the user's financial information:
{financialContext}

User Question: {userMessage}

Please provide a helpful, concise answer based on the financial data above. Be specific with numbers and dates when relevant. If the data doesn't contain information to answer the question, politely let the user know.

Keep your response under 150 words and use a friendly tone.";

                // Create a placeholder message for AI response
                Border aiMessageBorder = AddAIMessagePlaceholder();

                // Get AI response with streaming
                StringBuilder fullResponse = new StringBuilder();

                await _ollamaService.GenerateResponseStreamAsync(prompt, (chunk) =>
                {
                    fullResponse.Append(chunk);

                    // Update UI on dispatcher thread
                    Dispatcher.Invoke(() =>
                    {
                        UpdateAIMessageContent(aiMessageBorder, fullResponse.ToString());
                        ChatMessagesScroll.ScrollToBottom();
                    });
                });

                // If no streaming response, fall back to regular response
                if (fullResponse.Length == 0)
                {
                    string response = await _ollamaService.GenerateResponseAsync(prompt);
                    Dispatcher.Invoke(() =>
                    {
                        UpdateAIMessageContent(aiMessageBorder, response);
                        ChatMessagesScroll.ScrollToBottom();
                    });
                }
            }
            catch (Exception ex)
            {
                AddAIMessage($"Sorry, I encountered an error: {ex.Message}", "#fed7d7", "#742a2a");
            }
            finally
            {
                _isProcessing = false;
                SendChatButton.IsEnabled = true;
                ChatInput.IsEnabled = true;
                ChatInput.Focus();
            }
        }

        private void AddUserMessage(string message)
        {
            Border messageBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#edf2f7")),
                Padding = new Thickness(12, 16, 12, 16),
                CornerRadius = new CornerRadius(12),
                Margin = new Thickness(0, 0, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Right,
                MaxWidth = 400
            };

            TextBlock messageText = new TextBlock
            {
                Text = message,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2d3748"))
            };

            messageBorder.Child = messageText;
            ChatMessages.Children.Add(messageBorder);
        }

        private void AddAIMessage(string message, string bgColor = "#f0f4ff", string borderColor = "#667eea")
        {
            Border messageBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bgColor)),
                Padding = new Thickness(12, 16, 12, 16),
                CornerRadius = new CornerRadius(12),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(borderColor)),
                MaxWidth = 400,
                Margin = new Thickness(0, 0, 0, 10)
            };

            TextBlock messageText = new TextBlock
            {
                Text = message,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2d3748"))
            };

            messageBorder.Child = messageText;
            ChatMessages.Children.Add(messageBorder);
        }

        private Border AddAIMessagePlaceholder()
        {
            Border messageBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f0f4ff")),
                Padding = new Thickness(12, 16, 12, 16),
                CornerRadius = new CornerRadius(12),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#667eea")),
                MaxWidth = 400,
                Margin = new Thickness(0, 0, 0, 10)
            };

            TextBlock messageText = new TextBlock
            {
                Text = "Thinking...",
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#718096")),
                FontStyle = FontStyles.Italic
            };

            messageBorder.Child = messageText;
            ChatMessages.Children.Add(messageBorder);

            return messageBorder;
        }

        private void UpdateAIMessageContent(Border messageBorder, string content)
        {
            if (messageBorder.Child is TextBlock textBlock)
            {
                textBlock.Text = content;
                textBlock.FontStyle = FontStyles.Normal;
                textBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2d3748"));
            }
        }

        private void AddTransactionButton_Click(object sender, RoutedEventArgs e)
        {
            AddTransactionWindow addWindow = new AddTransactionWindow();
            bool? result = addWindow.ShowDialog();

            // If transaction was saved successfully, refresh the dashboard
            if (result == true)
            {
                LoadDashboardData();
                LoadRecentTransactions();
            }
        }

        private void LoadDashboardData()
        {
            try
            {
                using (SqlConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();

                    // Get total balance
                    string balanceQuery = @"SELECT 
                        ISNULL(SUM(CASE WHEN IsIncome = 1 THEN Amount ELSE -Amount END), 0) as Balance
                        FROM Transactions";

                    using (SqlCommand cmd = new SqlCommand(balanceQuery, conn))
                    {
                        decimal balance = (decimal)cmd.ExecuteScalar();
                        TotalBalanceText.Text = $"£{balance:N2}";
                    }

                    // Get monthly spending
                    string spendingQuery = @"SELECT 
                        ISNULL(SUM(Amount), 0) as MonthlySpending
                        FROM Transactions
                        WHERE IsIncome = 0 
                        AND MONTH(Date) = MONTH(GETDATE()) 
                        AND YEAR(Date) = YEAR(GETDATE())";

                    using (SqlCommand cmd = new SqlCommand(spendingQuery, conn))
                    {
                        decimal spending = (decimal)cmd.ExecuteScalar();
                        MonthlySpendingText.Text = $"£{spending:N2}";
                    }

                    // Get monthly income
                    string incomeQuery = @"SELECT 
                        ISNULL(SUM(Amount), 0) as MonthlyIncome
                        FROM Transactions
                        WHERE IsIncome = 1 
                        AND MONTH(Date) = MONTH(GETDATE()) 
                        AND YEAR(Date) = YEAR(GETDATE())";

                    using (SqlCommand cmd = new SqlCommand(incomeQuery, conn))
                    {
                        decimal income = (decimal)cmd.ExecuteScalar();
                        MonthlyIncomeText.Text = $"£{income:N2}";

                        // Calculate savings rate
                        if (income > 0)
                        {
                            decimal spending = decimal.Parse(MonthlySpendingText.Text.Replace("£", "").Replace(",", ""));
                            decimal savingsRate = ((income - spending) / income) * 100;
                            SavingsRateText.Text = $"{savingsRate:F0}%";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading dashboard data: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadRecentTransactions()
        {
            try
            {
                TransactionsPanel.Children.Clear();

                using (SqlConnection conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string query = @"SELECT TOP 10 
                        t.TransactionID, t.Date, t.Amount, t.Description, 
                        t.IsIncome, c.Name as CategoryName, c.Icon, c.Color
                        FROM Transactions t
                        LEFT JOIN Categories c ON t.CategoryID = c.CategoryID
                        ORDER BY t.Date DESC, t.CreatedDate DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        SqlDataReader reader = cmd.ExecuteReader();

                        if (!reader.HasRows)
                        {
                            TextBlock noData = new TextBlock
                            {
                                Text = "No transactions yet. Click 'Add Transaction' to get started!",
                                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#718096")),
                                FontSize = 14,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                Margin = new Thickness(0, 20, 0, 20)
                            };
                            TransactionsPanel.Children.Add(noData);
                        }
                        else
                        {
                            while (reader.Read())
                            {
                                CreateTransactionItem(
                                    reader["Icon"].ToString(),
                                    reader["Description"].ToString(),
                                    reader["CategoryName"].ToString(),
                                    Convert.ToDateTime(reader["Date"]),
                                    Convert.ToDecimal(reader["Amount"]),
                                    Convert.ToBoolean(reader["IsIncome"]),
                                    reader["Color"].ToString()
                                );
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading transactions: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateTransactionItem(string icon, string description, string category,
            DateTime date, decimal amount, bool isIncome, string categoryColor)
        {
            // Create container
            Grid grid = new Grid
            {
                Margin = new Thickness(0, 0, 0, 15)
            };

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Icon
            Border iconBorder = new Border
            {
                Width = 45,
                Height = 45,
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(categoryColor + "30"))
            };

            TextBlock iconText = new TextBlock
            {
                Text = icon,
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            iconBorder.Child = iconText;
            Grid.SetColumn(iconBorder, 0);

            // Description and Category
            StackPanel detailsPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(15, 0, 0, 0)
            };

            TextBlock descText = new TextBlock
            {
                Text = description,
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2d3748"))
            };

            TextBlock categoryText = new TextBlock
            {
                Text = $"{category} • {date:MMM dd, yyyy}",
                FontSize = 13,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#718096")),
                Margin = new Thickness(0, 3, 0, 0)
            };

            detailsPanel.Children.Add(descText);
            detailsPanel.Children.Add(categoryText);
            Grid.SetColumn(detailsPanel, 1);

            // Amount
            TextBlock amountText = new TextBlock
            {
                Text = $"{(isIncome ? "+" : "-")}£{amount:N2}",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isIncome ? "#48bb78" : "#f56565")),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(amountText, 2);

            grid.Children.Add(iconBorder);
            grid.Children.Add(detailsPanel);
            grid.Children.Add(amountText);

            // Add separator
            Border separator = new Border
            {
                Height = 1,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e2e8f0")),
                Margin = new Thickness(0, 15, 0, 0)
            };

            TransactionsPanel.Children.Add(grid);
            TransactionsPanel.Children.Add(separator);
        }
    }
}