using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;

namespace FinanceGPT.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private string currentPage = "Dashboard";

        public void NavigateTo(string page)
        {
        //    CurrentPage = page;
        }
    }
}
