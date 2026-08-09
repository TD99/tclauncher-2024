using System;
using System.Collections.Generic;
using System.Windows.Input;
using TCLauncher.Core;
using TCLauncher.Core.Services;
using TCLauncher.Models;

namespace TCLauncher.MVVM.ViewModel
{
    internal sealed class ProfileCreatorViewModel : ObservableObject
    {
        private readonly IProfileService _profiles;
        private int _currentStep;
        private string _errorMessage;

        public ProfileDraft Draft { get; }
        public IEnumerable<LoaderType> LoaderTypes { get; } = (LoaderType[])Enum.GetValues(typeof(LoaderType));
        public string StorageRoot { get; }
        public int CurrentStep
        {
            get => _currentStep;
            set { _currentStep = Math.Max(0, Math.Min(5, value)); OnPropertyChanged(); OnPropertyChanged(nameof(StepLabel)); OnPropertyChanged(nameof(Draft)); }
        }
        public string StepLabel => (CurrentStep + 1) + " / 6";
        public string ErrorMessage { get => _errorMessage; set { _errorMessage = value; OnPropertyChanged(); } }
        public ICommand BackCommand { get; }
        public ICommand NextCommand { get; }
        public ICommand CreateCommand { get; }
        public event EventHandler<InstalledInstance> ProfileCreated;

        public ProfileCreatorViewModel(IProfileService profiles, string storageRoot, ProfileDraft draft = null)
        {
            _profiles = profiles;
            StorageRoot = storageRoot;
            Draft = draft ?? new ProfileDraft();
            BackCommand = new RelayCommand(_ => CurrentStep--);
            NextCommand = new RelayCommand(_ => Next());
            CreateCommand = new RelayCommand(_ => Create());
        }

        public void RefreshDraft()
        {
            OnPropertyChanged(nameof(Draft));
        }

        private void Next()
        {
            ErrorMessage = ValidateStep(CurrentStep);
            if (ErrorMessage == null) CurrentStep++;
        }

        private string ValidateStep(int step)
        {
            switch (step)
            {
                case 0:
                    if (string.IsNullOrWhiteSpace(Draft.DisplayName)) return "Enter a profile name.";
                    if (string.IsNullOrWhiteSpace(Draft.Name) || !System.Text.RegularExpressions.Regex.IsMatch(Draft.Name, "^[A-Za-z0-9._-]+$"))
                        return "The internal name may contain letters, numbers, dots, underscores, and hyphens.";
                    break;
                case 1:
                    if (string.IsNullOrWhiteSpace(Draft.MinecraftVersion)) return "Enter a Minecraft version.";
                    break;
                case 2:
                    if (Draft.LoaderType != LoaderType.Vanilla && string.IsNullOrWhiteSpace(Draft.LoaderVersion))
                        return "Choose a loader version.";
                    break;
                case 3:
                    if (Draft.MinimumRamMb < 0 || Draft.MaximumRamMb <= 0 || Draft.MinimumRamMb > Draft.MaximumRamMb)
                        return "Choose a valid memory range.";
                    break;
            }
            return null;
        }

        private void Create()
        {
            var result = _profiles.Create(Draft);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Message;
                return;
            }
            ProfileCreated?.Invoke(this, result.Value);
        }
    }
}
