﻿using Files.Helpers;
using Files.Services;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;

namespace Files.ViewModels.SettingsViewModels
{
    public class ExperimentalViewModel : ObservableObject
    {
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        public ICommand EditFileTagsCommand { get; }

        public ICommand SetAsDefaultExplorerCommand { get; }

        public ICommand SetAsOpenFileDialogCommand { get; }

        public ExperimentalViewModel()
        {
            IsSetAsDefaultFileManager = DetectIsSetAsDefaultFileManager();
            IsSetAsOpenFileDialog = DetectIsSetAsOpenFileDialog();

            EditFileTagsCommand =  new AsyncRelayCommand(LaunchFileTagsConfigFile);
            SetAsDefaultExplorerCommand = new AsyncRelayCommand(SetAsDefaultExplorer);
            SetAsOpenFileDialogCommand = new AsyncRelayCommand(SetAsOpenFileDialog);
        }

        public bool AreFileTagsEnabled
        {
            get => UserSettingsService.FilesAndFoldersSettingsService.AreFileTagsEnabled;
            set
            {
                if (value != UserSettingsService.FilesAndFoldersSettingsService.AreFileTagsEnabled)
                {
                    UserSettingsService.FilesAndFoldersSettingsService.AreFileTagsEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        private async Task LaunchFileTagsConfigFile()
        {
            var configFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appdata:///local/settings/filetags.json"));
            if (!await Launcher.LaunchFileAsync(configFile))
            {
                var connection = await AppServiceConnectionHelper.Instance;
                if (connection != null)
                {
                    await connection.SendMessageAsync(new ValueSet()
                    {
                        { "Arguments", "InvokeVerb" },
                        { "FilePath", configFile.Path },
                        { "Verb", "open" }
                    });
                }
            }
        }

        private async Task SetAsDefaultExplorer()
        {
            if (IsSetAsDefaultFileManager == DetectIsSetAsDefaultFileManager())
            {
                return;
            }
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection != null)
            {
                var (_, _) = await connection.SendMessageForResponseAsync(new ValueSet()
                {
                    { "Arguments", "SetAsDefaultExplorer" },
                    { "Value", IsSetAsDefaultFileManager }
                });
            }
            IsSetAsDefaultFileManager = DetectIsSetAsDefaultFileManager();
            if (!IsSetAsDefaultFileManager)
            {
                IsSetAsOpenFileDialog = false;
                await SetAsOpenFileDialog();
            }
        }

        private async Task SetAsOpenFileDialog()
        {
            if (IsSetAsOpenFileDialog == DetectIsSetAsOpenFileDialog())
            {
                return;
            }
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection != null)
            {
                var (_, _) = await connection.SendMessageForResponseAsync(new ValueSet()
                {
                    { "Arguments", "SetAsOpenFileDialog" },
                    { "Value", IsSetAsOpenFileDialog }
                });
            }
            IsSetAsOpenFileDialog = DetectIsSetAsOpenFileDialog();
        }

        private bool DetectIsSetAsDefaultFileManager()
        {
            using var subkey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Classes\Directory\shell");
            return subkey?.GetValue(string.Empty) as string == "openinfiles";
        }

        private bool DetectIsSetAsOpenFileDialog()
        {
            using var subkey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Classes\CLSID\{DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7}");
            return subkey?.GetValue(string.Empty) as string == "FilesOpenDialog class";
        }

        private bool isSetAsDefaultFileManager;

        public bool IsSetAsDefaultFileManager
        {
            get => isSetAsDefaultFileManager;
            set => SetProperty(ref isSetAsDefaultFileManager, value);
        }

        private bool isSetAsOpenFileDialog;

        public bool IsSetAsOpenFileDialog
        {
            get => isSetAsOpenFileDialog;
            set => SetProperty(ref isSetAsOpenFileDialog, value);
        }
    }
}