using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;

namespace SPTask3FileEncyrptAndDecyrpt;

public partial class MainWindow : Window, INotifyPropertyChanged
{
	public MainWindow()
	{
		InitializeComponent();

		DataContext = this;

		Min = 0;
	}

	public event PropertyChangedEventHandler? PropertyChanged;
	public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

	private int _min;
	private int _max;
	private int _value;

	private CancellationTokenSource? CTS { get; set; } // Cancellation Token Source
	public int Min
	{
		get => _min;
		set { _min = value; OnPropertyChanged(); }
	}
	public int Max
	{
		get => _max;
		set { _max = value; OnPropertyChanged(); }
	}
	public int Value
	{
		get => _value;
		set { _value = value; OnPropertyChanged(); }
	}

	public string GetFileName()
	{
		var dialog = new OpenFileDialog();

		dialog.Filter = "(*.txt)|*.txt";

		dialog.ShowDialog();

		return dialog.FileName;
	}

	private void GetFileBtn_Click(object sender, RoutedEventArgs e) => FileNameTB.Text = GetFileName();

	public void EncryptProcess(string fileName, byte[] arr, int password, int len, int increaseValue, bool isErrorAccess)
	{
		int currentIndex = 0;

		using (FileStream fs = new(fileName, FileMode.OpenOrCreate))
		{
			fs.Seek(0, SeekOrigin.Begin);

			while (currentIndex < len)
			{
				fs.WriteByte((byte)(arr[currentIndex++] ^ password));
				fs.Flush();

				Dispatcher.Invoke(() => Value += increaseValue);

				Thread.Sleep(10);

				if (isErrorAccess && CTS!.Token.IsCancellationRequested)
					throw new OperationCanceledException();
			}
		}
	}

	private void StartBtn_Click(object sender, RoutedEventArgs e)
	{
		var fileName = FileNameTB.Text;
		int ps = Convert.ToInt32(PasswordTB.Text);

		if (string.IsNullOrEmpty(fileName))
		{
			MessageBox.Show("Please, Select Files.", "File Operation.", MessageBoxButton.OK, MessageBoxImage.Warning);
			return;
		}

		CTS = new();

		ThreadPool.QueueUserWorkItem(state =>
		{
			byte[]? arr = null;

			try
			{
				arr = File.ReadAllBytes(fileName);

				Max = arr.Length;
				Value = 0;

				Dispatcher.Invoke(() =>
					MessageBox.Show("Encrypting Started.", "File Operation.", MessageBoxButton.OK, MessageBoxImage.Information));

				EncryptProcess(fileName, arr, ps, Max, 1, true);
			}
			catch (OperationCanceledException)
			{
				arr = File.ReadAllBytes(fileName);

				Dispatcher.Invoke(() =>
					MessageBox.Show("Encrypting Canceled.", "File Operation.", MessageBoxButton.OK, MessageBoxImage.Information));

				EncryptProcess(fileName, arr, ps, Value, -1, false);
			}
			finally
			{
				CTS = null;
			}

			Dispatcher.Invoke(() =>
				MessageBox.Show("Encrypting Completed.", "File Operation.", MessageBoxButton.OK, MessageBoxImage.Information));
		});
	}


	private void CancelBtn_Click(object sender, RoutedEventArgs e)
	{
		if (CTS is not null)
			CTS.Cancel();
	}

}