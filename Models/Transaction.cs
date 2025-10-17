using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinanceGPT.Models
{
        public class Transaction
        {
            public int Id { get; set; }
            public string Description { get; set; }
            public decimal Amount { get; set; }
            public string Category { get; set; }
            public DateTime Date { get; set; }
            public string Type => Amount >= 0 ? "Income" : "Expense";
        }
}
