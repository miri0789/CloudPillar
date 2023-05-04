using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;


public partial class ProgressWindow : Window
{
    public ObservableCollection<FileProgressViewModel> Files { get; } = new ObservableCollection<FileProgressViewModel>();
    public ProgressWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void UpdateProgress(string fileName, int progress, bool isUpload)
    {
        var file = Files.FirstOrDefault(f => f.FileName == fileName);
        if (file == null)
        {
            file = new FileProgressViewModel { FileName = fileName };
            Files.Add(file);
        }

        file.Progress = progress / 100.0;

        if (progress == 100)
        {
            file.Status = isUpload ? "Uploaded" : "Downloaded";
        }
    }
}
