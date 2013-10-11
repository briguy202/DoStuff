using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Net;

namespace DoStuff
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		#region Properties
		public List<OperationType> Operations { get; set; }
		public OperationType SelectedOperation { get { return (this.ddlType.SelectedIndex >= 0) ? ((ComboBoxItem)this.ddlType.SelectedItem).Tag as OperationType : null; } }
		public ConfigurationDTO Configuration { get; set; }
		public string OpenedFile { get; set; }
		#endregion

		#region Constructors
		public MainWindow()
		{
			InitializeComponent();

			this.InitializeOperations();

			this.ddlType.Items.Clear();
			this.Operations.Sort((a, b) => a.Name.CompareTo(b.Name));
			foreach (OperationType operation in this.Operations)
			{
				this.ddlType.Items.Add(new ComboBoxItem() { Content = operation.Name, Tag = operation });
			}
			this.ddlType.SelectedIndex = 0;

			if (!string.IsNullOrEmpty(Properties.Settings.Default["LastOpened"].ToString()))
				this.ImportSettings(Properties.Settings.Default["LastOpened"].ToString(), false);
		}
		#endregion

		private void InitializeOperations()
		{
			this.Operations = new List<OperationType>()
			{
				#region Simple Formatter
				new OperationType() {
					Name = "Simple Formatter",
					ID = "simpleFormatter",
					DefaultSourceDescription = "A tokenized ({0}, {1}, {2}, etc.) string for replacement.  Can contain {guid_0-30} or {increment_0-30} replacements.",
					DefaultSourceValue = "Sandy sells {0} by the {1}",
					DefaultParametersDescription = "One or more lines of comma-separated token values",
					DefaultParametersValue = "seashells,seashore\npickles,costco\n",
					Execute = (source, iterators, parameters) => {
						Dictionary<int, int> counters = new Dictionary<int, int>();
						StringBuilder output = new StringBuilder();

						if (this.GetLines(iterators).Count > 0) {
							foreach (var iterator in this.GetLines(iterators))
								foreach (string line in this.GetLines(parameters))
									output.AppendLine(this.PerformSimpleFormatter(line, source, counters, iterator));
						} else {
							foreach (string line in this.GetLines(parameters))
								output.AppendLine(this.PerformSimpleFormatter(line, source, counters, null));
						}
						return output.ToString();
					}
				},
				#endregion

				#region Print all cultures
				new OperationType() {
					Name = "Cultures - List all",
					ID = "culturesListAll",
					Execute = (source, iterators, parameters) => {
						StringBuilder output = new StringBuilder();
						foreach (var item in CultureInfo.GetCultures(CultureTypes.AllCultures))
						{
							output.AppendLine(item.ToString());
						}
					    return output.ToString();
					}
				},
				#endregion

				#region Wrap Text
				new OperationType() {
					Name = "Wrap Text",
					ID = "wrapText",
					DefaultSourceDescription = "A list of strings that will be wrapped with a value",
					DefaultParametersDescription = "A %% separated pair of values that will be used at the beginning and end of each source value. \"{source}\" is replaced with the source line.",
					DefaultParametersValue = "leading value%%tailing value",
					Execute = (source, iterators, parameters) => {
						StringBuilder output = new StringBuilder();
						var pieces = parameters.Split(new string[] { "%%" }, StringSplitOptions.None);
						foreach (var sourceLine in this.GetLines(source))
						{
							output.Append(pieces[0].Replace("{source}", sourceLine));
							output.Append(sourceLine);
							if (pieces.Length == 2)
								output.Append(pieces[1].Replace("{source}", sourceLine));
							output.AppendLine();
						}
					    return output.ToString();
					}
				},
				#endregion

				#region Sort Text
				new OperationType() {
					Name = "Sort",
					ID = "sort",
					DefaultSourceDescription = "Sorts a list of strings",
					Execute = (source, iterators, parameters) => {
						StringBuilder output = new StringBuilder();
						var lines = this.GetLines(source);
						lines.Sort();
						foreach (var sourceLine in lines)
						{
							output.AppendLine(sourceLine);
						}
					    return output.ToString();
					}
				},
				#endregion

				#region URL Validator
				new OperationType() {
					Name = "URL Validator",
					ID = "urlvalidator",
					DefaultSourceDescription = "A list of URLs.",
					Execute = (source, iterators, parameters) => {
						var sourceLines = this.GetLines(source);
						StringBuilder output = new StringBuilder();

						foreach (var line in sourceLines) {
							var request = WebRequest.Create(line);
							try {
								using (var response = request.GetResponse()) {
									output.AppendLine(string.Concat("Valid - ", line));
								}
							} catch (Exception) {
								output.AppendLine(string.Concat("INVALID - ", line));
							}
						}

					    return output.ToString();
					}
				},
				#endregion

				#region Parse Language Values
				new OperationType() {
					Name = "Parse Value",
					ID = "parseValue",
					Execute = (source, iterators, parameters) => {
						StringBuilder output = new StringBuilder();
						List<string> sourceLines = this.GetLines(source);
						var written = new List<string>();

						foreach (var line in sourceLines)
						{
							var pieces = line.Split(',');
							foreach (var piece in pieces)
							{
								var parts = piece.Trim().Split(' ');
								var part0 = parts[0].ToUpper();
								var part1 = parts[1].Trim('(', ')');

								if (!written.Contains(part0)) {
									output.AppendLine(string.Concat(part0, ",", part1));
									written.Add(part0);
								}
							}
						}
					    return output.ToString();
					}
				},
				#endregion

				#region WPF - Increment Rows
				new OperationType() {
					Name = "WPF - Increment Rows",
					ID = "wpfIncrementRows",
					DefaultSourceDescription = "XAML with 'Grid.Row=\"{INT}\" values",
					DefaultSourceValue = "<StackPanel Grid.Row=\"0\"></StackPanel>\n<StackPanel Grid.Row=\"1\"></StackPanel>\n<StackPanel Grid.Row=\"2\"></StackPanel>",
					DefaultParametersDescription = "Optional integer starting index for incremented row values (Default 0)",
					DefaultParametersValue = "1",
					Execute = (source, iterators, parameters) => {
						string newText = source;
						int startPoint = 0;
						if (!string.IsNullOrEmpty(parameters))
							int.TryParse(parameters, out startPoint);
						for (int i = 100; i >= startPoint; i--)
							newText = newText.Replace("Grid.Row=\"" + i.ToString() + "\"", "Grid.Row=\"" + (i + 1).ToString() + "\"");
						return newText;
					}
				},
				#endregion

				#region Remove Duplicates
				new OperationType() {
					Name = "Remove Duplicates",
					ID = "removeDuplicates",
					DefaultSourceDescription = "Text that has duplicate lines.",
					DefaultSourceValue = "Text\nDifferent\nText",
					DefaultParametersDescription = "",
					DefaultParametersValue = "",
					Execute = (source, iterators, parameters) => {
						StringBuilder output = new StringBuilder();
						List<string> sourceLines = this.GetLines(source);
						var written = new List<string>();

						foreach (var line in sourceLines)
						{
							if (!written.Contains(line)) {
								output.AppendLine(line);
								written.Add(line);
							}
						}
					    return output.ToString();
					}
				},
				#endregion

				#region Temp
				new OperationType() {
					Name = "ZZ-Temp",
					ID = "temp",
					Execute = (source, iterators, parameters) => {
						var date = new DateTime(2013, 7, 31);
						return date.AddMonths(-1).ToString();
					}
				}
				#endregion
			};
		}

		private List<string> GetLines(string text)
		{
			StringReader s = new StringReader(text);
			StringBuilder output = new StringBuilder();
			List<string> lines = new List<string>();
			string line = string.Empty;
			while ((line = s.ReadLine()) != null)
			{
				if (line != string.Empty)
					lines.Add(line);
			}
			return lines;
		}

		#region Operation Functionality
		private string PerformSimpleFormatter(string line, string source, Dictionary<int, int> counters, string iteratorValue)
		{
			var pieces = line.Split(',');
			string sourceModified = source;

			for (int i = 0; i < 30; i++)
			{
				Match match = Regex.Match(sourceModified, "{increment_([0-9])([:].*?)*}");
				if (match.Success)
				{
					int incrementIndex = int.Parse(match.Groups[1].Value);
					if (!counters.ContainsKey(incrementIndex))
					{
						int startValue = 0;
						string startValueString = match.Groups[2].Value.Trim(':');
						if (!string.IsNullOrEmpty(startValueString))
						{
							startValue = int.Parse(startValueString);
						}
						counters.Add(incrementIndex, startValue);
					}
					sourceModified = sourceModified.Replace(match.Value, counters[incrementIndex]++.ToString());
				}
			}

			for (int i = 0; i < 30; i++)
			{
				string lookup = string.Concat("{guid_", i.ToString(), "}");
				if (sourceModified.Contains(lookup))
					sourceModified = sourceModified.Replace(lookup, Guid.NewGuid().ToString());
			}

			if (!string.IsNullOrEmpty(iteratorValue))
			{
				var iteratorPieces = iteratorValue.Split(',');
				for (int i = 0; i < iteratorPieces.Length; i++)
					sourceModified = sourceModified.Replace("{iterator_" + i.ToString() + "}", iteratorPieces[i]);
			}

			var returnValue = string.Format(sourceModified, pieces);
			return returnValue;
		}
		#endregion

		#region Events
		private void RunClick(object sender, RoutedEventArgs e)
		{
			OperationType operation = (OperationType)((ComboBoxItem)this.ddlType.SelectedItem).Tag;
			string sourceText = this.sourceValue.Text;
			string parametersText = this.modifiersValue.Text;

			bool execute = true;
			if (operation.PromptBeforeExecuting)
				execute = MessageBox.Show("Are you sure you want to execute this operation?", "Proceed?", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes;

			if (execute)
			{
				string output = operation.Execute(this.sourceValue.Text, this.iteratorsValue.Text, this.modifiersValue.Text);
				this.outputValue.Text = output;
			}
		}

		private void NewClick(object sender, RoutedEventArgs e)
		{
			this.ddlType.SelectedIndex = 0;
		}

		private void SaveClick(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(this.OpenedFile))
			{
				Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
				dlg.FileName = "DoStuffConfiguration"; // Default file name
				dlg.DefaultExt = ".xml"; // Default file extension
				dlg.Filter = "XML documents (.xml)|*.xml"; // Filter files by extension 
				dlg.CheckPathExists = true;
				dlg.InitialDirectory = Properties.Settings.Default["LastOpened"].ToString();

				// Show save file dialog box
				Nullable<bool> result = dlg.ShowDialog();

				// Process save file dialog box results 
				if (result == true)
				{
					// Save document 
					this.OpenedFile = dlg.FileName;
				}
			}

			// Process save file dialog box results
			if (!string.IsNullOrEmpty(this.OpenedFile))
			{
				string filename = this.OpenedFile;
				ConfigurationDTO dto = new ConfigurationDTO();
				dto.Operation = this.SelectedOperation.ID;
				dto.Source = this.sourceValue.Text;
				dto.Iterators = this.iteratorsValue.Text;
				dto.Elements = this.modifiersValue.Text;
				dto.Output = this.outputValue.Text;

				XmlSerializer serializer = new XmlSerializer(typeof(ConfigurationDTO));
				using (XmlWriter writer = XmlWriter.Create(filename, new XmlWriterSettings() { Indent = true }))
				{
					serializer.Serialize(writer, dto);
				}

				this.Configuration = dto;
				Properties.Settings.Default["LastOpened"] = this.OpenedFile;
				Properties.Settings.Default.Save();
			}
		}

		private void OpenClick(object sender, RoutedEventArgs e)
		{
			// Configure open file dialog box
			Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
			dlg.FileName = "DoStuffConfiguration"; // Default file name
			dlg.DefaultExt = ".xml"; // Default file extension
			dlg.Filter = "XML documents (.xml)|*.xml"; // Filter files by extension 

			// Show open file dialog box
			Nullable<bool> result = dlg.ShowDialog();

			// Process open file dialog box results 
			if (result == true)
				this.ImportSettings(dlg.FileName, true);
		}

		private void ImportSettings(string filename, bool setAsOpenedFile)
		{
			if (File.Exists(filename))
			{
				ConfigurationDTO dto = null;
				XmlSerializer serializer = new XmlSerializer(typeof(ConfigurationDTO));
				using (XmlReader reader = XmlReader.Create(filename))
				{
					dto = serializer.Deserialize(reader) as ConfigurationDTO;
				}

				if (dto != null)
				{
					int index = 0;
					foreach (ComboBoxItem item in this.ddlType.Items)
					{
						OperationType type = (OperationType)item.Tag;
						if (type.ID == dto.Operation)
						{
							this.ddlType.SelectedIndex = index;
							break;
						}
						index++;
					}

					this.sourceValue.Text = dto.Source;
					this.sourceValue.Foreground = Brushes.Black;
					this.modifiersValue.Text = dto.Elements;
					this.modifiersValue.Foreground = Brushes.Black;
					this.iteratorsValue.Text = dto.Iterators;
					this.iteratorsValue.Foreground = Brushes.Black;
					this.outputValue.Text = dto.Output;
					this.outputValue.Foreground = Brushes.Black;

					Properties.Settings.Default["LastOpened"] = filename;
					Properties.Settings.Default.Save();
				}

				// Open document
				if (setAsOpenedFile)
				{
					this.OpenedFile = filename;
					this.Title = "DoStuff - " + filename;
				}
			}
		}

		private void ddlType_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (this.sourceValue != null)
			{
				this.sourceValue.Clear();
				this.iteratorsValue.Clear();
				this.modifiersValue.Clear();
				this.outputValue.Clear();

				if (this.ddlType.SelectedIndex >= 0)
				{
					OperationType type = ((ComboBoxItem)this.ddlType.SelectedItem).Tag as OperationType;
					if (type != null)
					{
						this.sourceValue.Text = type.DefaultSourceValue;
						this.sourceValue.Foreground = Brushes.LightGray;
						this.sourceDefaultText.Text = type.DefaultSourceDescription;

						this.iteratorsValue.Text = string.Empty;
						this.iteratorsValue.Foreground = Brushes.LightGray;
						this.iteratorsDefaultText.Text = string.Empty;

						this.modifiersValue.Text = type.DefaultParametersValue;
						this.modifiersValue.Foreground = Brushes.LightGray;
						this.modifiersDefaultText.Text = type.DefaultParametersDescription;
					}
				}

				this.OpenedFile = null;
			}
		}

		private void sourceValue_TextChanged(object sender, TextChangedEventArgs e)
		{
			this.sourceValue.Foreground = (this.sourceValue.Text == this.SelectedOperation.DefaultSourceValue) ? Brushes.LightGray : Brushes.Black;
		}

		private void modifiersValue_TextChanged(object sender, TextChangedEventArgs e)
		{
			this.modifiersValue.Foreground = (this.modifiersValue.Text == this.SelectedOperation.DefaultSourceValue) ? Brushes.LightGray : Brushes.Black;
		}

		private void iteratorsValue_TextChanged(object sender, TextChangedEventArgs e)
		{
			this.iteratorsValue.Foreground = (this.iteratorsValue.Text == this.SelectedOperation.DefaultSourceValue) ? Brushes.LightGray : Brushes.Black;
		}

		private string ReplaceFirst(string text, string search, string replace)
		{
			int pos = text.IndexOf(search);
			if (pos < 0)
			{
				return text;
			}
			return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
		}
		#endregion

	}
}
