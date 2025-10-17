using FinanceGPT.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;

namespace FinanceGPT
{
    public class DatabaseContext : DbContext
    {
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var databasePath = Path.Combine(Directory.GetCurrentDirectory(), "FinanceGPT.db");
            optionsBuilder.UseSqlite($"Data Source={databasePath}");
        }
    }
}