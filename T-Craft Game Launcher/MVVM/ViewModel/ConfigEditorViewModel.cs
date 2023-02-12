using T_Craft_Game_Launcher.Core;

namespace T_Craft_Game_Launcher.MVVM.ViewModel
{
    class ConfigEditorViewModel : ObservableObject
    {
        //private string _selectedFile;
        //private string _text;
        //private ObservableCollection<string> _files;

        //public ConfigEditorViewModel()
        //{
        //    LoadFiles();
        //    SaveCommand = new RelayCommand(Save);
        //}

        //public string SelectedFile
        //{
        //    get { return _selectedFile; }
        //    set { _selectedFile = value; }
        //}

        //public string Text
        //{
        //    get { return _text; }
        //    set { _text = value; }
        //}

        //public ObservableCollection<string> Files
        //{
        //    get { return _files; }
        //    set { _files = value; }
        //}

        //public ICommand SaveCommand { get; set; }

        //private void LoadFiles()
        //{
        //    // Recursively search for .json files
        //    string[] files = Directory.GetFiles(".", "*.json", SearchOption.AllDirectories);
        //    Files = new ObservableCollection<string>(files);
        //}

        //private void Save()
        //{
        //    // Save the contents of the TextEditor to the selected file
        //    File.WriteAllText(SelectedFile, Text);
        //}
    }

    //public class RelayCommand : ICommand
    //{
    //    private readonly Action _execute;

    //    public event EventHandler CanExecuteChanged;

    //    public RelayCommand(Action execute)
    //    {
    //        _execute = execute;
    //    }
    //    public bool CanExecute(object parameter)
    //    {
    //        return true;
    //    }

    //    public void Execute(object parameter)
    //    {
    //        _execute();
    //    }
    //}
}