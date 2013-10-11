using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

namespace DoStuff
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
		{
			//Handling the exception within the UnhandledExcpeiton handler.
			string message = e.Exception.Message;
			Exception ex = e.Exception;

			while ((ex = ex.InnerException) != null && !string.IsNullOrEmpty(ex.Message))
				message = string.Concat(message, "\r\rInner Exception: ", ex.Message);
			MessageBox.Show(message, "Exception Caught", MessageBoxButton.OK, MessageBoxImage.Error);
			e.Handled = true;
		}
	}
}